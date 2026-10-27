using Nomadica.Application.Imports;

namespace Nomadica.Application.Interfaces;

public interface IPlaceImportService
{
    /// Inserts normalized places with dedup + provenance. autoApprove=true publishes them Live;
    /// otherwise they land in PendingReview for the moderation queue.
    Task<ImportReport> ImportAsync(
        IReadOnlyList<ImportPlace> places,
        bool autoApprove,
        CancellationToken cancellationToken = default);
}
