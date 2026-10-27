using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Nomadica.Domain.Entities;
using Nomadica.Domain.ValueObjects;

namespace Nomadica.Infrastructure.Persistence.Configurations;

public class UserSocialLoginConfiguration : IEntityTypeConfiguration<UserSocialLogin>
{
    public void Configure(EntityTypeBuilder<UserSocialLogin> builder)
    {
        builder.ToTable("UserSocialLogins");

        builder.HasKey(sl => sl.Id);

        builder.Property(sl => sl.UserId)
            .IsRequired()
            .HasConversion(id => id.Value, value => new UserId(value));

        builder.Property(sl => sl.Provider).IsRequired();
        builder.Property(sl => sl.ProviderId).IsRequired().HasMaxLength(256);

        builder.Property(sl => sl.CreatedAt).HasColumnType("datetime2");
        builder.Property(sl => sl.UpdatedAt).HasColumnType("datetime2");

        builder.HasIndex(sl => new { sl.Provider, sl.ProviderId }).IsUnique();
    }
}
