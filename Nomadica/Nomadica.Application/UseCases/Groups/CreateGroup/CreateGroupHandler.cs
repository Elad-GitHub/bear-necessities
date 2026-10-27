using Nomadica.Application.Common;
using Nomadica.Application.Exceptions;
using Nomadica.Application.Interfaces;
using Nomadica.Domain.Entities;
using Nomadica.Domain.Enums;
using Nomadica.Domain.Interfaces;

namespace Nomadica.Application.UseCases.Groups.CreateGroup;

public sealed class CreateGroupHandler : IRequestHandler<CreateGroupRequest, CreateGroupResponse>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUserService _currentUser;

    public CreateGroupHandler(IUnitOfWork unitOfWork, ICurrentUserService currentUser)
    {
        _unitOfWork = unitOfWork;
        _currentUser = currentUser;
    }

    public async Task<CreateGroupResponse> HandleAsync(CreateGroupRequest request, CancellationToken cancellationToken = default)
    {
        if (!_currentUser.IsAuthenticated || _currentUser.UserId is null)
            throw new UnauthorizedException("Authentication required.");

        ValidateRequest(request);

        var group = CommunityGroup.Create(
            request.Name,
            request.Latitude,
            request.Longitude,
            _currentUser.UserId,
            request.Description,
            request.City,
            request.Country);

        // The founder moderates their own group
        var founderMembership = GroupMembership.Create(group.Id, _currentUser.UserId, GroupRole.Moderator);
        group.IncrementMemberCount();

        await _unitOfWork.Groups.AddAsync(group, cancellationToken);
        await _unitOfWork.Groups.AddMembershipAsync(founderMembership, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return new CreateGroupResponse(GroupMapping.ToSummaryDto(group, founderMembership));
    }

    private static void ValidateRequest(CreateGroupRequest request)
    {
        var errors = new Dictionary<string, string[]>();

        if (string.IsNullOrWhiteSpace(request.Name) || request.Name.Trim().Length < 3)
            errors["name"] = ["Name must be at least 3 characters."];

        if (request.Latitude < -90 || request.Latitude > 90)
            errors["latitude"] = ["Latitude must be between -90 and 90."];

        if (request.Longitude < -180 || request.Longitude > 180)
            errors["longitude"] = ["Longitude must be between -180 and 180."];

        if (errors.Count > 0)
            throw new ValidationException(errors);
    }
}
