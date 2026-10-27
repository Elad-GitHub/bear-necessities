using Nomadica.Application.Exceptions;
using Nomadica.Application.Interfaces;
using Nomadica.Domain.Entities;
using Nomadica.Domain.Enums;
using Nomadica.Domain.Interfaces;
using Nomadica.Domain.ValueObjects;

namespace Nomadica.Application.UseCases.Rsvps.CreateOrUpdateRsvp;

public sealed class CreateOrUpdateRsvpHandler : IRequestHandler<CreateOrUpdateRsvpRequest, CreateOrUpdateRsvpResponse>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUserService _currentUser;

    public CreateOrUpdateRsvpHandler(IUnitOfWork unitOfWork, ICurrentUserService currentUser)
    {
        _unitOfWork = unitOfWork;
        _currentUser = currentUser;
    }

    public async Task<CreateOrUpdateRsvpResponse> HandleAsync(
        CreateOrUpdateRsvpRequest request,
        CancellationToken cancellationToken = default)
    {
        if (!_currentUser.IsAuthenticated || _currentUser.UserId is null)
            throw new UnauthorizedException("Authentication required.");

        if (request.PartySize < 1 || request.PartySize > 10)
            throw new ValidationException(new Dictionary<string, string[]>
            {
                ["partySize"] = ["Party size must be between 1 and 10."]
            });

        var userId = _currentUser.UserId;
        var eventId = new EventId(request.EventId);

        var evt = await _unitOfWork.Events.GetByIdAsync(eventId, cancellationToken)
            ?? throw new NotFoundException("Event", request.EventId);

        var existingRsvp = await _unitOfWork.Rsvps.GetUserRsvpForEventAsync(
            userId, eventId, request.OccurrenceId, cancellationToken);

        if (request.Status == RsvpStatus.Going && evt.MaxCapacity.HasValue)
        {
            var goingCount = await _unitOfWork.Rsvps.GetCountByEventAsync(
                eventId, RsvpStatus.Going, cancellationToken);
            var existingGoingSize = existingRsvp?.Status == RsvpStatus.Going
                ? existingRsvp.PartySize
                : 0;
            if (goingCount - existingGoingSize + request.PartySize > evt.MaxCapacity.Value)
                throw new ValidationException(new Dictionary<string, string[]>
                {
                    ["capacity"] = ["Event is at capacity."]
                });
        }

        Rsvp rsvp;
        bool created;

        if (existingRsvp is null)
        {
            rsvp = Rsvp.Create(userId, eventId, request.Status, request.PartySize, request.OccurrenceId);
            await _unitOfWork.Rsvps.AddAsync(rsvp, cancellationToken);
            created = true;
        }
        else
        {
            existingRsvp.Update(request.Status, request.PartySize);
            await _unitOfWork.Rsvps.UpdateAsync(existingRsvp, cancellationToken);
            rsvp = existingRsvp;
            created = false;
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return new CreateOrUpdateRsvpResponse(rsvp.Id, rsvp.Status, rsvp.PartySize, created);
    }
}
