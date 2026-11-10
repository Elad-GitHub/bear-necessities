using System.Text.Json;
using BearNecessities.Application.Exceptions;
using BearNecessities.Application.Interfaces;
using BearNecessities.Domain.Enums;
using BearNecessities.Domain.Interfaces;
using BearNecessities.Domain.ValueObjects;

namespace BearNecessities.Application.UseCases.Events.ApproveEvent;

public sealed class ApproveEventHandler : IRequestHandler<ApproveEventRequest, bool>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUserService _currentUser;

    public ApproveEventHandler(IUnitOfWork unitOfWork, ICurrentUserService currentUser)
    {
        _unitOfWork = unitOfWork;
        _currentUser = currentUser;
    }

    public async Task<bool> HandleAsync(ApproveEventRequest request, CancellationToken cancellationToken = default)
    {
        if (!_currentUser.IsAuthenticated || _currentUser.UserId is null)
            throw new UnauthorizedException("Authentication required.");

        if (!_currentUser.HasRole(UserRole.Admin))
            throw new UnauthorizedException("Only admins can approve events.");

        var evt = await _unitOfWork.Events.GetByIdAsync(new EventId(request.EventId), cancellationToken)
            ?? throw new NotFoundException("Event", request.EventId);

        evt.Approve(_currentUser.UserId);
        await _unitOfWork.Events.UpdateAsync(evt, cancellationToken);

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
