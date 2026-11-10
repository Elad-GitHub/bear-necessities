using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using BearNecessities.Domain.Entities;
using BearNecessities.Domain.ValueObjects;

namespace BearNecessities.Infrastructure.Persistence.Configurations;

public class CommunityGroupConfiguration : IEntityTypeConfiguration<CommunityGroup>
{
    public void Configure(EntityTypeBuilder<CommunityGroup> builder)
    {
        builder.ToTable("CommunityGroups");

        builder.HasKey(g => g.Id);
        builder.Property(g => g.Id)
            .HasConversion(id => id.Value, value => new GroupId(value));

        builder.Property(g => g.Slug).IsRequired().HasMaxLength(160);
        builder.Property(g => g.Name).IsRequired().HasMaxLength(120);
        builder.Property(g => g.Description).HasMaxLength(2000);
        builder.Property(g => g.City).HasMaxLength(100);
        builder.Property(g => g.Country).HasMaxLength(100);
        builder.Property(g => g.Latitude).HasColumnType("decimal(9,6)");
        builder.Property(g => g.Longitude).HasColumnType("decimal(9,6)");

        builder.Property(g => g.CreatedBy)
            .IsRequired()
            .HasConversion(id => id.Value, value => new UserId(value));

        builder.Property(g => g.CreatedAt).HasColumnType("datetime2");
        builder.Property(g => g.UpdatedAt).HasColumnType("datetime2");

        builder.HasIndex(g => g.Slug).IsUnique();
        builder.HasIndex(g => g.Country);
        builder.HasIndex(g => new { g.Latitude, g.Longitude });
    }
}
