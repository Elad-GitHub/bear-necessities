using BearNecessities.Domain.Interfaces;

namespace BearNecessities.Infrastructure.Persistence;

public class UnitOfWork : IUnitOfWork
{
    private readonly BearNecessitiesDbContext _context;

    public UnitOfWork(
        BearNecessitiesDbContext context,
        IPlaceRepository places,
        IEventRepository events,
        IEventOccurrenceRepository occurrences,
        ICategoryRepository categories,
        IMediaRepository media,
        IUserRepository users,
        IListingChangeLogRepository changeLogs,
        IReviewRepository reviews,
        IReportRepository reports,
        IRsvpRepository rsvps,
        IConversationRepository conversations,
        ISavedItemRepository savedItems,
        IGroupRepository groups,
        IEditSuggestionRepository editSuggestions)
    {
        _context = context;
        Places = places;
        Events = events;
        Occurrences = occurrences;
        Categories = categories;
        Media = media;
        Users = users;
        ChangeLogs = changeLogs;
        Reviews = reviews;
        Reports = reports;
        Rsvps = rsvps;
        Conversations = conversations;
        SavedItems = savedItems;
        Groups = groups;
        EditSuggestions = editSuggestions;
    }

    public IPlaceRepository Places { get; }
    public IEventRepository Events { get; }
    public IEventOccurrenceRepository Occurrences { get; }
    public ICategoryRepository Categories { get; }
    public IMediaRepository Media { get; }
    public IUserRepository Users { get; }
    public IListingChangeLogRepository ChangeLogs { get; }
    public IReviewRepository Reviews { get; }
    public IReportRepository Reports { get; }
    public IRsvpRepository Rsvps { get; }
    public IConversationRepository Conversations { get; }
    public ISavedItemRepository SavedItems { get; }
    public IGroupRepository Groups { get; }
    public IEditSuggestionRepository EditSuggestions { get; }

    public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        => _context.SaveChangesAsync(cancellationToken);
}
