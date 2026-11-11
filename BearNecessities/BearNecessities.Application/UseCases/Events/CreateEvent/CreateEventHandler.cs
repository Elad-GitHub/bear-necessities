using System.Text.Json;
using BearNecessities.Application.Exceptions;
using BearNecessities.Application.Interfaces;
using BearNecessities.Domain.Entities;
using BearNecessities.Domain.Enums;
using BearNecessities.Domain.Interfaces;
using BearNecessities.Domain.ValueObjects;

namespace BearNecessities.Application.UseCases.Events.CreateEvent;

public sealed class CreateEventHandler : IRequestHandler<CreateEventRequest, CreateEventResponse>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUserService _currentUser;
    private readonly IOccurrenceExpander _expander;

    public CreateEventHandler(IUnitOfWork unitOfWork, ICurrentUserService currentUser, IOccurrenceExpander expander)
    {
        _unitOfWork = unitOfWork;
        _currentUser = currentUser;
        _expander = expander;
    }

    public async Task<CreateEventResponse> HandleAsync(CreateEventRequest request, CancellationToken cancellationToken = default)
    {
        if (!_currentUser.IsAuthenticated || _currentUser.UserId is null)
            throw new UnauthorizedException("Authentication required.");

        Validate(request);

        Place? place = null;
        if (request.PlaceId.HasValue)
        {
            place = await _unitOfWork.Places.GetByIdAsync(new PlaceId(request.PlaceId.Value), cancellationToken)
                ?? throw new NotFoundException("Place", request.PlaceId.Value);

            if (place.Status != ListingStatus.Live)
                throw new ValidationException(new Dictionary<string, string[]>
                {
                    ["placeId"] = ["The referenced place must be Live."]
                });
        }

        var categoryIds = request.CategoryIds.Select(id => new CategoryId(id)).ToList();
        var allCategories = await _unitOfWork.Categories.GetAllAsync(cancellationToken);
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

        var evt = Event.Create(request.Title, request.EventType, request.Timezone, _currentUser.UserId);
        evt.SetDescription(request.Description);
        evt.SetStartAt(request.StartAt);
        evt.SetEndAt(request.EndAt);
        evt.SetRRule(request.RRule);
        evt.SetPrice(request.Price, request.Currency, request.IsFree);
        evt.SetIsOnline(request.IsOnline, request.OnlineUrl);
        evt.SetMaxCapacity(request.MaxCapacity);

        if (request.PlaceId.HasValue)
            evt.SetPlaceId(new PlaceId(request.PlaceId.Value));

        foreach (var category in foundCategories)
            evt.AddCategory(category);

        evt.Submit();
        // Admins are the reviewers, so their own listings skip the queue
        if (_currentUser.HasRole(UserRole.Admin)) evt.Approve(_currentUser.UserId);

        await _unitOfWork.Events.AddAsync(evt, cancellationToken);

        var occurrencesCreated = 0;
        if (request.EventType == EventType.Recurring && request.RRule is not null && request.StartAt.HasValue)
        {
            var expanded = _expander.ExpandOccurrences(request.RRule, request.StartAt.Value, request.Timezone, daysAhead: 90);
            TimeSpan? duration = request.EndAt.HasValue ? request.EndAt.Value - request.StartAt.Value : null;

            var occurrences = expanded.Select(o =>
            {
                var endAt = duration.HasValue ? (DateTime?)(o.StartUtc + duration.Value) : null;
                return EventOccurrence.Create(evt.Id, o.StartUtc, endAt);
            }).ToList();

            await _unitOfWork.Occurrences.AddRangeAsync(occurrences, cancellationToken);
            occurrencesCreated = occurrences.Count;
        }
        else if (request.EventType == EventType.OneTime && request.StartAt.HasValue)
        {
            var occurrence = EventOccurrence.Create(evt.Id, request.StartAt.Value, request.EndAt);
            await _unitOfWork.Occurrences.AddRangeAsync([occurrence], cancellationToken);
            occurrencesCreated = 1;
        }

        var changeLog = ListingChangeLog.Create(
            ListingChangeTargetType.Event,
            evt.Id.Value,
            _currentUser.UserId,
            ChangeType.StatusChange,
            newData: JsonSerializer.Serialize(new { evt.Title, Status = evt.Status.ToString() }));
        await _unitOfWork.ChangeLogs.AddAsync(changeLog, cancellationToken);

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return new CreateEventResponse(evt.Id.Value, evt.Status.ToString(), occurrencesCreated);
    }

    private static void Validate(CreateEventRequest request)
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
        else if (request.Timezone.Length > 100)
            errors["timezone"] = ["Timezone must not exceed 100 characters."];

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
