namespace Nomadica.Application.UseCases.Reviews.GetMyReviews;

public record GetMyReviewsRequest(int PageSize = 10, Guid? Cursor = null);
