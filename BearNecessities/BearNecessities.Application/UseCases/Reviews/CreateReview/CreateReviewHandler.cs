using BearNecessities.Application.Exceptions;
using BearNecessities.Application.Interfaces;
using BearNecessities.Domain.Entities;
using BearNecessities.Domain.Interfaces;
using BearNecessities.Domain.ValueObjects;

namespace BearNecessities.Application.UseCases.Reviews.CreateReview;

public sealed class CreateReviewHandler : IRequestHandler<CreateReviewRequest, CreateReviewResponse>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUserService _currentUser;

    public CreateReviewHandler(IUnitOfWork unitOfWork, ICurrentUserService currentUser)
    {
        _unitOfWork = unitOfWork;
        _currentUser = currentUser;
    }

    public async Task<CreateReviewResponse> HandleAsync(CreateReviewRequest request, CancellationToken cancellationToken = default)
    {
        if (!_currentUser.IsAuthenticated || _currentUser.UserId is null)
            throw new UnauthorizedException("Authentication required.");

        ValidateRequest(request);
        await VerifyTargetExistsAsync(request, cancellationToken);

        var alreadyReviewed = await _unitOfWork.Reviews.ExistsAsync(
            _currentUser.UserId, request.TargetType, request.TargetId, cancellationToken);

        if (alreadyReviewed)
            throw new ValidationException(new Dictionary<string, string[]>
            {
                ["targetId"] = ["You have already reviewed this."]
            });

        var counterpartyId = request.CounterpartyId.HasValue
            ? new UserId(request.CounterpartyId.Value)
            : null;

        var review = Review.Create(
            _currentUser.UserId, request.TargetType, request.TargetId,
            request.Rating, request.Body, counterpartyId);

        review.SetAuthorSubmittedAt();
        review.TryReveal();

        await _unitOfWork.Reviews.AddAsync(review, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return new CreateReviewResponse(review.Id, review.Status.ToString());
    }

    private static void ValidateRequest(CreateReviewRequest request)
    {
        var errors = new Dictionary<string, string[]>();

        if (request.TargetType != "Place" && request.TargetType != "Event")
            errors["targetType"] = ["TargetType must be 'Place' or 'Event'."];

        if (request.Rating < 1 || request.Rating > 5)
            errors["rating"] = ["Rating must be between 1 and 5."];

        if (request.Body is not null && request.Body.Length > 2000)
            errors["body"] = ["Body cannot exceed 2000 characters."];

        if (errors.Count > 0)
            throw new ValidationException(errors);
    }

    private async Task VerifyTargetExistsAsync(CreateReviewRequest request, CancellationToken cancellationToken)
    {
        if (request.TargetType == "Place")
        {
            var place = await _unitOfWork.Places.GetByIdAsync(new PlaceId(request.TargetId), cancellationToken);
            if (place is null)
                throw new NotFoundException("Place", request.TargetId);
        }
        else
        {
            var evt = await _unitOfWork.Events.GetByIdAsync(new EventId(request.TargetId), cancellationToken);
            if (evt is null)
                throw new NotFoundException("Event", request.TargetId);
        }
    }
}
