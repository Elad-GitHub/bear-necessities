using System.Text.Json;
using BearNecessities.Application.Exceptions;
using BearNecessities.Domain.Entities;
using BearNecessities.Domain.Enums;
using BearNecessities.Domain.Interfaces;
using BearNecessities.Domain.ValueObjects;

namespace BearNecessities.Application.UseCases.Places.UpdatePlace;

/// <summary>
/// Checks and applies an edit to a place. Shared by direct edits and by approved edit suggestions,
/// so both change the place the same way.
/// </summary>
public static class PlaceEdits
{
    private const int MaxNameLength = 200;
    private const int MaxDescriptionLength = 4000;
    private const int MaxTimezoneLength = 100;
    private const int MaxCategories = 5;
    private const int MaxLinkLabelLength = 100;
    private const int MaxLinkUrlLength = 1024;

    /// <summary>Applies the edit and records it in the change log; the caller saves.</summary>
    public static async Task ApplyAsync(
        IUnitOfWork unitOfWork, Place place, UpdatePlaceRequest request, UserId editorId, CancellationToken cancellationToken)
    {
        var oldSnapshot = Snapshot(place);
        var isMajorChange = IsMajorChange(request, place);

        ApplyFieldUpdates(request, place);

        if (request.CategoryIds is not null)
        {
            var categoryIds = request.CategoryIds.Select(id => new CategoryId(id)).ToList();
            var allCategories = await unitOfWork.Categories.GetAllAsync(cancellationToken);
            var foundCategories = allCategories.Where(c => categoryIds.Contains(c.Id)).ToList();

            if (foundCategories.Count != categoryIds.Count)
                throw new ValidationException(new Dictionary<string, string[]>
                {
                    ["categoryIds"] = ["One or more category IDs were not found."]
                });

            place.ClearCategories();
            foreach (var category in foundCategories)
                place.AddCategory(category);
        }

        if (request.ExternalLinks is not null)
        {
            var newLinks = request.ExternalLinks
                .Select((link, index) => PlaceExternalLink.Create(place.Id, link.Label, link.Url, index))
                .ToList();
            await unitOfWork.Places.ReplaceExternalLinksAsync(place.Id, newLinks, cancellationToken);
        }

        var changeLog = ListingChangeLog.Create(
            ListingChangeTargetType.Place,
            place.Id.Value,
            editorId,
            isMajorChange ? ChangeType.MajorEdit : ChangeType.MinorEdit,
            oldData: oldSnapshot,
            newData: Snapshot(place));
        await unitOfWork.ChangeLogs.AddAsync(changeLog, cancellationToken);

        await unitOfWork.Places.UpdateAsync(place, cancellationToken);
    }

    public static void Validate(UpdatePlaceRequest request)
    {
        var errors = new Dictionary<string, string[]>();

        if (request.Name is not null)
        {
            if (string.IsNullOrWhiteSpace(request.Name))
                errors["name"] = ["Name cannot be empty."];
            else if (request.Name.Length > MaxNameLength)
                errors["name"] = [$"Name must not exceed {MaxNameLength} characters."];
        }

        if (request.Description?.Length > MaxDescriptionLength)
            errors["description"] = [$"Description must not exceed {MaxDescriptionLength} characters."];

        if (request.Latitude.HasValue && (request.Latitude < -90m || request.Latitude > 90m))
            errors["latitude"] = ["Latitude must be between -90 and 90."];

        if (request.Longitude.HasValue && (request.Longitude < -180m || request.Longitude > 180m))
            errors["longitude"] = ["Longitude must be between -180 and 180."];

        if (request.CountryCode is not null && request.CountryCode.Length != 2)
            errors["countryCode"] = ["Country code must be exactly 2 characters."];

        if (request.Timezone is not null)
        {
            if (string.IsNullOrWhiteSpace(request.Timezone))
                errors["timezone"] = ["Timezone cannot be empty."];
            else if (request.Timezone.Length > MaxTimezoneLength)
                errors["timezone"] = [$"Timezone must not exceed {MaxTimezoneLength} characters."];
        }

        if (request.CategoryIds is not null)
        {
            if (request.CategoryIds.Count == 0)
                errors["categoryIds"] = ["At least one category is required."];
            else if (request.CategoryIds.Count > MaxCategories)
                errors["categoryIds"] = [$"A place cannot have more than {MaxCategories} categories."];
        }

        for (var i = 0; i < (request.ExternalLinks?.Count ?? 0); i++)
        {
            var link = request.ExternalLinks![i];
            if (string.IsNullOrWhiteSpace(link.Label) || link.Label.Length > MaxLinkLabelLength)
                errors[$"externalLinks[{i}].label"] = [$"Label is required and must not exceed {MaxLinkLabelLength} characters."];
            if (string.IsNullOrWhiteSpace(link.Url) || link.Url.Length > MaxLinkUrlLength ||
                !Uri.IsWellFormedUriString(link.Url, UriKind.Absolute))
                errors[$"externalLinks[{i}].url"] = [$"A valid absolute URL is required, max {MaxLinkUrlLength} characters."];
        }

        if (errors.Count > 0)
            throw new ValidationException(errors);
    }

    private static string Snapshot(Place place) => JsonSerializer.Serialize(new
    {
        place.Name, place.Latitude, place.Longitude,
        Status = place.Status.ToString(),
        CategoryIds = place.Categories.Select(c => c.Id.Value)
    });

    private static bool IsMajorChange(UpdatePlaceRequest request, Place place)
    {
        if (request.Name is not null && request.Name.Trim() != place.Name)
            return true;

        if ((request.Latitude ?? place.Latitude) != place.Latitude || (request.Longitude ?? place.Longitude) != place.Longitude)
            return true;

        if (request.CategoryIds is null)
            return false;

        var existing = place.Categories.Select(c => c.Id.Value).Order();
        return !existing.SequenceEqual(request.CategoryIds.Order());
    }

    private static void ApplyFieldUpdates(UpdatePlaceRequest request, Place place)
    {
        if (request.Name is not null)
            place.SetName(request.Name);

        if (request.Description is not null)
            place.SetDescription(request.Description);

        if (request.Latitude.HasValue || request.Longitude.HasValue)
        {
            var latitude = request.Latitude ?? place.Latitude;
            var longitude = request.Longitude ?? place.Longitude;
            place.UpdateCoordinates(latitude, longitude, $"h3_res8_{latitude:F4}_{longitude:F4}", $"h3_res5_{latitude:F2}_{longitude:F2}");
        }

        if (request.Address is not null || request.City is not null || request.Country is not null || request.CountryCode is not null)
            place.SetAddress(request.Address ?? place.Address, request.City ?? place.City,
                request.Country ?? place.Country, request.CountryCode ?? place.CountryCode);

        if (request.Timezone is not null)
            place.SetTimezone(request.Timezone);

        if (request.IsPermanent.HasValue)
            place.SetIsPermanent(request.IsPermanent.Value);
    }
}
