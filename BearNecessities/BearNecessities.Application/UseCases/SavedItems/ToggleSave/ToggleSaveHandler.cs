using BearNecessities.Application.Exceptions;
using BearNecessities.Application.Interfaces;
using BearNecessities.Domain.Entities;
using BearNecessities.Domain.Enums;
using BearNecessities.Domain.Interfaces;

namespace BearNecessities.Application.UseCases.SavedItems.ToggleSave;

public sealed class ToggleSaveHandler : IRequestHandler<ToggleSaveRequest, ToggleSaveResponse>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUserService _currentUser;

    public ToggleSaveHandler(IUnitOfWork unitOfWork, ICurrentUserService currentUser)
    {
        _unitOfWork = unitOfWork;
        _currentUser = currentUser;
    }

    public async Task<ToggleSaveResponse> HandleAsync(
        ToggleSaveRequest request,
        CancellationToken cancellationToken = default)
    {
        if (!_currentUser.IsAuthenticated || _currentUser.UserId is null)
            throw new UnauthorizedException("Authentication required.");

        if (request.TargetType != "Place" && request.TargetType != "Event")
            throw new ValidationException(new Dictionary<string, string[]>
            {
                ["targetType"] = ["TargetType must be 'Place' or 'Event'."]
            });

        if (!Enum.TryParse<SavedItemType>(request.SaveType, ignoreCase: true, out var requestedType))
            throw new ValidationException(new Dictionary<string, string[]>
            {
                ["saveType"] = ["SaveType must be 'WantToGo', 'Visited' or 'VisitedLoved'."]
            });

        var userId = _currentUser.UserId;
        var existing = await _unitOfWork.SavedItems.GetAsync(
            userId, request.TargetType, request.TargetId, cancellationToken);

        ToggleSaveResponse response;

        if (existing is null)
        {
            var item = SavedItem.Create(userId, request.TargetType, request.TargetId, requestedType);
            await _unitOfWork.SavedItems.SaveAsync(item, cancellationToken);
            response = new ToggleSaveResponse(true, requestedType.ToString());
        }
        else if (existing.ItemType == requestedType)
        {
            _unitOfWork.SavedItems.Remove(existing);
            response = new ToggleSaveResponse(false, null);
        }
        else
        {
            existing.ChangeType(requestedType);
            response = new ToggleSaveResponse(true, requestedType.ToString());
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return response;
    }
}
