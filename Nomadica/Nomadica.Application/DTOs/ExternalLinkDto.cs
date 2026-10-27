namespace Nomadica.Application.DTOs;

public record ExternalLinkDto(Guid Id, string Label, string Url, int SortOrder);
