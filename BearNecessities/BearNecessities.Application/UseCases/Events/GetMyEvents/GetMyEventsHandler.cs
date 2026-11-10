using BearNecessities.Application.Common;
using BearNecessities.Application.DTOs.Events;
using BearNecessities.Application.Exceptions;
using BearNecessities.Application.Interfaces;
using BearNecessities.Domain.Enums;
using BearNecessities.Domain.Interfaces;

namespace BearNecessities.Application.UseCases.Events.GetMyEvents;

public sealed class GetMyEventsHandler : IRequestHandler<GetMyEventsRequest, GetMyEventsResponse>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUserService _currentUser;

    public GetMyEventsHandler(IUnitOfWork unitOfWork, ICurrentUserService currentUser)
    {
        _unitOfWork = unitOfWork;
        _currentUser = currentUser;
    }

    public async Task<GetMyEventsResponse> HandleAsync(GetMyEventsRequest request, CancellationToken cancellationToken = default)
    {
        if (!_currentUser.IsAuthenticated || _currentUser.UserId is null)
            throw new UnauthorizedException("Authentication required.");

        var events = await _unitOfWork.Events.GetByOwnerAsync(_currentUser.UserId, cancellationToken);

        var suspendedIds = events
            .Where(e => e.Status == ListingStatus.Suspended)
            .Select(e => e.Id.Value)
            .ToList();

        var rejectionReasons = await GetRejectionReasonsAsync(suspendedIds, cancellationToken);

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
                AverageRating: null,
                RejectionReason: rejectionReasons.GetValueOrDefault(evt.Id.Value)))
            .ToList();

        return new GetMyEventsResponse(items);
    }

    private async Task<Dictionary<Guid, string?>> GetRejectionReasonsAsync(
        List<Guid> suspendedEventIds,
        CancellationToken cancellationToken)
    {
        if (suspendedEventIds.Count == 0) return [];

        var statusChanges = await _unitOfWork.ChangeLogs.GetLatestStatusChangesAsync(
            ListingChangeTargetType.Event, suspendedEventIds, cancellationToken);

        return statusChanges.ToDictionary(
            log => log.TargetId,
            log => ChangeLogReasonParser.ParseReason(log.NewData));
    }
}
