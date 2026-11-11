using System.Text.Json;
using BearNecessities.Application.Exceptions;
using BearNecessities.Application.Interfaces;
using BearNecessities.Domain.Entities;
using BearNecessities.Domain.Enums;
using BearNecessities.Domain.Interfaces;
using BearNecessities.Domain.ValueObjects;

namespace BearNecessities.Application.UseCases.Events.UpdateEvent;

/// <summary>
/// Checks and applies an edit to an event, rescheduling its occurrences when the timing changed.
/// Shared by direct edits and by approved edit suggestions.
/// </summary>
public static class EventEdits
{
    private const int MaxTitleLength = 200;
    private const int MaxDescriptionLength = 5000;
    private const int MaxCategories = 5;
    private const int ScheduleDaysAhead = 90;

    /// <summary>Applies the edit and records it in the change log; the caller saves. Returns the occurrences scheduled.</summary>
    public static async Task<int> ApplyAsync(
        IUnitOfWork unitOfWork, IOccurrenceExpander expander, Event evt, UpdateEventRequest request,
        UserId editorId, CancellationToken cancellationToken)
    {
        if (request.PlaceId.HasValue)
        {
            var place = await unitOfWork.Places.GetByIdAsync(new PlaceId(request.PlaceId.Value), cancellationToken)
                ?? throw new NotFoundException("Place", request.PlaceId.Value);

            if (place.Status != ListingStatus.Live)
                throw new ValidationException(new Dictionary<string, string[]>
                {
                    ["placeId"] = ["The referenced place must be Live."]
                });
        }

        var allCategories = await unitOfWork.Categories.GetAllAsync(cancellationToken);
        var categoryIds = request.CategoryIds.Select(id => new CategoryId(id)).ToList();
        var foundCategories = allCategories.Where(c => categoryIds.Contains(c.Id)).ToList();

        if (foundCategories.Count != categoryIds.Count)
        {
            var missing = categoryIds.Where(id => foundCategories.All(c => c.Id != id)).Select(id => id.Value.ToString());
            throw new ValidationException(new Dictionary<string, string[]>
            {
                ["categoryIds"] = [$"Categories not found: {string.Join(", ", missing)}."]
            });
        }

        var oldData = Snapshot(evt);
        var isRescheduled = evt.RRule != request.RRule || evt.StartAt != request.StartAt;
        var isMajorChange = isRescheduled || evt.EventType != request.EventType;

        evt.SetTitle(request.Title);
        evt.SetDescription(request.Description);
        evt.SetPlaceId(request.PlaceId.HasValue ? new PlaceId(request.PlaceId.Value) : null);
        evt.SetStartAt(request.StartAt);
        evt.SetEndAt(request.EndAt);
        evt.SetRRule(request.RRule);
        evt.SetPrice(request.Price, request.Currency, request.IsFree);
        evt.SetIsOnline(request.IsOnline, request.OnlineUrl);
        evt.SetMaxCapacity(request.MaxCapacity);
        evt.SetTimezone(request.Timezone);
        evt.SetEventType(request.EventType);
        evt.ClearCategories();
        foreach (var category in foundCategories)
            evt.AddCategory(category);

        await unitOfWork.Events.UpdateAsync(evt, cancellationToken);

        var occurrencesScheduled = isRescheduled
            ? await RescheduleAsync(unitOfWork, expander, evt.Id, request, cancellationToken)
            : 0;

        var changeLog = ListingChangeLog.Create(
            ListingChangeTargetType.Event,
            evt.Id.Value,
            editorId,
            isMajorChange ? ChangeType.MajorEdit : ChangeType.MinorEdit,
            oldData: oldData,
            newData: Snapshot(evt));
        await unitOfWork.ChangeLogs.AddAsync(changeLog, cancellationToken);

        return occurrencesScheduled;
    }

    public static void Validate(UpdateEventRequest request)
    {
        var errors = new Dictionary<string, string[]>();

        if (string.IsNullOrWhiteSpace(request.Title))
            errors["title"] = ["Title is required."];
        else if (request.Title.Length > MaxTitleLength)
            errors["title"] = [$"Title must not exceed {MaxTitleLength} characters."];

        if (request.Description?.Length > MaxDescriptionLength)
            errors["description"] = [$"Description must not exceed {MaxDescriptionLength} characters."];

        if (string.IsNullOrWhiteSpace(request.Timezone))
            errors["timezone"] = ["Timezone is required."];

        if (request.EventType != EventType.Permanent && !request.StartAt.HasValue)
            errors["startAt"] = ["StartAt is required for OneTime and Recurring events."];

        if (request.StartAt.HasValue && request.EndAt.HasValue && request.EndAt.Value <= request.StartAt.Value)
            errors["endAt"] = ["EndAt must be after StartAt."];

        if (request.EventType == EventType.Recurring && string.IsNullOrWhiteSpace(request.RRule))
            errors["rrule"] = ["RRule is required for Recurring events."];

        if (request.Price.HasValue && request.Price.Value < 0)
            errors["price"] = ["Price must be zero or greater."];

        if (request.Price.HasValue && request.Price.Value > 0 && string.IsNullOrWhiteSpace(request.Currency))
            errors["currency"] = ["Currency is required when Price is greater than zero."];

        if (request.Currency is not null && request.Currency.Length != 3)
            errors["currency"] = ["Currency must be an ISO 4217 code (3 characters)."];

        if (request.IsOnline && string.IsNullOrWhiteSpace(request.OnlineUrl))
            errors["onlineUrl"] = ["OnlineUrl is required for online events."];

        if (request.MaxCapacity.HasValue && request.MaxCapacity.Value <= 0)
            errors["maxCapacity"] = ["MaxCapacity must be greater than zero."];

        if (request.CategoryIds is null || request.CategoryIds.Count == 0)
            errors["categoryIds"] = ["At least one category is required."];
        else if (request.CategoryIds.Count > MaxCategories)
            errors["categoryIds"] = [$"An event cannot have more than {MaxCategories} categories."];

        if (errors.Count > 0)
            throw new ValidationException(errors);
    }

    private static async Task<int> RescheduleAsync(
        IUnitOfWork unitOfWork, IOccurrenceExpander expander, EventId eventId, UpdateEventRequest request,
        CancellationToken cancellationToken)
    {
        if (!request.StartAt.HasValue) return 0;

        List<EventOccurrence> occurrences;
        if (request.EventType == EventType.Recurring && request.RRule is not null)
        {
            TimeSpan? duration = request.EndAt.HasValue ? request.EndAt.Value - request.StartAt.Value : null;
            occurrences = expander.ExpandOccurrences(request.RRule, request.StartAt.Value, request.Timezone, daysAhead: ScheduleDaysAhead)
                .Select(occurrence => EventOccurrence.Create(eventId, occurrence.StartUtc,
                    duration.HasValue ? occurrence.StartUtc + duration.Value : null))
                .ToList();
        }
        else if (request.EventType == EventType.OneTime)
        {
            occurrences = [EventOccurrence.Create(eventId, request.StartAt.Value, request.EndAt)];
        }
        else
        {
            return 0;
        }

        await unitOfWork.Occurrences.DeleteFutureOccurrencesAsync(eventId, DateTime.UtcNow, cancellationToken);
        await unitOfWork.Occurrences.AddRangeAsync(occurrences, cancellationToken);
        return occurrences.Count;
    }

    private static string Snapshot(Event evt) => JsonSerializer.Serialize(new { evt.Title, evt.RRule, Status = evt.Status.ToString() });
}
