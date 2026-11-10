using BearNecessities.Application.Exceptions;
using BearNecessities.Application.Interfaces;
using BearNecessities.Domain.Enums;
using BearNecessities.Domain.Interfaces;
using BearNecessities.Domain.ValueObjects;

namespace BearNecessities.Application.UseCases.Users.ReviewMembership;

public sealed class ReviewMembershipHandler : IRequestHandler<ReviewMembershipRequest, ReviewMembershipResponse>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUserService _currentUser;

    public ReviewMembershipHandler(IUnitOfWork unitOfWork, ICurrentUserService currentUser)
    {
        _unitOfWork = unitOfWork;
        _currentUser = currentUser;
    }

    public async Task<ReviewMembershipResponse> HandleAsync(ReviewMembershipRequest request, CancellationToken cancellationToken = default)
    {
        if (!_currentUser.HasRole(UserRole.Admin))
            throw new ForbiddenException();

        var user = await _unitOfWork.Users.GetByIdAsync(new UserId(request.UserId), cancellationToken)
            ?? throw new NotFoundException("User", request.UserId);

        if (request.Approve)
            user.ApproveMembership();
        else
            user.RejectMembership();

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return new ReviewMembershipResponse(user.Id.Value, user.MembershipStatus.ToString());
    }
}
