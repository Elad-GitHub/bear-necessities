using Nomadica.Application.DTOs.Reviews;

namespace Nomadica.Application.UseCases.Reviews.GetMyReviews;

public record GetMyReviewsResponse(IReadOnlyList<MyReviewDto> Items, Guid? NextCursor);
