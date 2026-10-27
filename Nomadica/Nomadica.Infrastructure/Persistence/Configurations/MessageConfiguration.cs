using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Nomadica.Domain.Entities;
using Nomadica.Domain.ValueObjects;

namespace Nomadica.Infrastructure.Persistence.Configurations;

public class MessageConfiguration : IEntityTypeConfiguration<Message>
{
    public void Configure(EntityTypeBuilder<Message> builder)
    {
        builder.ToTable("Messages");

        builder.HasKey(m => m.Id);

        builder.Property(m => m.SenderId)
            .IsRequired()
            .HasConversion(id => id.Value, value => new UserId(value));

        builder.Property(m => m.Body).IsRequired().HasMaxLength(4000);

        builder.Property(m => m.CreatedAt).HasColumnType("datetime2");
        builder.Property(m => m.UpdatedAt).HasColumnType("datetime2");

        builder.HasIndex(m => new { m.ConversationId, m.CreatedAt });
    }
}
