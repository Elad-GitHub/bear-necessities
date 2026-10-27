using Nomadica.Application.DTOs.Rsvps;
using Nomadica.Application.Exceptions;
using Nomadica.Application.Interfaces;
using Nomadica.Domain.Enums;
using Nomadica.Domain.Interfaces;
using Nomadica.Domain.ValueObjects;

namespace Nomadica.Application.UseCases.Rsvps.GetEventRsvps;

public sealed class GetEventRsvpsHandler : IRequestHandler<GetEventRsvpsRequest, GetEventRsvpsResponse>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUserService _currentUser;

    public GetEventRsvpsHandler(IUnitOfWork unitOfWork, ICurrentUserService currentUser)
    {
        _unitOfWork = unitOfWork;
        _currentUser = currentUser;
    }

    public async Task<GetEventRsvpsResponse> HandleAsync(
        GetEventRsvpsRequest request,
        CancellationToken cancellationToken = default)
    {
        var eventId = new EventId(request.EventId);
        var evt = await _unitOfWork.Events.GetByIdAsync(eventId, cancellationToken)
            ?? throw new NotFoundException("Event", request.EventId);

        var goingCount = await _unitOfWork.Rsvps.GetCountByEventAsync(eventId, RsvpStatus.Going, cancellationToken);
        var maybeCount = await _unitOfWork.Rsvps.GetCountByEventAsync(eventId, RsvpStatus.Maybe, cancellationToken);

        var isOwnerOrAdmin = _currentUser.IsAuthenticated
            && (_currentUser.HasRole(UserRole.Admin) || evt.OwnerId == _currentUser.UserId);

        if (!isOwnerOrAdmin)
            return new GetEventRsvpsResponse([], null, goingCount, maybeCount);

        var rsvps = await _unitOfWork.Rsvps.GetByEventAsync(
            eventId, request.PageSize, request.Cursor, cancellationToken);

        var userIds = rsvps.Select(r => r.UserId).Distinct().ToList();
        var users = await _unitOfWork.Users.GetByIdsAsync(userIds, cancellationToken);
        var userMap = users.ToDictionary(u => u.Id);

        var items = rsvps.Select(r =>
        {
            userMap.TryGetValue(r.UserId, out var user);
            return new RsvpDto(
                r.Id,
                r.UserId.Value,
                user?.DisplayName ?? string.Empty,
                user?.AvatarUrl,
                r.Status,
                r.PartySize);
        }).ToList();

        Guid? nextCursor = items.Count == request.PageSize ? items[^1].Id : null;
        return new GetEventRsvpsResponse(items, nextCursor, goingCount, maybeCount);
    }
}
