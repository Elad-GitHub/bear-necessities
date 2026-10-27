using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Nomadica.Domain.Entities;
using Nomadica.Domain.Enums;
using Nomadica.Domain.ValueObjects;

namespace Nomadica.Infrastructure.Persistence.Configurations;

public class SavedItemConfiguration : IEntityTypeConfiguration<SavedItem>
{
    public void Configure(EntityTypeBuilder<SavedItem> builder)
    {
        builder.ToTable("SavedItems");

        builder.HasKey(si => new { si.UserId, si.TargetType, si.TargetId });

        builder.Property(si => si.UserId)
            .IsRequired()
            .HasConversion(id => id.Value, value => new UserId(value));

        builder.Property(si => si.TargetType).IsRequired().HasMaxLength(50);
        builder.Property(si => si.ItemType).IsRequired().HasDefaultValue(SavedItemType.WantToGo);
        builder.Property(si => si.SavedAt).HasColumnType("datetime2");

        builder.HasIndex(si => si.UserId);
    }
}
