using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Nomadica.Domain.Entities;
using Nomadica.Domain.ValueObjects;

namespace Nomadica.Infrastructure.Persistence.Configurations;

public class ReportConfiguration : IEntityTypeConfiguration<Report>
{
    public void Configure(EntityTypeBuilder<Report> builder)
    {
        builder.ToTable("Reports");

        builder.HasKey(r => r.Id);

        builder.Property(r => r.ReporterId)
            .IsRequired()
            .HasConversion(id => id.Value, value => new UserId(value));

        builder.Property(r => r.ReviewedBy)
            .HasConversion(
                id => id == null ? (Guid?)null : id.Value,
                value => value == null ? null : new UserId(value.Value));

        builder.Property(r => r.TargetType).IsRequired().HasMaxLength(50);
        builder.Property(r => r.Details).HasMaxLength(2000);

        builder.Property(r => r.CreatedAt).HasColumnType("datetime2");
        builder.Property(r => r.UpdatedAt).HasColumnType("datetime2");

        builder.HasIndex(r => new { r.TargetType, r.TargetId });
        builder.HasIndex(r => r.Status);
    }
}
