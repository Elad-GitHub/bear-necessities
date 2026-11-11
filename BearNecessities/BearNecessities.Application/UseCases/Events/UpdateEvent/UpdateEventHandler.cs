using System.Text.Json;
using BearNecessities.Application.Exceptions;
using BearNecessities.Application.Interfaces;
using BearNecessities.Domain.Entities;
using BearNecessities.Domain.Enums;
using BearNecessities.Domain.Interfaces;
using BearNecessities.Domain.ValueObjects;

namespace BearNecessities.Application.UseCases.Events.UpdateEvent;

/// <summary>
/// Admins edit events directly, and so do owners while their event isn't live yet. Anyone else's edit
/// becomes a suggestion that waits for an admin; the event stays as it is meanwhile.
/// </summary>
public sealed class UpdateEventHandler : IRequestHandler<UpdateEventRequest, UpdateEventResponse>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUserService _currentUser;
    private readonly IOccurrenceExpander _expander;

    public UpdateEventHandler(IUnitOfWork unitOfWork, ICurrentUserService currentUser, IOccurrenceExpander expander)
    {
        _unitOfWork = unitOfWork;
        _currentUser = currentUser;
        _expander = expander;
    }

    public async Task<UpdateEventResponse> HandleAsync(UpdateEventRequest request, CancellationToken cancellationToken = default)
    {
        if (!_currentUser.IsAuthenticated || _currentUser.UserId is null)
            throw new UnauthorizedException("Authentication required.");

        EventEdits.Validate(request);

        var evt = await _unitOfWork.Events.GetByIdWithRelationsAsync(new EventId(request.EventId), cancellationToken)
            ?? throw new NotFoundException("Event", request.EventId);

        if (evt.Status is ListingStatus.Cancelled or ListingStatus.Archived)
            throw new ValidationException(new Dictionary<string, string[]>
            {
                ["status"] = ["Cannot update a Cancelled or Archived event."]
            });

        var isOwnDraft = evt.OwnerId == _currentUser.UserId && evt.Status != ListingStatus.Live;
        if (!_currentUser.HasRole(UserRole.Admin) && !isOwnDraft)
        {
            var suggestion = EditSuggestion.Create(
                ListingChangeTargetType.Event, evt.Id.Value, _currentUser.UserId, JsonSerializer.Serialize(request));
            await _unitOfWork.EditSuggestions.AddAsync(suggestion, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);
            return new UpdateEventResponse(evt.Id.Value, evt.Status.ToString(), OccurrencesScheduled: 0, RequiresReview: true);
        }

        var occurrencesScheduled = await EventEdits.ApplyAsync(
            _unitOfWork, _expander, evt, request, _currentUser.UserId, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return new UpdateEventResponse(evt.Id.Value, evt.Status.ToString(), occurrencesScheduled, RequiresReview: false);
    }
}
