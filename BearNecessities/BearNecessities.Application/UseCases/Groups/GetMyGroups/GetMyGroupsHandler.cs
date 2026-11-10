using BearNecessities.Application.Common;
using BearNecessities.Application.Exceptions;
using BearNecessities.Application.Interfaces;
using BearNecessities.Domain.Interfaces;

namespace BearNecessities.Application.UseCases.Groups.GetMyGroups;

public sealed class GetMyGroupsHandler : IRequestHandler<GetMyGroupsRequest, GetMyGroupsResponse>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUserService _currentUser;

    public GetMyGroupsHandler(IUnitOfWork unitOfWork, ICurrentUserService currentUser)
    {
        _unitOfWork = unitOfWork;
        _currentUser = currentUser;
    }

    public async Task<GetMyGroupsResponse> HandleAsync(GetMyGroupsRequest request, CancellationToken cancellationToken = default)
    {
        if (!_currentUser.IsAuthenticated || _currentUser.UserId is null)
            throw new UnauthorizedException("Authentication required.");

        var groups = await _unitOfWork.Groups.GetMyGroupsAsync(_currentUser.UserId, cancellationToken);

        var items = groups
            .Select(group => GroupMapping.ToSummaryDto(group, isMember: true, myRole: null))
            .ToList();

        return new GetMyGroupsResponse(items);
    }
}
