using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using BearNecessities.Domain.Entities;
using BearNecessities.Domain.ValueObjects;

namespace BearNecessities.Infrastructure.Persistence.Configurations;

public class EditSuggestionConfiguration : IEntityTypeConfiguration<EditSuggestion>
{
    public void Configure(EntityTypeBuilder<EditSuggestion> builder)
    {
        builder.ToTable("EditSuggestions");

        builder.HasKey(s => s.Id);

        builder.Property(s => s.SuggestedBy)
            .IsRequired()
            .HasConversion(id => id.Value, value => new UserId(value));

        builder.Property(s => s.ReviewedBy)
            .HasConversion(
                id => id == null ? (Guid?)null : id.Value,
                value => value == null ? null : new UserId(value.Value));

        builder.Property(s => s.ProposedChanges).IsRequired();

        builder.Property(s => s.CreatedAt).HasColumnType("datetime2");
        builder.Property(s => s.UpdatedAt).HasColumnType("datetime2");
        builder.Property(s => s.ReviewedAt).HasColumnType("datetime2");

        builder.HasIndex(s => s.Status);
        builder.HasIndex(s => new { s.TargetType, s.TargetId });
    }
}
