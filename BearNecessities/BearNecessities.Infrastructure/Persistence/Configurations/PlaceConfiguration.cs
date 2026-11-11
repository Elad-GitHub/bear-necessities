using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using BearNecessities.Domain.Entities;
using BearNecessities.Domain.Enums;
using BearNecessities.Domain.ValueObjects;

namespace BearNecessities.Infrastructure.Persistence.Configurations;

public class PlaceConfiguration : IEntityTypeConfiguration<Place>
{
    public void Configure(EntityTypeBuilder<Place> builder)
    {
        builder.ToTable("Places");

        builder.HasKey(p => p.Id);
        builder.Property(p => p.Id)
            .HasConversion(id => id.Value, value => new PlaceId(value));

        builder.Property(p => p.Name).IsRequired().HasMaxLength(200);
        builder.Property(p => p.Description).HasMaxLength(4000);
        builder.Property(p => p.PhotoUrl).HasMaxLength(1000);
        builder.Property(p => p.PhotoCredit).HasMaxLength(300);
        builder.Property(p => p.PhotoCreditUrl).HasMaxLength(1000);
        builder.Property(p => p.Latitude).HasPrecision(9, 6);
        builder.Property(p => p.Longitude).HasPrecision(9, 6);
        builder.Property(p => p.H3IndexRes8).IsRequired().HasMaxLength(30);
        builder.Property(p => p.H3IndexRes5).IsRequired().HasMaxLength(30);
        builder.Property(p => p.Address).HasMaxLength(500);
        builder.Property(p => p.City).HasMaxLength(100);
        builder.Property(p => p.Country).HasMaxLength(100);
        builder.Property(p => p.CountryCode).HasMaxLength(3);
        builder.Property(p => p.Timezone).IsRequired().HasMaxLength(100);
        builder.Property(p => p.ExternalSourceId).HasMaxLength(256);

        builder.Property(p => p.OwnerId)
            .IsRequired()
            .HasConversion(id => id.Value, value => new UserId(value));

        builder.Property(p => p.ReviewedBy)
            .HasConversion(
                id => id == null ? (Guid?)null : id.Value,
                value => value == null ? null : new UserId(value.Value));

        builder.Property(p => p.CreatedAt).HasColumnType("datetime2");
        builder.Property(p => p.UpdatedAt).HasColumnType("datetime2");
        builder.Property(p => p.DeletedAt).HasColumnType("datetime2");
        builder.Property(p => p.ReviewedAt).HasColumnType("datetime2");

        builder.Ignore(p => p.IsDeleted);

        builder.HasQueryFilter(p => p.DeletedAt == null);

        builder.HasIndex(p => new { p.Status, p.OwnerId })
            .HasFilter("DeletedAt IS NULL");

        builder.HasIndex(p => new { p.H3IndexRes8 });
        builder.HasIndex(p => new { p.H3IndexRes5 });
        builder.HasIndex(p => new { p.Latitude, p.Longitude });

        builder.HasMany(p => p.Categories)
            .WithMany()
            .UsingEntity<Dictionary<string, object>>(
                "PlaceCategories",
                r => r.HasOne<Category>().WithMany().HasForeignKey("CategoryId"),
                l => l.HasOne<Place>().WithMany().HasForeignKey("PlaceId"),
                j => j.HasKey("PlaceId", "CategoryId"));

        builder.Navigation(p => p.Categories)
            .HasField("_categories")
            .UsePropertyAccessMode(PropertyAccessMode.Field);

        builder.HasMany(p => p.ExternalLinks)
            .WithOne()
            .HasForeignKey(el => el.PlaceId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Navigation(p => p.ExternalLinks)
            .HasField("_externalLinks")
            .UsePropertyAccessMode(PropertyAccessMode.Field);

        // Media is loaded via OwnerType/OwnerId filter; navigation not configured in EF Core
        builder.Ignore(p => p.Media);
    }
}
