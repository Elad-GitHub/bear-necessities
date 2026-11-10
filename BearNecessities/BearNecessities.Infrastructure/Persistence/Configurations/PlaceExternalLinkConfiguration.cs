using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using BearNecessities.Domain.Entities;
using BearNecessities.Domain.ValueObjects;

namespace BearNecessities.Infrastructure.Persistence.Configurations;

public class PlaceExternalLinkConfiguration : IEntityTypeConfiguration<PlaceExternalLink>
{
    public void Configure(EntityTypeBuilder<PlaceExternalLink> builder)
    {
        builder.ToTable("PlaceExternalLinks");

        builder.HasKey(l => l.Id);

        builder.Property(l => l.PlaceId)
            .IsRequired()
            .HasConversion(id => id.Value, value => new PlaceId(value));

        builder.Property(l => l.Label).IsRequired().HasMaxLength(100);
        builder.Property(l => l.Url).IsRequired().HasMaxLength(2048);

        builder.Property(l => l.CreatedAt).HasColumnType("datetime2");
        builder.Property(l => l.UpdatedAt).HasColumnType("datetime2");

        builder.HasIndex(l => l.PlaceId);
    }
}
