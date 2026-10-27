using Nomadica.Application.DTOs.Events;
using Nomadica.Application.Exceptions;
using Nomadica.Application.Interfaces;
using Nomadica.Domain.Enums;
using Nomadica.Domain.Interfaces;

namespace Nomadica.Application.UseCases.Events.GetPendingEvents;

public sealed class GetPendingEventsHandler : IRequestHandler<GetPendingEventsRequest, GetPendingEventsResponse>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUserService _currentUser;

    public GetPendingEventsHandler(IUnitOfWork unitOfWork, ICurrentUserService currentUser)
    {
        _unitOfWork = unitOfWork;
        _currentUser = currentUser;
    }

    public async Task<GetPendingEventsResponse> HandleAsync(GetPendingEventsRequest request, CancellationToken cancellationToken = default)
    {
        if (!_currentUser.HasRole(UserRole.Admin) && !_currentUser.HasRole(UserRole.Steward))
            throw new ForbiddenException();

        var events = await _unitOfWork.Events.GetPendingAsync(request.PageSize, request.Cursor, cancellationToken);

        var items = events
            .Select(evt => new EventSummaryDto(
                Id: evt.Id.Value,
                Title: evt.Title,
                PlaceId: evt.PlaceId?.Value,
                Latitude: null,
                Longitude: null,
                StartAt: evt.StartAt,
                EndAt: evt.EndAt,
                EventType: evt.EventType,
                IsFree: evt.IsFree,
                Price: evt.Price,
                Currency: evt.Currency,
                Status: evt.Status,
                CategoryIds: evt.Categories.Select(c => c.Id.Value).ToList(),
                ThumbnailUrl: null,
                AverageRating: null))
            .ToList();

        var nextCursor = events.Count == request.PageSize ? events[^1].Id.Value : (Guid?)null;
        return new GetPendingEventsResponse(items, nextCursor);
    }
}
