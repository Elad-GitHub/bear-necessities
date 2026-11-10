namespace BearNecessities.Application.UseCases.Places.RejectPlace;

public record RejectPlaceRequest(Guid PlaceId, string Reason);
