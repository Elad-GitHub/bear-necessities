using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Nomadica.Domain.Entities;
using Nomadica.Domain.ValueObjects;

namespace Nomadica.Infrastructure.Persistence.Configurations;

public class RsvpConfiguration : IEntityTypeConfiguration<Rsvp>
{
    public void Configure(EntityTypeBuilder<Rsvp> builder)
    {
        builder.ToTable("Rsvps");

        builder.HasKey(r => r.Id);

        builder.Property(r => r.UserId)
            .IsRequired()
            .HasConversion(id => id.Value, value => new UserId(value));

        builder.Property(r => r.EventId)
            .IsRequired()
            .HasConversion(id => id.Value, value => new EventId(value));

        builder.Property(r => r.CreatedAt).HasColumnType("datetime2");
        builder.Property(r => r.UpdatedAt).HasColumnType("datetime2");

        builder.HasIndex(r => new { r.UserId, r.EventId, r.OccurrenceId }).IsUnique();
    }
}
