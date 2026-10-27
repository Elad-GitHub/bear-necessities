using Nomadica.Application.Exceptions;
using Nomadica.Application.Interfaces;
using Nomadica.Domain.Enums;
using Nomadica.Domain.Interfaces;

namespace Nomadica.Application.UseCases.Reviews.AdminRemoveReview;

public sealed class AdminRemoveReviewHandler : IRequestHandler<AdminRemoveReviewRequest, bool>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUserService _currentUser;

    public AdminRemoveReviewHandler(IUnitOfWork unitOfWork, ICurrentUserService currentUser)
    {
        _unitOfWork = unitOfWork;
        _currentUser = currentUser;
    }

    public async Task<bool> HandleAsync(AdminRemoveReviewRequest request, CancellationToken cancellationToken = default)
    {
        if (!_currentUser.IsAuthenticated || _currentUser.UserId is null)
            throw new UnauthorizedException("Authentication required.");

        if (!_currentUser.HasRole(UserRole.Admin))
            throw new ForbiddenException();

        var review = await _unitOfWork.Reviews.GetByIdAsync(request.ReviewId, cancellationToken)
            ?? throw new NotFoundException("Review", request.ReviewId);

        review.Remove();

        await _unitOfWork.Reviews.UpdateAsync(review, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return true;
    }
}
