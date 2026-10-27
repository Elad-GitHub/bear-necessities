using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Nomadica.Domain.Entities;
using Nomadica.Domain.ValueObjects;

namespace Nomadica.Infrastructure.Persistence.Configurations;

public class ConversationParticipantConfiguration : IEntityTypeConfiguration<ConversationParticipant>
{
    public void Configure(EntityTypeBuilder<ConversationParticipant> builder)
    {
        builder.ToTable("ConversationParticipants");

        builder.HasKey(cp => new { cp.ConversationId, cp.UserId });

        builder.Property(cp => cp.UserId)
            .IsRequired()
            .HasConversion(id => id.Value, value => new UserId(value));

        builder.Property(cp => cp.JoinedAt).HasColumnType("datetime2");
        builder.Property(cp => cp.LastReadAt).HasColumnType("datetime2");
    }
}
