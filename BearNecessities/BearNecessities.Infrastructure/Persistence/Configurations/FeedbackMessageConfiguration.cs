using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using BearNecessities.Domain.Entities;
using BearNecessities.Domain.ValueObjects;

namespace BearNecessities.Infrastructure.Persistence.Configurations;

public class FeedbackMessageConfiguration : IEntityTypeConfiguration<FeedbackMessage>
{
    public void Configure(EntityTypeBuilder<FeedbackMessage> builder)
    {
        builder.ToTable("FeedbackMessages");

        builder.HasKey(f => f.Id);
        builder.Ignore(f => f.IsHandled);

        builder.Property(f => f.Kind).HasConversion<string>().HasMaxLength(20);
        builder.Property(f => f.Message).IsRequired().HasMaxLength(FeedbackMessage.MaxMessageLength);
        builder.Property(f => f.PageUrl).HasMaxLength(FeedbackMessage.MaxPageUrlLength);
        builder.Property(f => f.UserAgent).HasMaxLength(FeedbackMessage.MaxUserAgentLength);
        builder.Property(f => f.ContactEmail).HasMaxLength(FeedbackMessage.MaxEmailLength);

        builder.Property(f => f.SentBy)
            .HasConversion(
                id => id == null ? (Guid?)null : id.Value,
                value => value == null ? null : new UserId(value.Value));
        builder.Property(f => f.HandledBy)
            .HasConversion(
                id => id == null ? (Guid?)null : id.Value,
                value => value == null ? null : new UserId(value.Value));

        builder.Property(f => f.CreatedAt).HasColumnType("datetime2");
        builder.Property(f => f.UpdatedAt).HasColumnType("datetime2");
        builder.Property(f => f.HandledAt).HasColumnType("datetime2");

        builder.HasIndex(f => f.HandledAt);
    }
}
