using BearNecessities.Application.DTOs;
using BearNecessities.Application.Interfaces;
using BearNecessities.Domain.Interfaces;

namespace BearNecessities.Application.UseCases.Media.GetMediaByOwner;

public sealed class GetMediaByOwnerHandler : IRequestHandler<GetMediaByOwnerRequest, GetMediaByOwnerResponse>
{
    private readonly IUnitOfWork _unitOfWork;

    public GetMediaByOwnerHandler(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<GetMediaByOwnerResponse> HandleAsync(GetMediaByOwnerRequest request, CancellationToken cancellationToken = default)
    {
        var allMedia = await _unitOfWork.Media.GetByOwnerAsync(request.OwnerType, request.OwnerId, cancellationToken);

        var approvedMedia = allMedia
            .Where(m => m.ModerationStatus == Domain.Enums.ModerationStatus.Approved)
            .OrderBy(m => m.SortOrder)
            .ThenBy(m => m.CreatedAt)
            .ToList();

        // Gallery photos credit whoever took them
        var uploaders = (await _unitOfWork.Users.GetByIdsAsync(approvedMedia.Select(m => m.UploadedBy).Distinct(), cancellationToken))
            .ToDictionary(user => user.Id.Value, user => user.DisplayName);
        var approved = approvedMedia
            .Select(m => new MediaDto(m.Id, m.Url, m.ThumbnailUrl, m.MimeType, m.SortOrder, m.ModerationStatus,
                uploaders.GetValueOrDefault(m.UploadedBy.Value), m.ReviewId))
            .ToList();

        return new GetMediaByOwnerResponse(approved);
    }
}
