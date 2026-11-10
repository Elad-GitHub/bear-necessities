using BearNecessities.Application.DTOs.Events;

namespace BearNecessities.Application.UseCases.Events.GetEventsByPlace;

public record GetEventsByPlaceResponse(List<EventSummaryDto> Items);
