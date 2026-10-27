using Nomadica.Application.Common;
using Nomadica.Application.DTOs.Groups;
using Nomadica.Application.Interfaces;
using Nomadica.Domain.Interfaces;
using Nomadica.Domain.ValueObjects;

namespace Nomadica.Application.UseCases.Groups.GetGroups;

public sealed class GetGroupsHandler : IRequestHandler<GetGroupsRequest, GetGroupsResponse>
{
    private const int MaxLimit = 100;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUserService _currentUser;

    public GetGroupsHandler(IUnitOfWork unitOfWork, ICurrentUserService currentUser)
    {
        _unitOfWork = unitOfWork;
        _currentUser = currentUser;
    }

    public async Task<GetGroupsResponse> HandleAsync(GetGroupsRequest request, CancellationToken cancellationToken = default)
    {
        var limit = Math.Clamp(request.Limit, 1, MaxLimit);
        var groups = await _unitOfWork.Groups.SearchAsync(request.Query, limit, cancellationToken);

        var myGroupIds = await GetMyGroupIdsAsync(cancellationToken);

        var items = groups
            .Select(group => GroupMapping.ToSummaryDto(
                group, myGroupIds.Contains(group.Id.Value), myRole: null))
            .ToList();

        return new GetGroupsResponse(items);
    }

    private async Task<HashSet<Guid>> GetMyGroupIdsAsync(CancellationToken cancellationToken)
    {
        if (!_currentUser.IsAuthenticated || _currentUser.UserId is null) return [];

        var groupIds = await _unitOfWork.Groups.GetMyGroupIdsAsync(_currentUser.UserId, cancellationToken);
        return groupIds.Select(id => id.Value).ToHashSet();
    }
}
