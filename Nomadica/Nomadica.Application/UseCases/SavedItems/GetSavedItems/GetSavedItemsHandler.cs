using Nomadica.Application.DTOs.Events;
using Nomadica.Application.DTOs.Places;
using Nomadica.Application.DTOs.Saved;
using Nomadica.Application.Exceptions;
using Nomadica.Application.Interfaces;
using Nomadica.Domain.Interfaces;
using Nomadica.Domain.ValueObjects;

namespace Nomadica.Application.UseCases.SavedItems.GetSavedItems;

public sealed class GetSavedItemsHandler : IRequestHandler<GetSavedItemsRequest, GetSavedItemsResponse>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUserService _currentUser;

    public GetSavedItemsHandler(IUnitOfWork unitOfWork, ICurrentUserService currentUser)
    {
        _unitOfWork = unitOfWork;
        _currentUser = currentUser;
    }

    public async Task<GetSavedItemsResponse> HandleAsync(
        GetSavedItemsRequest request,
        CancellationToken cancellationToken = default)
    {
        if (!_currentUser.IsAuthenticated || _currentUser.UserId is null)
            throw new UnauthorizedException("Authentication required.");

        var userId = _currentUser.UserId;

        var savedItems = await _unitOfWork.SavedItems.GetByUserAsync(
            userId, request.TargetType, request.PageSize, request.Cursor, cancellationToken);

        if (savedItems.Count == 0)
            return new GetSavedItemsResponse([], null);

        var placeIds = savedItems
            .Where(s => s.TargetType == "Place")
            .Select(s => new PlaceId(s.TargetId))
            .ToList();

        var eventIds = savedItems
            .Where(s => s.TargetType == "Event")
            .Select(s => new EventId(s.TargetId))
            .ToList();

        var placesTask = placeIds.Count > 0
            ? _unitOfWork.Places.GetByIdsAsync(placeIds, cancellationToken)
            : Task.FromResult(new List<Domain.Entities.Place>());

        var eventsTask = eventIds.Count > 0
            ? _unitOfWork.Events.GetByIdsAsync(eventIds, cancellationToken)
            : Task.FromResult(new List<Domain.Entities.Event>());

        await Task.WhenAll(placesTask, eventsTask);

        var placeMap = placesTask.Result.ToDictionary(p => p.Id.Value);
        var eventMap = eventsTask.Result.ToDictionary(e => e.Id.Value);

        var items = savedItems.Select(s =>
        {
            object? summary = s.TargetType switch
            {
                "Place" when placeMap.TryGetValue(s.TargetId, out var place) => new PlaceSummaryDto(
                    place.Id.Value,
                    place.Name,
                    place.Latitude,
                    place.Longitude,
                    place.H3IndexRes8,
                    place.City,
                    place.Country,
                    place.Status,
                    null,
                    0,
                    place.Categories.Select(c => c.Id.Value).ToList(),
                    null,
                    place.IsPermanent),
                "Event" when eventMap.TryGetValue(s.TargetId, out var evt) => new EventSummaryDto(
                    evt.Id.Value,
                    evt.Title,
                    evt.PlaceId?.Value,
                    null,
                    null,
                    evt.StartAt,
                    evt.EndAt,
                    evt.EventType,
                    evt.IsFree,
                    evt.Price,
                    evt.Currency,
                    evt.Status,
                    evt.Categories.Select(c => c.Id.Value).ToList(),
                    null,
                    null),
                _ => null
            };

            return new SavedItemDto(s.TargetType, s.TargetId, s.ItemType.ToString(), s.SavedAt, summary);
        }).ToList();

        Guid? nextCursor = savedItems.Count == request.PageSize ? savedItems[^1].TargetId : null;
        return new GetSavedItemsResponse(items, nextCursor);
    }
}
