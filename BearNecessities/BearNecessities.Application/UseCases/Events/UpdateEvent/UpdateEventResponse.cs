namespace BearNecessities.Application.UseCases.Events.UpdateEvent;

public record UpdateEventResponse(Guid EventId, string Status, int OccurrencesScheduled, bool RequiresReview);
