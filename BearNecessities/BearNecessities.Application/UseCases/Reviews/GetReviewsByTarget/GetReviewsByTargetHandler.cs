using BearNecessities.Application.DTOs.Reviews;
using BearNecessities.Application.Exceptions;
using BearNecessities.Application.Interfaces;
using BearNecessities.Domain.Interfaces;
using BearNecessities.Domain.ValueObjects;

namespace BearNecessities.Application.UseCases.Reviews.GetReviewsByTarget;

public sealed class GetReviewsByTargetHandler : IRequestHandler<GetReviewsByTargetRequest, GetReviewsByTargetResponse>
{
    private readonly IUnitOfWork _unitOfWork;

    public GetReviewsByTargetHandler(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<GetReviewsByTargetResponse> HandleAsync(GetReviewsByTargetRequest request, CancellationToken cancellationToken = default)
    {
        if (request.TargetType != "Place" && request.TargetType != "Event")
            throw new ValidationException(new Dictionary<string, string[]>
            {
                ["targetType"] = ["TargetType must be 'Place' or 'Event'."]
            });

        var pageSize = Math.Clamp(request.PageSize, 1, 100);

        await VerifyTargetExistsAsync(request, cancellationToken);

        var reviews = await _unitOfWork.Reviews.GetByTargetAsync(
            request.TargetType, request.TargetId, pageSize, request.Cursor, cancellationToken);

        var totalCount = await _unitOfWork.Reviews.CountRevealedAsync(
            request.TargetType, request.TargetId, cancellationToken);

        var averageRating = await _unitOfWork.Reviews.GetAverageRatingAsync(
            request.TargetType, request.TargetId, cancellationToken);

        var authorIds = reviews.Select(r => r.AuthorId).Distinct().ToList();
        var authors = await _unitOfWork.Users.GetByIdsAsync(authorIds, cancellationToken);
        var authorMap = authors.ToDictionary(u => u.Id.Value);

        var items = reviews.Select(r =>
        {
            authorMap.TryGetValue(r.AuthorId.Value, out var author);
            return new ReviewDto(
                r.Id, r.AuthorId.Value,
                author?.DisplayName ?? "Unknown",
                author?.AvatarUrl,
                r.Rating, r.Body, r.RevealedAt,
                r.TargetType, r.TargetId);
        }).ToList();

        var nextCursor = items.Count == pageSize ? items[^1].Id : (Guid?)null;

        return new GetReviewsByTargetResponse(items, nextCursor, averageRating, totalCount);
    }

    private async Task VerifyTargetExistsAsync(GetReviewsByTargetRequest request, CancellationToken cancellationToken)
    {
        if (request.TargetType == "Place")
        {
            var place = await _unitOfWork.Places.GetByIdAsync(new PlaceId(request.TargetId), cancellationToken);
            if (place is null) throw new NotFoundException("Place", request.TargetId);
        }
        else
        {
            var evt = await _unitOfWork.Events.GetByIdAsync(new EventId(request.TargetId), cancellationToken);
            if (evt is null) throw new NotFoundException("Event", request.TargetId);
        }
    }
}
