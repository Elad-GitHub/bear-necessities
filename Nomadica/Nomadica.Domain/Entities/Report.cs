using Nomadica.Domain.Enums;
using Nomadica.Domain.ValueObjects;

namespace Nomadica.Domain.Entities;

public sealed class Report : BaseEntity<Guid>
{
    private Report() { }

    public UserId ReporterId { get; private set; } = default!;
    public string TargetType { get; private set; } = default!;
    public Guid TargetId { get; private set; }
    public ReportReason Reason { get; private set; }
    public string? Details { get; private set; }
    public ReportStatus Status { get; private set; }
    public UserId? ReviewedBy { get; private set; }

    public static Report Create(UserId reporterId, string targetType, Guid targetId, ReportReason reason, string? details = null)
    {
        if (string.IsNullOrWhiteSpace(targetType))
            throw new ArgumentException("Target type cannot be empty.", nameof(targetType));

        return new Report
        {
            Id = Guid.NewGuid(),
            ReporterId = reporterId,
            TargetType = targetType,
            TargetId = targetId,
            Reason = reason,
            Details = details,
            Status = ReportStatus.Open
        };
    }
}
