namespace BearNecessities.Application.UseCases.Places.GetPendingPlaces;

public record GetPendingPlacesRequest(int PageSize = 20, Guid? Cursor = null);
