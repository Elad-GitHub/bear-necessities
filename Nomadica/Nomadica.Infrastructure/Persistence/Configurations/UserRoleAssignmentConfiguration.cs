using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Nomadica.Domain.Entities;
using Nomadica.Domain.ValueObjects;

namespace Nomadica.Infrastructure.Persistence.Configurations;

public class UserRoleAssignmentConfiguration : IEntityTypeConfiguration<UserRoleAssignment>
{
    public void Configure(EntityTypeBuilder<UserRoleAssignment> builder)
    {
        builder.ToTable("UserRoles");

        builder.Property(u => u.UserId)
            .HasConversion(id => id.Value, value => new UserId(value))
            .HasColumnName("UserId");

        builder.HasKey(u => new { u.UserId, u.RoleName });

        builder.Property(u => u.RoleName)
            .HasConversion<string>()
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(u => u.Region).HasMaxLength(100);
        builder.Property(u => u.AssignedAt).IsRequired();

        builder.HasIndex(u => u.UserId);
    }
}
