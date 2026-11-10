using BearNecessities.Application.DTOs.Places;

namespace BearNecessities.Application.UseCases.Places.GetPlaceById;

public record GetPlaceByIdResponse(PlaceDetailDto? Place);
