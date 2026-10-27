using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Nomadica.Domain.Entities;
using Nomadica.Domain.ValueObjects;

namespace Nomadica.Infrastructure.Persistence.Configurations;

public class EventConfiguration : IEntityTypeConfiguration<Event>
{
    public void Configure(EntityTypeBuilder<Event> builder)
    {
        builder.ToTable("Events");

        builder.HasKey(e => e.Id);
        builder.Property(e => e.Id)
            .HasConversion(id => id.Value, value => new EventId(value));

        builder.Property(e => e.PlaceId)
            .HasConversion(
                id => id == null ? (Guid?)null : id.Value,
                value => value == null ? null : new PlaceId(value.Value));

        builder.Property(e => e.OwnerId)
            .IsRequired()
            .HasConversion(id => id.Value, value => new UserId(value));

        builder.Property(e => e.ReviewedBy)
            .HasConversion(
                id => id == null ? (Guid?)null : id.Value,
                value => value == null ? null : new UserId(value.Value));

        builder.Property(e => e.Title).IsRequired().HasMaxLength(200);
        builder.Property(e => e.Description).HasMaxLength(4000);
        builder.Property(e => e.Timezone).IsRequired().HasMaxLength(100);
        builder.Property(e => e.RRule).HasMaxLength(500);
        builder.Property(e => e.Price).HasPrecision(10, 2);
        builder.Property(e => e.Currency).HasMaxLength(3);
        builder.Property(e => e.OnlineUrl).HasMaxLength(2048);

        builder.Property(e => e.CreatedAt).HasColumnType("datetime2");
        builder.Property(e => e.UpdatedAt).HasColumnType("datetime2");
        builder.Property(e => e.DeletedAt).HasColumnType("datetime2");
        builder.Property(e => e.StartAt).HasColumnType("datetime2");
        builder.Property(e => e.EndAt).HasColumnType("datetime2");
        builder.Property(e => e.ReviewedAt).HasColumnType("datetime2");

        builder.Ignore(e => e.IsDeleted);

        builder.HasQueryFilter(e => e.DeletedAt == null);

        builder.HasIndex(e => new { e.Status, e.OwnerId })
            .HasFilter("DeletedAt IS NULL");
        builder.HasIndex(e => new { e.PlaceId, e.Status });
        builder.HasIndex(e => e.StartAt);

        // ExDates is exposed as IReadOnlyCollection<DateTime> (read-only). EF Core 8 rejects the
        // type mismatch when the property name is used, so we register via the backing field name.
        builder.Ignore(e => e.ExDates);
        builder.PrimitiveCollection<List<DateTime>>("_exDates")
            .HasField("_exDates")
            .UsePropertyAccessMode(PropertyAccessMode.Field)
            .HasColumnName("ExDates")
            .HasColumnType("nvarchar(max)");

        builder.HasMany(e => e.Categories)
            .WithMany()
            .UsingEntity<Dictionary<string, object>>(
                "EventCategories",
                r => r.HasOne<Category>().WithMany().HasForeignKey("CategoryId"),
                l => l.HasOne<Event>().WithMany().HasForeignKey("EventId"),
                j => j.HasKey("EventId", "CategoryId"));

        builder.Navigation(e => e.Categories)
            .HasField("_categories")
            .UsePropertyAccessMode(PropertyAccessMode.Field);

        builder.HasMany(e => e.Occurrences)
            .WithOne()
            .HasForeignKey(o => o.RecurringEventId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Navigation(e => e.Occurrences)
            .HasField("_occurrences")
            .UsePropertyAccessMode(PropertyAccessMode.Field);

        // Media loaded via OwnerType/OwnerId; navigation not configured in EF Core
        builder.Ignore(e => e.Media);
    }
}
