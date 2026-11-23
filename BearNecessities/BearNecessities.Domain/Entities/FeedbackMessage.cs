using BearNecessities.Domain.Enums;
using BearNecessities.Domain.ValueObjects;

namespace BearNecessities.Domain.Entities;

/// <summary>
/// A bug report, idea or other message sent from inside the app. Signed-out visitors can send one too,
/// leaving an email if they want an answer; admins read them and mark them handled.
/// </summary>
public sealed class FeedbackMessage : BaseEntity<Guid>
{
    public const int MaxMessageLength = 2000;
    public const int MaxPageUrlLength = 500;
    public const int MaxUserAgentLength = 300;
    public const int MaxEmailLength = 256;

    private FeedbackMessage() { }

    public FeedbackKind Kind { get; private set; }
    public string Message { get; private set; } = default!;
    public string? PageUrl { get; private set; }
    public string? UserAgent { get; private set; }
    public string? ContactEmail { get; private set; }
    public UserId? SentBy { get; private set; }
    public DateTime? HandledAt { get; private set; }
    public UserId? HandledBy { get; private set; }

    public bool IsHandled => HandledAt is not null;

    public static FeedbackMessage Create(
        FeedbackKind kind, string message, string? pageUrl, string? userAgent, string? contactEmail, UserId? sentBy)
    {
        if (string.IsNullOrWhiteSpace(message))
            throw new ArgumentException("Feedback message cannot be empty.", nameof(message));

        return new FeedbackMessage
        {
            Id = Guid.NewGuid(),
            Kind = kind,
            Message = Truncate(message.Trim(), MaxMessageLength)!,
            PageUrl = Truncate(pageUrl, MaxPageUrlLength),
            UserAgent = Truncate(userAgent, MaxUserAgentLength),
            ContactEmail = Truncate(contactEmail?.Trim(), MaxEmailLength),
            SentBy = sentBy
        };
    }

    public void MarkHandled(UserId adminId)
    {
        if (IsHandled) return;
        HandledAt = DateTime.UtcNow;
        HandledBy = adminId;
    }

    private static string? Truncate(string? text, int maxLength) =>
        string.IsNullOrWhiteSpace(text) ? null : text.Length <= maxLength ? text : text[..maxLength];
}
