using System.Globalization;
using BearNecessities.Application.UseCases.Events.UpdateEvent;
using BearNecessities.Application.UseCases.Places.UpdatePlace;
using BearNecessities.Domain.Entities;

namespace BearNecessities.Application.UseCases.EditSuggestions;

public sealed record FieldChangeDto(string Field, string? Current, string? Proposed);

/// <summary>Lists the fields an edit suggestion would change, as the admin reads them: current value next to proposed.</summary>
public static class EditSuggestionDiff
{
    public static List<FieldChangeDto> ForPlace(Place place, UpdatePlaceRequest edit, IReadOnlyDictionary<Guid, string> categoryNames)
    {
        var changes = new List<FieldChangeDto>();
        AddIfProposed(changes, "Name", place.Name, edit.Name);
        AddIfProposed(changes, "Description", place.Description, edit.Description);
        AddIfProposed(changes, "Address", place.Address, edit.Address);
        AddIfProposed(changes, "City", place.City, edit.City);
        AddIfProposed(changes, "Country", place.Country, edit.Country);
        AddIfProposed(changes, "Timezone", place.Timezone, edit.Timezone);
        AddIfProposed(changes, "Ongoing", YesNo(place.IsPermanent), edit.IsPermanent.HasValue ? YesNo(edit.IsPermanent.Value) : null);

        if (edit.Latitude.HasValue || edit.Longitude.HasValue)
            AddIfProposed(changes, "Location", Location(place.Latitude, place.Longitude),
                Location(edit.Latitude ?? place.Latitude, edit.Longitude ?? place.Longitude));

        if (edit.CategoryIds is not null)
            AddIfProposed(changes, "Categories", CategoryList(place.Categories.Select(c => c.Id.Value), categoryNames),
                CategoryList(edit.CategoryIds, categoryNames));

        if (edit.ExternalLinks is not null)
            AddIfProposed(changes, "Links", string.Join("\n", place.ExternalLinks.OrderBy(l => l.SortOrder).Select(l => l.Url)),
                string.Join("\n", edit.ExternalLinks.Select(l => l.Url)));

        return changes;
    }

    public static List<FieldChangeDto> ForEvent(
        Event evt, UpdateEventRequest edit, IReadOnlyDictionary<Guid, string> categoryNames, IReadOnlyDictionary<Guid, string> venueNames)
    {
        var changes = new List<FieldChangeDto>();
        AddIfDifferent(changes, "Title", evt.Title, edit.Title);
        AddIfDifferent(changes, "Description", evt.Description, edit.Description);
        AddIfDifferent(changes, "Type", evt.EventType.ToString(), edit.EventType.ToString());
        AddIfDifferent(changes, "Starts", DateTimeText(evt.StartAt), DateTimeText(edit.StartAt));
        AddIfDifferent(changes, "Ends", DateTimeText(evt.EndAt), DateTimeText(edit.EndAt));
        AddIfDifferent(changes, "Repeats", evt.RRule, edit.RRule);
        AddIfDifferent(changes, "Timezone", evt.Timezone, edit.Timezone);
        AddIfDifferent(changes, "Price", PriceText(evt.IsFree, evt.Price, evt.Currency), PriceText(edit.IsFree, edit.Price, edit.Currency));
        AddIfDifferent(changes, "Online link", evt.IsOnline ? evt.OnlineUrl : null, edit.IsOnline ? edit.OnlineUrl : null);
        AddIfDifferent(changes, "Capacity", evt.MaxCapacity?.ToString(CultureInfo.InvariantCulture), edit.MaxCapacity?.ToString(CultureInfo.InvariantCulture));
        AddIfDifferent(changes, "Venue", VenueName(evt.PlaceId?.Value, venueNames), VenueName(edit.PlaceId, venueNames));
        AddIfDifferent(changes, "Categories", CategoryList(evt.Categories.Select(c => c.Id.Value), categoryNames),
            CategoryList(edit.CategoryIds, categoryNames));
        return changes;
    }

    // Place edits are partial: a field left null keeps its current value
    private static void AddIfProposed(List<FieldChangeDto> changes, string field, string? current, string? proposed)
    {
        if (proposed is not null) AddIfDifferent(changes, field, current, proposed);
    }

    // Event edits carry every field, so a null clears it
    private static void AddIfDifferent(List<FieldChangeDto> changes, string field, string? current, string? proposed)
    {
        if (Normalize(current) != Normalize(proposed)) changes.Add(new FieldChangeDto(field, current, proposed));
    }

    private static string Normalize(string? text) => (text ?? string.Empty).Trim();

    private static string YesNo(bool flag) => flag ? "Yes" : "No";

    private static string Location(decimal latitude, decimal longitude) =>
        string.Create(CultureInfo.InvariantCulture, $"{latitude:F6}, {longitude:F6}");

    private static string? DateTimeText(DateTime? moment) =>
        moment?.ToString("yyyy-MM-dd HH:mm", CultureInfo.InvariantCulture);

    private static string PriceText(bool isFree, decimal? price, string? currency) =>
        isFree || price is null ? "Free" : string.Create(CultureInfo.InvariantCulture, $"{price:0.##} {currency}").Trim();

    private static string? VenueName(Guid? placeId, IReadOnlyDictionary<Guid, string> venueNames) =>
        placeId is null ? null : venueNames.GetValueOrDefault(placeId.Value, "Unknown place");

    private static string CategoryList(IEnumerable<Guid> categoryIds, IReadOnlyDictionary<Guid, string> categoryNames) =>
        string.Join(", ", categoryIds.Select(id => categoryNames.GetValueOrDefault(id, "Unknown")).Order());
}
