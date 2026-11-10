using BearNecessities.Application.DTOs.Rsvps;
using BearNecessities.Application.Exceptions;
using BearNecessities.Application.Interfaces;
using BearNecessities.Domain.Interfaces;
using BearNecessities.Domain.ValueObjects;

namespace BearNecessities.Application.UseCases.Rsvps.GetMyRsvps;

public sealed class GetMyRsvpsHandler : IRequestHandler<GetMyRsvpsRequest, GetMyRsvpsResponse>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUserService _currentUser;

    public GetMyRsvpsHandler(IUnitOfWork unitOfWork, ICurrentUserService currentUser)
    {
        _unitOfWork = unitOfWork;
        _currentUser = currentUser;
    }

    public async Task<GetMyRsvpsResponse> HandleAsync(
        GetMyRsvpsRequest request,
        CancellationToken cancellationToken = default)
    {
        if (!_currentUser.IsAuthenticated || _currentUser.UserId is null)
            throw new UnauthorizedException("Authentication required.");

        var userId = _currentUser.UserId;

        var rsvps = await _unitOfWork.Rsvps.GetByUserAsync(
            userId, request.PageSize, request.Cursor, cancellationToken);

        if (rsvps.Count == 0)
            return new GetMyRsvpsResponse([], null);

        var eventIds = rsvps.Select(r => r.EventId).Distinct().ToList();
        var events = await _unitOfWork.Events.GetByIdsAsync(eventIds, cancellationToken);
        var eventMap = events.ToDictionary(e => e.Id);

        var placeIds = events
            .Where(e => e.PlaceId is not null)
            .Select(e => e.PlaceId!)
            .Distinct()
            .ToList();
        var places = placeIds.Count > 0
            ? await _unitOfWork.Places.GetByIdsAsync(placeIds, cancellationToken)
            : new List<Domain.Entities.Place>();
        var placeMap = places.ToDictionary(p => p.Id);

        var items = rsvps.Select(r =>
        {
            eventMap.TryGetValue(r.EventId, out var evt);
            string? placeName = null;
            if (evt?.PlaceId is not null && placeMap.TryGetValue(evt.PlaceId, out var place))
                placeName = place.Name;

            return new MyRsvpDto(
                r.Id,
                r.EventId.Value,
                evt?.Title ?? string.Empty,
                evt?.StartAt,
                placeName,
                r.Status,
                r.PartySize);
        }).ToList();

        Guid? nextCursor = items.Count == request.PageSize ? items[^1].Id : null;
        return new GetMyRsvpsResponse(items, nextCursor);
    }
}
