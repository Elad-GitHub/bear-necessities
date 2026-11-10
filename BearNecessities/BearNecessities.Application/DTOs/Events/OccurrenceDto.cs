namespace BearNecessities.Application.DTOs.Events;

public record OccurrenceDto(Guid Id, DateTime StartAt, DateTime? EndAt, string Status);
