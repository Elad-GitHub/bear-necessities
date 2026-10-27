using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Nomadica.Domain.Entities;
using Nomadica.Domain.ValueObjects;

namespace Nomadica.Infrastructure.Persistence.Configurations;

public class MediaConfiguration : IEntityTypeConfiguration<Media>
{
    public void Configure(EntityTypeBuilder<Media> builder)
    {
        builder.ToTable("Media");

        builder.HasKey(m => m.Id);

        builder.Property(m => m.UploadedBy)
            .IsRequired()
            .HasConversion(id => id.Value, value => new UserId(value));

        builder.Property(m => m.StoragePath).IsRequired().HasMaxLength(1000);
        builder.Property(m => m.Url).IsRequired().HasMaxLength(2048);
        builder.Property(m => m.ThumbnailUrl).HasMaxLength(2048);
        builder.Property(m => m.MimeType).IsRequired().HasMaxLength(100);

        builder.Property(m => m.CreatedAt).HasColumnType("datetime2");
        builder.Property(m => m.UpdatedAt).HasColumnType("datetime2");

        builder.HasIndex(m => new { m.OwnerType, m.OwnerId });
        builder.HasIndex(m => m.ModerationStatus);
    }
}
