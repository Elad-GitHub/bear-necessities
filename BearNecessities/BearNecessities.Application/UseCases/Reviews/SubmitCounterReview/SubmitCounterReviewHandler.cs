using BearNecessities.Application.Exceptions;
using BearNecessities.Application.Interfaces;
using BearNecessities.Domain.Entities;
using BearNecessities.Domain.Enums;
using BearNecessities.Domain.Interfaces;

namespace BearNecessities.Application.UseCases.Reviews.SubmitCounterReview;

public sealed class SubmitCounterReviewHandler : IRequestHandler<SubmitCounterReviewRequest, SubmitCounterReviewResponse>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUserService _currentUser;

    public SubmitCounterReviewHandler(IUnitOfWork unitOfWork, ICurrentUserService currentUser)
    {
        _unitOfWork = unitOfWork;
        _currentUser = currentUser;
    }

    public async Task<SubmitCounterReviewResponse> HandleAsync(SubmitCounterReviewRequest request, CancellationToken cancellationToken = default)
    {
        if (!_currentUser.IsAuthenticated || _currentUser.UserId is null)
            throw new UnauthorizedException("Authentication required.");

        if (request.Rating < 1 || request.Rating > 5)
            throw new ValidationException(new Dictionary<string, string[]>
            {
                ["rating"] = ["Rating must be between 1 and 5."]
            });

        if (request.Body is not null && request.Body.Length > 2000)
            throw new ValidationException(new Dictionary<string, string[]>
            {
                ["body"] = ["Body cannot exceed 2000 characters."]
            });

        var original = await _unitOfWork.Reviews.GetByIdAsync(request.OriginalReviewId, cancellationToken)
            ?? throw new NotFoundException("Review", request.OriginalReviewId);

        if (original.CounterpartyId != _currentUser.UserId)
            throw new ForbiddenException();

        if (original.Status != ReviewStatus.Pending)
            throw new ValidationException(new Dictionary<string, string[]>
            {
                ["status"] = ["This review has already been revealed and cannot accept a counter submission."]
            });

        var counter = Review.Create(
            _currentUser.UserId,
            targetType: "User",
            targetId: original.AuthorId.Value,
            rating: request.Rating,
            body: request.Body,
            counterpartyId: original.AuthorId);

        counter.SetAuthorSubmittedAt();
        counter.SetCounterSubmittedAt();

        original.SetCounterSubmittedAt();

        original.TryReveal();
        counter.TryReveal();

        await _unitOfWork.Reviews.AddAsync(counter, cancellationToken);
        await _unitOfWork.Reviews.UpdateAsync(original, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return new SubmitCounterReviewResponse(original.Id, counter.Id, original.Status.ToString());
    }
}
