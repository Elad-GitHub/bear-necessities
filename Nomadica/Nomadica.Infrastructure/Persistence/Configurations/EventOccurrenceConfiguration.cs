using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Nomadica.Domain.Entities;
using Nomadica.Domain.ValueObjects;

namespace Nomadica.Infrastructure.Persistence.Configurations;

public class EventOccurrenceConfiguration : IEntityTypeConfiguration<EventOccurrence>
{
    public void Configure(EntityTypeBuilder<EventOccurrence> builder)
    {
        builder.ToTable("EventOccurrences");

        builder.HasKey(o => o.Id);

        builder.Property(o => o.RecurringEventId)
            .IsRequired()
            .HasConversion(id => id.Value, value => new EventId(value));

        builder.Property(o => o.StartAt).IsRequired().HasColumnType("datetime2");
        builder.Property(o => o.EndAt).HasColumnType("datetime2");
        builder.Property(o => o.OverrideData).HasColumnType("nvarchar(max)");

        builder.Property(o => o.CreatedAt).HasColumnType("datetime2");
        builder.Property(o => o.UpdatedAt).HasColumnType("datetime2");

        builder.HasIndex(o => new { o.RecurringEventId, o.StartAt });
    }
}
