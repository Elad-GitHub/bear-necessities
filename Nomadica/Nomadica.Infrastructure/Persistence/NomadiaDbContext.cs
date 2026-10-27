using System.Reflection;
using Microsoft.EntityFrameworkCore;
using Nomadica.Domain.Entities;

namespace Nomadica.Infrastructure.Persistence;

public class NomadiaDbContext : DbContext
{
    public NomadiaDbContext(DbContextOptions<NomadiaDbContext> options) : base(options) { }

    public DbSet<User> Users => Set<User>();
    public DbSet<UserSocialLogin> UserSocialLogins => Set<UserSocialLogin>();
    public DbSet<Place> Places => Set<Place>();
    public DbSet<Event> Events => Set<Event>();
    public DbSet<EventOccurrence> EventOccurrences => Set<EventOccurrence>();
    public DbSet<Category> Categories => Set<Category>();
    public DbSet<Review> Reviews => Set<Review>();
    public DbSet<Rsvp> Rsvps => Set<Rsvp>();
    public DbSet<Media> Media => Set<Media>();
    public DbSet<Conversation> Conversations => Set<Conversation>();
    public DbSet<ConversationParticipant> ConversationParticipants => Set<ConversationParticipant>();
    public DbSet<Message> Messages => Set<Message>();
    public DbSet<Report> Reports => Set<Report>();
    public DbSet<SavedItem> SavedItems => Set<SavedItem>();
    public DbSet<ListingChangeLog> ListingChangeLogs => Set<ListingChangeLog>();
    public DbSet<PlaceExternalLink> PlaceExternalLinks => Set<PlaceExternalLink>();
    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();
    public DbSet<UserRoleAssignment> UserRoleAssignments => Set<UserRoleAssignment>();
    public DbSet<CommunityGroup> CommunityGroups => Set<CommunityGroup>();
    public DbSet<GroupMembership> GroupMemberships => Set<GroupMembership>();
    public DbSet<GroupPost> GroupPosts => Set<GroupPost>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.ApplyConfigurationsFromAssembly(Assembly.GetExecutingAssembly());
    }

    public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        foreach (var entry in ChangeTracker.Entries())
        {
            if (entry.State == EntityState.Modified)
            {
                var prop = entry.Properties.FirstOrDefault(p => p.Metadata.Name == "UpdatedAt");
                if (prop is not null)
                    prop.CurrentValue = DateTime.UtcNow;
            }
        }

        return await base.SaveChangesAsync(cancellationToken);
    }
}
