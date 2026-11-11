using System.Text.Json;
using BearNecessities.Application.Exceptions;
using BearNecessities.Application.Interfaces;
using BearNecessities.Application.UseCases.Events.UpdateEvent;
using BearNecessities.Application.UseCases.Places.UpdatePlace;
using BearNecessities.Domain.Entities;
using BearNecessities.Domain.Enums;
using BearNecessities.Domain.Interfaces;
using BearNecessities.Domain.ValueObjects;

namespace BearNecessities.Application.UseCases.EditSuggestions;

public sealed record ReviewEditSuggestionRequest(Guid SuggestionId, bool IsApproved);

public sealed record ReviewEditSuggestionResponse(Guid SuggestionId, string Status);

/// <summary>An admin approves a suggested edit, which applies it to the listing, or rejects it.</summary>
public sealed class ReviewEditSuggestionHandler : IRequestHandler<ReviewEditSuggestionRequest, ReviewEditSuggestionResponse>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUserService _currentUser;
    private readonly IOccurrenceExpander _expander;

    public ReviewEditSuggestionHandler(IUnitOfWork unitOfWork, ICurrentUserService currentUser, IOccurrenceExpander expander)
    {
        _unitOfWork = unitOfWork;
        _currentUser = currentUser;
        _expander = expander;
    }

    public async Task<ReviewEditSuggestionResponse> HandleAsync(
        ReviewEditSuggestionRequest request, CancellationToken cancellationToken = default)
    {
        if (!_currentUser.HasRole(UserRole.Admin) || _currentUser.UserId is null)
            throw new ForbiddenException();

        var suggestion = await _unitOfWork.EditSuggestions.GetByIdAsync(request.SuggestionId, cancellationToken)
            ?? throw new NotFoundException("EditSuggestion", request.SuggestionId);

        if (suggestion.Status != EditSuggestionStatus.Pending)
            throw new ValidationException(new Dictionary<string, string[]>
            {
                ["status"] = ["This suggestion was already reviewed."]
            });

        if (request.IsApproved)
        {
            await ApplyAsync(suggestion, cancellationToken);
            suggestion.Approve(_currentUser.UserId);
        }
        else
        {
            suggestion.Reject(_currentUser.UserId);
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return new ReviewEditSuggestionResponse(suggestion.Id, suggestion.Status.ToString());
    }

    // The change log credits the member who suggested the edit
    private async Task ApplyAsync(EditSuggestion suggestion, CancellationToken cancellationToken)
    {
        if (suggestion.TargetType == ListingChangeTargetType.Place)
        {
            var edit = JsonSerializer.Deserialize<UpdatePlaceRequest>(suggestion.ProposedChanges)!;
            var place = await _unitOfWork.Places.GetByIdWithRelationsAsync(new PlaceId(suggestion.TargetId), cancellationToken)
                ?? throw new NotFoundException("Place", suggestion.TargetId);
            await PlaceEdits.ApplyAsync(_unitOfWork, place, edit, suggestion.SuggestedBy, cancellationToken);
            return;
        }

        var eventEdit = JsonSerializer.Deserialize<UpdateEventRequest>(suggestion.ProposedChanges)!;
        var evt = await _unitOfWork.Events.GetByIdWithRelationsAsync(new EventId(suggestion.TargetId), cancellationToken)
            ?? throw new NotFoundException("Event", suggestion.TargetId);
        await EventEdits.ApplyAsync(_unitOfWork, _expander, evt, eventEdit, suggestion.SuggestedBy, cancellationToken);
    }
}
