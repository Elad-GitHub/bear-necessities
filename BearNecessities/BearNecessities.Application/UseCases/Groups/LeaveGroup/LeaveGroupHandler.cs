using BearNecessities.Application.Exceptions;
using BearNecessities.Application.Interfaces;
using BearNecessities.Domain.Interfaces;
using BearNecessities.Domain.ValueObjects;

namespace BearNecessities.Application.UseCases.Groups.LeaveGroup;

public sealed class LeaveGroupHandler : IRequestHandler<LeaveGroupRequest, LeaveGroupResponse>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUserService _currentUser;

    public LeaveGroupHandler(IUnitOfWork unitOfWork, ICurrentUserService currentUser)
    {
        _unitOfWork = unitOfWork;
        _currentUser = currentUser;
    }

    public async Task<LeaveGroupResponse> HandleAsync(LeaveGroupRequest request, CancellationToken cancellationToken = default)
    {
        if (!_currentUser.IsAuthenticated || _currentUser.UserId is null)
            throw new UnauthorizedException("Authentication required.");

        var group = await _unitOfWork.Groups.GetByIdAsync(new GroupId(request.GroupId), cancellationToken)
            ?? throw new NotFoundException("Group", request.GroupId);

        var membership = await _unitOfWork.Groups.GetMembershipAsync(group.Id, _currentUser.UserId, cancellationToken);
        if (membership is null)
            return new LeaveGroupResponse(false, group.MemberCount);

        _unitOfWork.Groups.RemoveMembership(membership);
        group.DecrementMemberCount();

        await _unitOfWork.Groups.UpdateAsync(group, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return new LeaveGroupResponse(false, group.MemberCount);
    }
}
