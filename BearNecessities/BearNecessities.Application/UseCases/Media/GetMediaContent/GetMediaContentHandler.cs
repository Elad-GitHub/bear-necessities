using BearNecessities.Application.DTOs.Media;
using BearNecessities.Application.Interfaces;
using BearNecessities.Domain.Interfaces;

namespace BearNecessities.Application.UseCases.Media.GetMediaContent;

public sealed class GetMediaContentHandler : IRequestHandler<GetMediaContentRequest, StorageDownload?>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IStorageService _storage;

    public GetMediaContentHandler(IUnitOfWork unitOfWork, IStorageService storage)
    {
        _unitOfWork = unitOfWork;
        _storage = storage;
    }

    public async Task<StorageDownload?> HandleAsync(GetMediaContentRequest request, CancellationToken cancellationToken = default)
    {
        var media = await _unitOfWork.Media.GetByIdAsync(request.MediaId, cancellationToken);
        if (media is null) return null;

        return await _storage.OpenReadAsync(media.StoragePath, cancellationToken);
    }
}
