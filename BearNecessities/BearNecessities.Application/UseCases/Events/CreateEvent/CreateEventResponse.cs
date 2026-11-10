namespace BearNecessities.Application.UseCases.Events.CreateEvent;

public record CreateEventResponse(Guid EventId, string Status, int OccurrencesScheduled);
