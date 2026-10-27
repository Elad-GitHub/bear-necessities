using Nomadica.Application.Exceptions;
using Nomadica.Application.Interfaces;
using Nomadica.Domain.Entities;
using Nomadica.Domain.Enums;
using Nomadica.Domain.Interfaces;

namespace Nomadica.Application.UseCases.Reviews.FlagReview;

public sealed class FlagReviewHandler : IRequestHandler<FlagReviewRequest, bool>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUserService _currentUser;

    public FlagReviewHandler(IUnitOfWork unitOfWork, ICurrentUserService currentUser)
    {
        _unitOfWork = unitOfWork;
        _currentUser = currentUser;
    }

    public async Task<bool> HandleAsync(FlagReviewRequest request, CancellationToken cancellationToken = default)
    {
        if (!_currentUser.IsAuthenticated || _currentUser.UserId is null)
            throw new UnauthorizedException("Authentication required.");

        if (string.IsNullOrWhiteSpace(request.Reason))
            throw new ValidationException(new Dictionary<string, string[]>
            {
                ["reason"] = ["Reason is required."]
            });

        if (request.Reason.Length > 500)
            throw new ValidationException(new Dictionary<string, string[]>
            {
                ["reason"] = ["Reason cannot exceed 500 characters."]
            });

        var review = await _unitOfWork.Reviews.GetByIdAsync(request.ReviewId, cancellationToken)
            ?? throw new NotFoundException("Review", request.ReviewId);

        review.Flag();

        var report = Report.Create(
            _currentUser.UserId, "Review", review.Id, ReportReason.Other, request.Reason);

        await _unitOfWork.Reviews.UpdateAsync(review, cancellationToken);
        await _unitOfWork.Reports.AddAsync(report, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return true;
    }
}
