using Nomadica.Application.Common;
using Nomadica.Application.DTOs.Groups;
using Nomadica.Application.Exceptions;
using Nomadica.Application.Interfaces;
using Nomadica.Domain.Entities;
using Nomadica.Domain.Interfaces;
using Nomadica.Domain.ValueObjects;

namespace Nomadica.Application.UseCases.Groups.GetGroupById;

public sealed class GetGroupByIdHandler : IRequestHandler<GetGroupByIdRequest, GroupSummaryDto>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUserService _currentUser;

    public GetGroupByIdHandler(IUnitOfWork unitOfWork, ICurrentUserService currentUser)
    {
        _unitOfWork = unitOfWork;
        _currentUser = currentUser;
    }

    public async Task<GroupSummaryDto> HandleAsync(GetGroupByIdRequest request, CancellationToken cancellationToken = default)
    {
        var group = await _unitOfWork.Groups.GetByIdAsync(new GroupId(request.GroupId), cancellationToken)
            ?? throw new NotFoundException("Group", request.GroupId);

        GroupMembership? membership = null;
        if (_currentUser.IsAuthenticated && _currentUser.UserId is not null)
        {
            membership = await _unitOfWork.Groups.GetMembershipAsync(
                group.Id, _currentUser.UserId, cancellationToken);
        }

        return GroupMapping.ToSummaryDto(group, membership);
    }
}
