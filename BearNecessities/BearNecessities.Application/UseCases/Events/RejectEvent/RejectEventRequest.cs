namespace BearNecessities.Application.UseCases.Events.RejectEvent;

public record RejectEventRequest(Guid EventId, string? Reason);
