using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using BearNecessities.Domain.Entities;
using BearNecessities.Domain.ValueObjects;

namespace BearNecessities.Infrastructure.Persistence.Configurations;

public class ReviewConfiguration : IEntityTypeConfiguration<Review>
{
    public void Configure(EntityTypeBuilder<Review> builder)
    {
        builder.ToTable("Reviews");

        builder.HasKey(r => r.Id);

        builder.Property(r => r.AuthorId)
            .IsRequired()
            .HasConversion(id => id.Value, value => new UserId(value));

        builder.Property(r => r.CounterpartyId)
            .HasConversion(
                id => id == null ? (Guid?)null : id.Value,
                value => value == null ? null : new UserId(value.Value));

        builder.Property(r => r.TargetType).IsRequired().HasMaxLength(50);
        builder.Property(r => r.Body).HasMaxLength(2000);

        builder.Property(r => r.CreatedAt).HasColumnType("datetime2");
        builder.Property(r => r.UpdatedAt).HasColumnType("datetime2");
        builder.Property(r => r.AuthorSubmittedAt).HasColumnType("datetime2");
        builder.Property(r => r.CounterSubmittedAt).HasColumnType("datetime2");
        builder.Property(r => r.RevealedAt).HasColumnType("datetime2");

        builder.HasIndex(r => new { r.TargetType, r.TargetId });
        builder.HasIndex(r => r.AuthorId);
    }
}
