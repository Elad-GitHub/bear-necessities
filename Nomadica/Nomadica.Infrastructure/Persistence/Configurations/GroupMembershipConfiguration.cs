using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Nomadica.Domain.Entities;
using Nomadica.Domain.ValueObjects;

namespace Nomadica.Infrastructure.Persistence.Configurations;

public class GroupMembershipConfiguration : IEntityTypeConfiguration<GroupMembership>
{
    public void Configure(EntityTypeBuilder<GroupMembership> builder)
    {
        builder.ToTable("GroupMemberships");

        builder.HasKey(m => new { m.GroupId, m.UserId });

        builder.Property(m => m.GroupId)
            .HasConversion(id => id.Value, value => new GroupId(value));

        builder.Property(m => m.UserId)
            .HasConversion(id => id.Value, value => new UserId(value));

        builder.Property(m => m.JoinedAt).HasColumnType("datetime2");

        builder.HasIndex(m => m.UserId);
    }
}
