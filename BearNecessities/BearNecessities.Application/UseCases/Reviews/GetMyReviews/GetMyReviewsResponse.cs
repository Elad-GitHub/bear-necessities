using BearNecessities.Application.DTOs.Reviews;

namespace BearNecessities.Application.UseCases.Reviews.GetMyReviews;

public record GetMyReviewsResponse(IReadOnlyList<MyReviewDto> Items, Guid? NextCursor);
