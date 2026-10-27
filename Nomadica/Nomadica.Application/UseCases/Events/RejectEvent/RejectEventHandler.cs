using System.Text.Json;
using Nomadica.Application.Exceptions;
using Nomadica.Application.Interfaces;
using Nomadica.Domain.Enums;
using Nomadica.Domain.Interfaces;
using Nomadica.Domain.ValueObjects;

namespace Nomadica.Application.UseCases.Events.RejectEvent;

public sealed class RejectEventHandler : IRequestHandler<RejectEventRequest, bool>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUserService _currentUser;

    public RejectEventHandler(IUnitOfWork unitOfWork, ICurrentUserService currentUser)
    {
        _unitOfWork = unitOfWork;
        _currentUser = currentUser;
    }

    public async Task<bool> HandleAsync(RejectEventRequest request, CancellationToken cancellationToken = default)
    {
        if (!_currentUser.IsAuthenticated || _currentUser.UserId is null)
            throw new UnauthorizedException("Authentication required.");

        if (!_currentUser.HasRole(UserRole.Admin))
            throw new UnauthorizedException("Only admins can reject events.");

        var evt = await _unitOfWork.Events.GetByIdAsync(new EventId(request.EventId), cancellationToken)
            ?? throw new NotFoundException("Event", request.EventId);

        evt.Reject(_currentUser.UserId);
        await _unitOfWork.Events.UpdateAsync(evt, cancellationToken);

        var changeLog = Domain.Entities.ListingChangeLog.Create(
            ListingChangeTargetType.Event,
            evt.Id.Value,
            _currentUser.UserId,
            ChangeType.StatusChange,
            newData: JsonSerializer.Serialize(new { Status = evt.Status.ToString(), request.Reason }));
        await _unitOfWork.ChangeLogs.AddAsync(changeLog, cancellationToken);

        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return true;
    }
}
