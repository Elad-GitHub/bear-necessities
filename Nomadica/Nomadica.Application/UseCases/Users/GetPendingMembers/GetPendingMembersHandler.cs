using Nomadica.Application.Exceptions;
using Nomadica.Application.Interfaces;
using Nomadica.Domain.Enums;
using Nomadica.Domain.Interfaces;

namespace Nomadica.Application.UseCases.Users.GetPendingMembers;

public sealed class GetPendingMembersHandler : IRequestHandler<GetPendingMembersRequest, GetPendingMembersResponse>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUserService _currentUser;

    public GetPendingMembersHandler(IUnitOfWork unitOfWork, ICurrentUserService currentUser)
    {
        _unitOfWork = unitOfWork;
        _currentUser = currentUser;
    }

    public async Task<GetPendingMembersResponse> HandleAsync(GetPendingMembersRequest request, CancellationToken cancellationToken = default)
    {
        if (!_currentUser.HasRole(UserRole.Admin))
            throw new ForbiddenException();

        var pendingUsers = await _unitOfWork.Users.GetPendingMembershipAsync(cancellationToken);

        var items = pendingUsers
            .Select(user => new PendingMemberDto(
                Id: user.Id.Value,
                Email: user.Email,
                DisplayName: user.DisplayName,
                SocialProfileUrl: user.SocialProfileUrl,
                AppliedAt: user.CreatedAt))
            .ToList();

        return new GetPendingMembersResponse(items);
    }
}
