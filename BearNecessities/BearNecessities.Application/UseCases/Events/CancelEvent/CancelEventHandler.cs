using System.Text.Json;
using BearNecessities.Application.Exceptions;
using BearNecessities.Application.Interfaces;
using BearNecessities.Domain.Enums;
using BearNecessities.Domain.Interfaces;
using BearNecessities.Domain.ValueObjects;

namespace BearNecessities.Application.UseCases.Events.CancelEvent;

public sealed class CancelEventHandler : IRequestHandler<CancelEventRequest, bool>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUserService _currentUser;

    public CancelEventHandler(IUnitOfWork unitOfWork, ICurrentUserService currentUser)
    {
        _unitOfWork = unitOfWork;
        _currentUser = currentUser;
    }

    public async Task<bool> HandleAsync(CancelEventRequest request, CancellationToken cancellationToken = default)
    {
        if (!_currentUser.IsAuthenticated || _currentUser.UserId is null)
            throw new UnauthorizedException("Authentication required.");

        var eventId = new EventId(request.EventId);
        var evt = await _unitOfWork.Events.GetByIdAsync(eventId, cancellationToken)
            ?? throw new NotFoundException("Event", request.EventId);

        var isAdmin = _currentUser.HasRole(UserRole.Admin);
        if (evt.OwnerId != _currentUser.UserId && !isAdmin)
            throw new UnauthorizedException("You do not have permission to cancel this event.");

        evt.Cancel();
        await _unitOfWork.Events.UpdateAsync(evt, cancellationToken);

        await _unitOfWork.Occurrences.CancelFutureOccurrencesAsync(eventId, DateTime.UtcNow, cancellationToken);

        var changeLog = Domain.Entities.ListingChangeLog.Create(
            ListingChangeTargetType.Event,
            evt.Id.Value,
            _currentUser.UserId,
            ChangeType.StatusChange,
            newData: JsonSerializer.Serialize(new { Status = evt.Status.ToString() }));
        await _unitOfWork.ChangeLogs.AddAsync(changeLog, cancellationToken);

        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return true;
    }
}
