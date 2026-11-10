namespace BearNecessities.Application.DTOs.Reviews;

public record MyReviewDto(
    Guid Id,
    Guid AuthorId,
    string AuthorDisplayName,
    string? AuthorAvatarUrl,
    int Rating,
    string? Body,
    DateTime? RevealedAt,
    string TargetType,
    Guid TargetId,
    bool IsPending,
    string Status);
