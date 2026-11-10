using System.Text.Json;
using BearNecessities.Application.Exceptions;
using BearNecessities.Application.Interfaces;
using BearNecessities.Domain.Enums;
using BearNecessities.Domain.Interfaces;
using BearNecessities.Domain.ValueObjects;

namespace BearNecessities.Application.UseCases.Events.UpdateEvent;

public sealed class UpdateEventHandler : IRequestHandler<UpdateEventRequest, UpdateEventResponse>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUserService _currentUser;
    private readonly IOccurrenceExpander _expander;

    public UpdateEventHandler(IUnitOfWork unitOfWork, ICurrentUserService currentUser, IOccurrenceExpander expander)
    {
        _unitOfWork = unitOfWork;
        _currentUser = currentUser;
        _expander = expander;
    }

    public async Task<UpdateEventResponse> HandleAsync(UpdateEventRequest request, CancellationToken cancellationToken = default)
    {
        if (!_currentUser.IsAuthenticated || _currentUser.UserId is null)
            throw new UnauthorizedException("Authentication required.");

        Validate(request);

        var eventId = new EventId(request.EventId);
        var evt = await _unitOfWork.Events.GetByIdWithRelationsAsync(eventId, cancellationToken)
            ?? throw new NotFoundException("Event", request.EventId);

        var isAdmin = _currentUser.HasRole(UserRole.Admin);
        if (evt.OwnerId != _currentUser.UserId && !isAdmin)
            throw new UnauthorizedException("You do not have permission to update this event.");

        if (evt.Status == ListingStatus.Cancelled || evt.Status == ListingStatus.Archived)
            throw new ValidationException(new Dictionary<string, string[]>
            {
                ["status"] = ["Cannot update a Cancelled or Archived event."]
            });

        if (request.PlaceId.HasValue)
        {
            var place = await _unitOfWork.Places.GetByIdAsync(new PlaceId(request.PlaceId.Value), cancellationToken)
                ?? throw new NotFoundException("Place", request.PlaceId.Value);

            if (place.Status != ListingStatus.Live)
                throw new ValidationException(new Dictionary<string, string[]>
                {
                    ["placeId"] = ["The referenced place must be Live."]
                });
        }

        var allCategories = await _unitOfWork.Categories.GetAllAsync(cancellationToken);
        var categoryIds = request.CategoryIds.Select(id => new CategoryId(id)).ToList();
        var foundCategories = allCategories
            .Where(c => categoryIds.Any(id => id == c.Id))
            .ToList();

        if (foundCategories.Count != categoryIds.Count)
        {
            var missing = categoryIds
                .Where(id => foundCategories.All(c => c.Id != id))
                .Select(id => id.Value.ToString());
            throw new ValidationException(new Dictionary<string, string[]>
            {
                ["categoryIds"] = [$"Categories not found: {string.Join(", ", missing)}."]
            });
        }

        var oldData = JsonSerializer.Serialize(new { evt.Title, evt.RRule, Status = evt.Status.ToString() });

        bool rruleChanged = evt.RRule != request.RRule;
        bool startChanged = evt.StartAt != request.StartAt;
        bool eventTypeChanged = evt.EventType != request.EventType;

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

        bool isMajorChange = rruleChanged || startChanged || eventTypeChanged;
        bool requiresReview = isMajorChange && evt.Status == ListingStatus.Live;
        if (requiresReview)
            evt.RequestReview();

        await _unitOfWork.Events.UpdateAsync(evt, cancellationToken);

        var occurrencesCreated = 0;
        if (rruleChanged || startChanged)
        {
            if (request.EventType == EventType.Recurring && request.RRule is not null && request.StartAt.HasValue)
            {
                await _unitOfWork.Occurrences.DeleteFutureOccurrencesAsync(eventId, DateTime.UtcNow, cancellationToken);

                var expanded = _expander.ExpandOccurrences(request.RRule, request.StartAt.Value, request.Timezone, daysAhead: 90);
                TimeSpan? duration = request.EndAt.HasValue ? request.EndAt.Value - request.StartAt.Value : null;

                var occurrences = expanded.Select(o =>
                {
                    var endAt = duration.HasValue ? (DateTime?)(o.StartUtc + duration.Value) : null;
                    return Domain.Entities.EventOccurrence.Create(eventId, o.StartUtc, endAt);
                }).ToList();

                await _unitOfWork.Occurrences.AddRangeAsync(occurrences, cancellationToken);
                occurrencesCreated = occurrences.Count;
            }
            else if (request.EventType == EventType.OneTime && request.StartAt.HasValue)
            {
                await _unitOfWork.Occurrences.DeleteFutureOccurrencesAsync(eventId, DateTime.UtcNow, cancellationToken);
                var occurrence = Domain.Entities.EventOccurrence.Create(eventId, request.StartAt.Value, request.EndAt);
                await _unitOfWork.Occurrences.AddRangeAsync([occurrence], cancellationToken);
                occurrencesCreated = 1;
            }
        }

        var changeLog = Domain.Entities.ListingChangeLog.Create(
            ListingChangeTargetType.Event,
            evt.Id.Value,
            _currentUser.UserId,
            isMajorChange ? ChangeType.MajorEdit : ChangeType.MinorEdit,
            oldData: oldData,
            newData: JsonSerializer.Serialize(new { evt.Title, evt.RRule, Status = evt.Status.ToString() }));
        await _unitOfWork.ChangeLogs.AddAsync(changeLog, cancellationToken);

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return new UpdateEventResponse(evt.Id.Value, evt.Status.ToString(), occurrencesCreated, requiresReview);
    }

    private static void Validate(UpdateEventRequest request)
    {
        var errors = new Dictionary<string, string[]>();

        if (string.IsNullOrWhiteSpace(request.Title))
            errors["title"] = ["Title is required."];
        else if (request.Title.Length > 200)
            errors["title"] = ["Title must not exceed 200 characters."];

        if (request.Description?.Length > 5000)
            errors["description"] = ["Description must not exceed 5000 characters."];

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
        else if (request.CategoryIds.Count > 5)
            errors["categoryIds"] = ["An event cannot have more than 5 categories."];

        if (errors.Count > 0)
            throw new ValidationException(errors);
    }
}
