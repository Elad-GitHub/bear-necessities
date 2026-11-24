using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using BearNecessities.Application.Imports;
using BearNecessities.Application.Interfaces;
using BearNecessities.Domain.Entities;
using BearNecessities.Domain.Enums;
using BearNecessities.Infrastructure.Persistence;

namespace BearNecessities.Infrastructure.Imports;

public sealed class PlaceImportService : IPlaceImportService
{
    private const string ImportOwnerEmail = "demo@bearnecessities.app";
    private const int SaveEvery = 100;
    private const int MaxCategoriesPerPlace = 5;

    // What the first version of the OSM import could assign; a place with only these was never categorised by hand
    private static readonly HashSet<string> FirstImportSlugs = new(StringComparer.OrdinalIgnoreCase)
    {
        "yoga", "vegan-restaurant", "indian-food", "health-food-cafe", "camping", "hot-springs", "farmers-market", "community-garden"
    };

    private readonly BearNecessitiesDbContext _db;
    private readonly ILogger<PlaceImportService> _logger;
    private readonly ITimeZoneLookup _timeZones;

    public PlaceImportService(BearNecessitiesDbContext db, ILogger<PlaceImportService> logger, ITimeZoneLookup timeZones)
    {
        _db = db;
        _logger = logger;
        _timeZones = timeZones;
    }

    public async Task<ImportReport> ImportAsync(
        IReadOnlyList<ImportPlace> places,
        bool autoApprove,
        CancellationToken cancellationToken = default)
    {
        var owner = await ResolveImportOwnerAsync(cancellationToken);
        if (owner is null)
            return new ImportReport(0, 0, places.Count,
                ["No import owner found — seed the demo/admin user first."]);

        var categoriesBySlug = await LoadCategoriesAsync(cancellationToken);

        // Pre-load dedup keys so we don't round-trip per record; deleted places count too, so a re-import doesn't revive them
        var existing = await _db.Places
            .IgnoreQueryFilters()
            .Select(p => new { p.Name, p.Latitude, p.Longitude, p.ExternalSourceId, HasDescription = p.Description != null })
            .ToListAsync(cancellationToken);
        // Re-imports fill in descriptions on places imported before descriptions were generated
        var missingDescription = new HashSet<string>(
            existing.Where(e => e.ExternalSourceId != null && !e.HasDescription).Select(e => e.ExternalSourceId!),
            StringComparer.OrdinalIgnoreCase);
        var descriptionsToBackfill = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        var categoriesToUpdate = new Dictionary<string, IReadOnlyList<string>>(StringComparer.OrdinalIgnoreCase);

        var seenExternalIds = new HashSet<string>(
            existing.Where(e => e.ExternalSourceId != null).Select(e => e.ExternalSourceId!),
            StringComparer.OrdinalIgnoreCase);
        var seenNameGeo = new HashSet<string>(
            existing.Select(e => NameGeoKey(e.Name, e.Latitude, e.Longitude)),
            StringComparer.OrdinalIgnoreCase);

        var inserted = 0;
        var duplicates = 0;
        var failed = 0;
        var errors = new List<string>();

        foreach (var candidate in places)
        {
            try
            {
                if (IsDuplicate(candidate, seenExternalIds, seenNameGeo))
                {
                    duplicates++;
                    if (candidate.ExternalSourceId is not null && candidate.Description is not null
                        && missingDescription.Contains(candidate.ExternalSourceId))
                        descriptionsToBackfill[candidate.ExternalSourceId] = candidate.Description;
                    if (candidate.ExternalSourceId is not null && candidate.CategorySlugs.Count > 0)
                        categoriesToUpdate[candidate.ExternalSourceId] = candidate.CategorySlugs;
                    continue;
                }

                var place = BuildPlace(candidate, owner, categoriesBySlug, autoApprove);
                await _db.Places.AddAsync(place, cancellationToken);

                if (candidate.ExternalSourceId is not null) seenExternalIds.Add(candidate.ExternalSourceId);
                seenNameGeo.Add(NameGeoKey(candidate.Name, candidate.Latitude, candidate.Longitude));
                inserted++;

                if (inserted % SaveEvery == 0)
                    await _db.SaveChangesAsync(cancellationToken);
            }
            catch (Exception ex)
            {
                failed++;
                errors.Add($"{candidate.Name}: {ex.Message}");
                _logger.LogWarning(ex, "Failed to import place {Name}", candidate.Name);
            }
        }

        await _db.SaveChangesAsync(cancellationToken);
        await BackfillDescriptionsAsync(descriptionsToBackfill, cancellationToken);
        await UpdateCategoriesAsync(categoriesToUpdate, categoriesBySlug, cancellationToken);
        return new ImportReport(inserted, duplicates, failed, errors);
    }

    // Re-imports bring places already on the map up to the current category rules. Places whose categories all
    // came from the first import get the new set; places someone categorised by hand only gain what's missing.
    private async Task UpdateCategoriesAsync(
        Dictionary<string, IReadOnlyList<string>> slugsByExternalId,
        IReadOnlyDictionary<string, Category> categoriesBySlug,
        CancellationToken cancellationToken)
    {
        if (slugsByExternalId.Count == 0) return;

        var externalIds = slugsByExternalId.Keys.ToList();
        var places = await _db.Places
            .Include(p => p.Categories)
            .Where(p => p.ExternalSourceId != null && externalIds.Contains(p.ExternalSourceId))
            .ToListAsync(cancellationToken);

        var changed = 0;
        foreach (var place in places)
        {
            var wanted = slugsByExternalId[place.ExternalSourceId!]
                .Where(categoriesBySlug.ContainsKey)
                .Select(slug => categoriesBySlug[slug])
                .ToList();
            var current = place.Categories.Select(c => c.Slug).ToHashSet(StringComparer.OrdinalIgnoreCase);
            if (wanted.Count == 0 || wanted.All(category => current.Contains(category.Slug)) && wanted.Count == current.Count) continue;

            if (current.All(FirstImportSlugs.Contains))
            {
                place.ClearCategories();
                foreach (var category in wanted.Take(MaxCategoriesPerPlace)) place.AddCategory(category);
            }
            else
            {
                foreach (var category in wanted.Where(category => !current.Contains(category.Slug))
                    .Take(MaxCategoriesPerPlace - current.Count))
                    place.AddCategory(category);
            }
            changed++;
        }

        await _db.SaveChangesAsync(cancellationToken);
        _logger.LogInformation("Updated categories on {Count} existing places", changed);
    }

    // One query loads every place that needs a description; one save writes them all
    private async Task BackfillDescriptionsAsync(Dictionary<string, string> descriptionsByExternalId, CancellationToken cancellationToken)
    {
        if (descriptionsByExternalId.Count == 0) return;

        var externalIds = descriptionsByExternalId.Keys.ToList();
        var places = await _db.Places
            .Where(p => p.ExternalSourceId != null && externalIds.Contains(p.ExternalSourceId))
            .ToListAsync(cancellationToken);
        foreach (var place in places.Where(p => p.Description is null))
            place.SetDescription(descriptionsByExternalId[place.ExternalSourceId!]);

        await _db.SaveChangesAsync(cancellationToken);
        _logger.LogInformation("Filled in descriptions on {Count} existing places", places.Count);
    }

    private Place BuildPlace(
        ImportPlace candidate,
        User owner,
        IReadOnlyDictionary<string, Category> categoriesBySlug,
        bool autoApprove)
    {
        var place = Place.Create(candidate.Name, candidate.Latitude, candidate.Longitude,
            _timeZones.FindTimeZone(candidate.Latitude, candidate.Longitude), owner.Id);

        if (!string.IsNullOrWhiteSpace(candidate.Description))
            place.SetDescription(candidate.Description);
        place.SetOpeningHours(candidate.OpeningHours);

        place.SetAddress(candidate.Address, candidate.City, candidate.Country, candidate.CountryCode);
        place.SetIsPermanent(candidate.IsPermanent);
        place.SetSource(candidate.Source, candidate.ExternalSourceId);
        place.UpdateCoordinates(
            candidate.Latitude,
            candidate.Longitude,
            $"h3_res8_{candidate.Latitude:F4}_{candidate.Longitude:F4}",
            $"h3_res5_{candidate.Latitude:F2}_{candidate.Longitude:F2}");

        foreach (var slug in candidate.CategorySlugs.Distinct().Take(5))
            if (categoriesBySlug.TryGetValue(slug, out var category))
                place.AddCategory(category);

        // A place without a category never shows under any filter, so a typo'd slug list must fail the row, not hide it
        if (place.Categories.Count == 0)
            throw new InvalidOperationException(
                $"no known category in '{string.Join(";", candidate.CategorySlugs)}' (slugs are separated by ';')");

        if (!string.IsNullOrWhiteSpace(candidate.Website))
            place.AddExternalLink(PlaceExternalLink.Create(place.Id, "Website", candidate.Website!));

        place.Submit();
        if (autoApprove)
            place.Approve(owner.Id);

        return place;
    }

    private static bool IsDuplicate(
        ImportPlace candidate,
        HashSet<string> seenExternalIds,
        HashSet<string> seenNameGeo)
    {
        if (candidate.ExternalSourceId is not null && seenExternalIds.Contains(candidate.ExternalSourceId))
            return true;

        return seenNameGeo.Contains(NameGeoKey(candidate.Name, candidate.Latitude, candidate.Longitude));
    }

    // ~110m precision so the same spot from two sources collapses into one listing
    private static string NameGeoKey(string name, decimal latitude, decimal longitude)
        => $"{name.Trim().ToLowerInvariant()}|{Math.Round(latitude, 3)}|{Math.Round(longitude, 3)}";

    private async Task<User?> ResolveImportOwnerAsync(CancellationToken cancellationToken)
    {
        var demoUser = await _db.Users.FirstOrDefaultAsync(u => u.Email == ImportOwnerEmail, cancellationToken);
        if (demoUser is not null) return demoUser;

        return await _db.Users.FirstOrDefaultAsync(cancellationToken);
    }

    private async Task<IReadOnlyDictionary<string, Category>> LoadCategoriesAsync(CancellationToken cancellationToken)
    {
        var categories = await _db.Categories.ToListAsync(cancellationToken);
        return categories
            .GroupBy(c => c.Slug, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(g => g.Key, g => g.First(), StringComparer.OrdinalIgnoreCase);
    }
}
