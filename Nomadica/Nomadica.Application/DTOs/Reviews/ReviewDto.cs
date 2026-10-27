namespace Nomadica.Application.DTOs.Reviews;

public record ReviewDto(
    Guid Id,
    Guid AuthorId,
    string AuthorDisplayName,
    string? AuthorAvatarUrl,
    int Rating,
    string? Body,
    DateTime? RevealedAt,
    string TargetType,
    Guid TargetId);
