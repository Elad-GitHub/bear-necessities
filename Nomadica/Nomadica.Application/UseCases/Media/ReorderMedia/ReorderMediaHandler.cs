using Nomadica.Application.Exceptions;
using Nomadica.Application.Interfaces;
using Nomadica.Domain.Enums;
using Nomadica.Domain.Interfaces;
using Nomadica.Domain.ValueObjects;

namespace Nomadica.Application.UseCases.Media.ReorderMedia;

public sealed class ReorderMediaHandler : IRequestHandler<ReorderMediaRequest, bool>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUserService _currentUser;

    public ReorderMediaHandler(IUnitOfWork unitOfWork, ICurrentUserService currentUser)
    {
        _unitOfWork = unitOfWork;
        _currentUser = currentUser;
    }

    public async Task<bool> HandleAsync(ReorderMediaRequest request, CancellationToken cancellationToken = default)
    {
        if (!_currentUser.IsAuthenticated || _currentUser.UserId is null)
            throw new UnauthorizedException("Authentication required.");

        if (request.OrderedMediaIds is null || request.OrderedMediaIds.Count == 0)
            throw new ValidationException(new Dictionary<string, string[]>
            {
                ["orderedMediaIds"] = ["At least one media ID is required."]
            });

        var isAdmin = _currentUser.HasRole(UserRole.Admin);
        await VerifyOwnershipAsync(request, isAdmin, cancellationToken);

        var allMedia = await _unitOfWork.Media.GetByOwnerAsync(request.OwnerType, request.OwnerId, cancellationToken);
        var mediaById = allMedia.ToDictionary(m => m.Id);

        for (var i = 0; i < request.OrderedMediaIds.Count; i++)
        {
            var id = request.OrderedMediaIds[i];
            if (!mediaById.TryGetValue(id, out var media))
                continue;

            media.SetSortOrder(i);
            await _unitOfWork.Media.UpdateAsync(media, cancellationToken);
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return true;
    }

    private async Task VerifyOwnershipAsync(ReorderMediaRequest request, bool isAdmin, CancellationToken cancellationToken)
    {
        if (isAdmin)
            return;

        switch (request.OwnerType)
        {
            case MediaOwnerType.Place:
            {
                var place = await _unitOfWork.Places.GetByIdAsync(new PlaceId(request.OwnerId), cancellationToken)
                    ?? throw new NotFoundException("Place", request.OwnerId);
                if (place.OwnerId != _currentUser.UserId)
                    throw new ForbiddenException();
                break;
            }
            case MediaOwnerType.Event:
            {
                var evt = await _unitOfWork.Events.GetByIdAsync(new EventId(request.OwnerId), cancellationToken)
                    ?? throw new NotFoundException("Event", request.OwnerId);
                if (evt.OwnerId != _currentUser.UserId)
                    throw new ForbiddenException();
                break;
            }
            case MediaOwnerType.User:
            {
                if (_currentUser.UserId!.Value != request.OwnerId)
                    throw new ForbiddenException();
                break;
            }
        }
    }
}
