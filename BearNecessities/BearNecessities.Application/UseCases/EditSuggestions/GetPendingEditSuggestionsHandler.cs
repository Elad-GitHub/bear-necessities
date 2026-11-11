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

public sealed record GetPendingEditSuggestionsRequest;

public sealed record EditSuggestionDto(
    Guid Id, string TargetType, Guid TargetId, string TargetName, string SuggestedBy, DateTime CreatedAt, List<FieldChangeDto> Changes);

public sealed record GetPendingEditSuggestionsResponse(List<EditSuggestionDto> Items);

/// <summary>The moderation queue's suggested edits, each with the fields it would change.</summary>
public sealed class GetPendingEditSuggestionsHandler : IRequestHandler<GetPendingEditSuggestionsRequest, GetPendingEditSuggestionsResponse>
{
    private const string DeletedListingName = "(deleted listing)";
    private const string UnknownUserName = "Unknown member";

    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUserService _currentUser;

    public GetPendingEditSuggestionsHandler(IUnitOfWork unitOfWork, ICurrentUserService currentUser)
    {
        _unitOfWork = unitOfWork;
        _currentUser = currentUser;
    }

    public async Task<GetPendingEditSuggestionsResponse> HandleAsync(
        GetPendingEditSuggestionsRequest request, CancellationToken cancellationToken = default)
    {
        if (!_currentUser.HasRole(UserRole.Admin))
            throw new ForbiddenException();

        var suggestions = await _unitOfWork.EditSuggestions.GetPendingAsync(cancellationToken);
        if (suggestions.Count == 0) return new GetPendingEditSuggestionsResponse([]);

        var placeEdits = suggestions.Where(s => s.TargetType == ListingChangeTargetType.Place)
            .ToDictionary(s => s.Id, s => JsonSerializer.Deserialize<UpdatePlaceRequest>(s.ProposedChanges)!);
        var eventEdits = suggestions.Where(s => s.TargetType == ListingChangeTargetType.Event)
            .ToDictionary(s => s.Id, s => JsonSerializer.Deserialize<UpdateEventRequest>(s.ProposedChanges)!);

        var events = (await _unitOfWork.Events.GetByIdsAsync(
            eventEdits.Values.Select(edit => new EventId(edit.EventId)), cancellationToken)).ToDictionary(e => e.Id.Value);

        // Venues of events are places too, so one lookup covers edited places and old and new venues
        var placeIds = placeEdits.Values.Select(edit => edit.PlaceId)
            .Concat(events.Values.Where(e => e.PlaceId is not null).Select(e => e.PlaceId!.Value))
            .Concat(eventEdits.Values.Where(edit => edit.PlaceId.HasValue).Select(edit => edit.PlaceId!.Value))
            .Distinct()
            .Select(id => new PlaceId(id));
        var places = (await _unitOfWork.Places.GetByIdsWithRelationsAsync(placeIds, cancellationToken)).ToDictionary(p => p.Id.Value);
        var placeNames = places.ToDictionary(pair => pair.Key, pair => pair.Value.Name);

        var categoryNames = (await _unitOfWork.Categories.GetAllAsync(cancellationToken)).ToDictionary(c => c.Id.Value, c => c.Name);
        var userNames = (await _unitOfWork.Users.GetByIdsAsync(suggestions.Select(s => s.SuggestedBy).Distinct(), cancellationToken))
            .ToDictionary(u => u.Id.Value, u => u.DisplayName);

        var items = suggestions.Select(suggestion =>
        {
            var suggestedBy = userNames.GetValueOrDefault(suggestion.SuggestedBy.Value, UnknownUserName);
            var (name, changes) = suggestion.TargetType == ListingChangeTargetType.Place
                ? DescribePlaceEdit(places.GetValueOrDefault(suggestion.TargetId), placeEdits[suggestion.Id], categoryNames)
                : DescribeEventEdit(events.GetValueOrDefault(suggestion.TargetId), eventEdits[suggestion.Id], categoryNames, placeNames);
            return new EditSuggestionDto(suggestion.Id, suggestion.TargetType.ToString(), suggestion.TargetId, name, suggestedBy,
                suggestion.CreatedAt, changes);
        }).ToList();

        return new GetPendingEditSuggestionsResponse(items);
    }

    private static (string Name, List<FieldChangeDto> Changes) DescribePlaceEdit(
        Place? place, UpdatePlaceRequest edit, IReadOnlyDictionary<Guid, string> categoryNames) =>
        place is null ? (DeletedListingName, []) : (place.Name, EditSuggestionDiff.ForPlace(place, edit, categoryNames));

    private static (string Name, List<FieldChangeDto> Changes) DescribeEventEdit(
        Event? evt, UpdateEventRequest edit, IReadOnlyDictionary<Guid, string> categoryNames, IReadOnlyDictionary<Guid, string> placeNames) =>
        evt is null ? (DeletedListingName, []) : (evt.Title, EditSuggestionDiff.ForEvent(evt, edit, categoryNames, placeNames));
}
