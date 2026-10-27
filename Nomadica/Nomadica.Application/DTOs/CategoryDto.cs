namespace Nomadica.Application.DTOs;

public record CategoryDto(Guid Id, string Slug, string Name, string? Icon, string? Color);
