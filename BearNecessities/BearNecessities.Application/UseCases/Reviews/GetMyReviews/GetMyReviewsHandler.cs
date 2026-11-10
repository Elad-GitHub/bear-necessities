using BearNecessities.Application.DTOs.Reviews;
using BearNecessities.Application.Exceptions;
using BearNecessities.Application.Interfaces;
using BearNecessities.Domain.Enums;
using BearNecessities.Domain.Interfaces;

namespace BearNecessities.Application.UseCases.Reviews.GetMyReviews;

public sealed class GetMyReviewsHandler : IRequestHandler<GetMyReviewsRequest, GetMyReviewsResponse>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUserService _currentUser;

    public GetMyReviewsHandler(IUnitOfWork unitOfWork, ICurrentUserService currentUser)
    {
        _unitOfWork = unitOfWork;
        _currentUser = currentUser;
    }

    public async Task<GetMyReviewsResponse> HandleAsync(GetMyReviewsRequest request, CancellationToken cancellationToken = default)
    {
        if (!_currentUser.IsAuthenticated || _currentUser.UserId is null)
            throw new UnauthorizedException("Authentication required.");

        var pageSize = Math.Clamp(request.PageSize, 1, 100);

        var currentUser = await _unitOfWork.Users.GetByIdAsync(_currentUser.UserId, cancellationToken)
            ?? throw new NotFoundException("User", _currentUser.UserId.Value);

        var reviews = await _unitOfWork.Reviews.GetByAuthorAsync(
            _currentUser.UserId, pageSize, request.Cursor, cancellationToken);

        var items = reviews.Select(r => new MyReviewDto(
            r.Id, r.AuthorId.Value,
            currentUser.DisplayName,
            currentUser.AvatarUrl,
            r.Rating, r.Body, r.RevealedAt,
            r.TargetType, r.TargetId,
            IsPending: r.Status == ReviewStatus.Pending,
            Status: r.Status.ToString()
        )).ToList();

        var nextCursor = items.Count == pageSize ? items[^1].Id : (Guid?)null;

        return new GetMyReviewsResponse(items, nextCursor);
    }
}
