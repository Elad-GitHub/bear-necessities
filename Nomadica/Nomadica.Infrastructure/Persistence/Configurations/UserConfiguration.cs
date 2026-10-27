using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Nomadica.Domain.Entities;
using Nomadica.Domain.Enums;
using Nomadica.Domain.ValueObjects;

namespace Nomadica.Infrastructure.Persistence.Configurations;

public class UserConfiguration : IEntityTypeConfiguration<User>
{
    public void Configure(EntityTypeBuilder<User> builder)
    {
        builder.ToTable("Users");

        builder.HasKey(u => u.Id);
        builder.Property(u => u.Id)
            .HasConversion(id => id.Value, value => new UserId(value));

        builder.Property(u => u.Email).IsRequired().HasMaxLength(256);
        builder.Property(u => u.PasswordHash).HasMaxLength(512);
        builder.Property(u => u.DisplayName).IsRequired().HasMaxLength(100);
        builder.Property(u => u.AvatarUrl).HasMaxLength(2048);
        builder.Property(u => u.Bio).HasMaxLength(1000);
        builder.Property(u => u.SocialProfileUrl).HasMaxLength(500);
        builder.Property(u => u.MembershipStatus).IsRequired();
        builder.HasIndex(u => u.MembershipStatus);
        builder.Property(u => u.TrustScore).HasPrecision(3, 2);
        builder.Property(u => u.PreferredLocale).IsRequired().HasMaxLength(10);
        builder.Property(u => u.CreatedAt).HasColumnType("datetime2");
        builder.Property(u => u.UpdatedAt).HasColumnType("datetime2");
        builder.Property(u => u.LastActiveAt).HasColumnType("datetime2");

        builder.HasIndex(u => u.Email).IsUnique();

        builder.Ignore(u => u.Roles);
        builder.PrimitiveCollection<List<UserRole>>("_roles")
            .HasField("_roles")
            .UsePropertyAccessMode(PropertyAccessMode.Field)
            .HasColumnName("Roles")
            .HasColumnType("nvarchar(max)");

        builder.HasMany(u => u.SocialLogins)
            .WithOne()
            .HasForeignKey(sl => sl.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Navigation(u => u.SocialLogins)
            .HasField("_socialLogins")
            .UsePropertyAccessMode(PropertyAccessMode.Field);
    }
}
