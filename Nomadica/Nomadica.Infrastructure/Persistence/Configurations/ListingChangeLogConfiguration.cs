using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Nomadica.Domain.Entities;
using Nomadica.Domain.ValueObjects;

namespace Nomadica.Infrastructure.Persistence.Configurations;

public class ListingChangeLogConfiguration : IEntityTypeConfiguration<ListingChangeLog>
{
    public void Configure(EntityTypeBuilder<ListingChangeLog> builder)
    {
        builder.ToTable("ListingChangeLogs");

        builder.HasKey(l => l.Id);

        builder.Property(l => l.ChangedBy)
            .IsRequired()
            .HasConversion(id => id.Value, value => new UserId(value));

        builder.Property(l => l.OldData).HasColumnType("nvarchar(max)");
        builder.Property(l => l.NewData).HasColumnType("nvarchar(max)");

        builder.Property(l => l.CreatedAt).HasColumnType("datetime2");
        builder.Property(l => l.UpdatedAt).HasColumnType("datetime2");

        builder.HasIndex(l => new { l.TargetType, l.TargetId });
        builder.HasIndex(l => l.ChangedBy);
    }
}
