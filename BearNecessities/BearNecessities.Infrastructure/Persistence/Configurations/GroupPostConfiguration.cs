using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using BearNecessities.Domain.Entities;
using BearNecessities.Domain.ValueObjects;

namespace BearNecessities.Infrastructure.Persistence.Configurations;

public class GroupPostConfiguration : IEntityTypeConfiguration<GroupPost>
{
    public void Configure(EntityTypeBuilder<GroupPost> builder)
    {
        builder.ToTable("GroupPosts");

        builder.HasKey(p => p.Id);

        builder.Property(p => p.GroupId)
            .HasConversion(id => id.Value, value => new GroupId(value));

        builder.Property(p => p.AuthorId)
            .IsRequired()
            .HasConversion(id => id.Value, value => new UserId(value));

        builder.Property(p => p.Body).IsRequired().HasMaxLength(GroupPost.MaxBodyLength);
        builder.Property(p => p.CreatedAt).HasColumnType("datetime2");
        builder.Property(p => p.UpdatedAt).HasColumnType("datetime2");

        builder.HasIndex(p => new { p.GroupId, p.CreatedAt });
    }
}
