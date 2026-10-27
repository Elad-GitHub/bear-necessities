using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Nomadica.Application.Imports;
using Nomadica.Application.Interfaces;
using Nomadica.Domain.Entities;
using Nomadica.Domain.Enums;
using Nomadica.Infrastructure.Persistence;

namespace Nomadica.Infrastructure.Imports;

public sealed class PlaceImportService : IPlaceImportService
{
    private const string ImportOwnerEmail = "demo@nomadica.app";
    private const int SaveEvery = 100;

    private readonly NomadiaDbContext _db;
    private readonly ILogger<PlaceImportService> _logger;

    public PlaceImportService(NomadiaDbContext db, ILogger<PlaceImportService> logger)
    {
        _db = db;
        _logger = logger;
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

        // Pre-load dedup keys so we don't round-trip per record
        var existing = await _db.Places
            .Select(p => new { p.Name, p.Latitude, p.Longitude, p.ExternalSourceId })
            .ToListAsync(cancellationToken);

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
        return new ImportReport(inserted, duplicates, failed, errors);
    }

    private Place BuildPlace(
        ImportPlace candidate,
        User owner,
        IReadOnlyDictionary<string, Category> categoriesBySlug,
        bool autoApprove)
    {
        var place = Place.Create(candidate.Name, candidate.Latitude, candidate.Longitude, "UTC", owner.Id);

        if (!string.IsNullOrWhiteSpace(candidate.Description))
            place.SetDescription(candidate.Description);

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
