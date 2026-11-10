namespace BearNecessities.Application.UseCases.Places.UpdatePlace;

public record UpdatePlaceResponse(Guid PlaceId, string Status, bool RequiresReview);
