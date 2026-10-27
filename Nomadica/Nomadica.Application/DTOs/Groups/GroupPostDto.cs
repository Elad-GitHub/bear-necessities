namespace Nomadica.Application.DTOs.Groups;

public record GroupPostDto(
    Guid Id,
    Guid AuthorId,
    string AuthorDisplayName,
    string? AuthorAvatarUrl,
    string Body,
    DateTime CreatedAt);
