using Nomadica.Application.Exceptions;
using Nomadica.Application.Interfaces;
using Nomadica.Domain.Entities;
using Nomadica.Domain.Interfaces;
using Nomadica.Domain.ValueObjects;

namespace Nomadica.Application.UseCases.Groups.JoinGroup;

public sealed class JoinGroupHandler : IRequestHandler<JoinGroupRequest, JoinGroupResponse>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUserService _currentUser;

    public JoinGroupHandler(IUnitOfWork unitOfWork, ICurrentUserService currentUser)
    {
        _unitOfWork = unitOfWork;
        _currentUser = currentUser;
    }

    public async Task<JoinGroupResponse> HandleAsync(JoinGroupRequest request, CancellationToken cancellationToken = default)
    {
        if (!_currentUser.IsAuthenticated || _currentUser.UserId is null)
            throw new UnauthorizedException("Authentication required.");

        var group = await _unitOfWork.Groups.GetByIdAsync(new GroupId(request.GroupId), cancellationToken)
            ?? throw new NotFoundException("Group", request.GroupId);

        var existing = await _unitOfWork.Groups.GetMembershipAsync(group.Id, _currentUser.UserId, cancellationToken);
        if (existing is not null)
            return new JoinGroupResponse(true, group.MemberCount);

        var membership = GroupMembership.Create(group.Id, _currentUser.UserId);
        group.IncrementMemberCount();

        await _unitOfWork.Groups.AddMembershipAsync(membership, cancellationToken);
        await _unitOfWork.Groups.UpdateAsync(group, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return new JoinGroupResponse(true, group.MemberCount);
    }
}
