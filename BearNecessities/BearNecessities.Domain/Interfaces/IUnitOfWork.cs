namespace BearNecessities.Domain.Interfaces;

public interface IUnitOfWork
{
    IPlaceRepository Places { get; }
    IEventRepository Events { get; }
    IEventOccurrenceRepository Occurrences { get; }
    ICategoryRepository Categories { get; }
    IMediaRepository Media { get; }
    IUserRepository Users { get; }
    IListingChangeLogRepository ChangeLogs { get; }
    IReviewRepository Reviews { get; }
    IReportRepository Reports { get; }
    IRsvpRepository Rsvps { get; }
    IConversationRepository Conversations { get; }
    ISavedItemRepository SavedItems { get; }
    IGroupRepository Groups { get; }
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
