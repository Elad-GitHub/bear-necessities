namespace BearNecessities.Application.UseCases.Places.SearchPlaces;

public record SearchPlacesRequest(string Query, int Limit = 8);
