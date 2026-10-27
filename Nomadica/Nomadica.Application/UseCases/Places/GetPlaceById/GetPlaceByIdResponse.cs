using Nomadica.Application.DTOs.Places;

namespace Nomadica.Application.UseCases.Places.GetPlaceById;

public record GetPlaceByIdResponse(PlaceDetailDto? Place);
