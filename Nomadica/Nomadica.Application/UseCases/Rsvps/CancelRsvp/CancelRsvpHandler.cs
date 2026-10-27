using Nomadica.Application.Exceptions;
using Nomadica.Application.Interfaces;
using Nomadica.Domain.Interfaces;

namespace Nomadica.Application.UseCases.Rsvps.CancelRsvp;

public sealed class CancelRsvpHandler : IRequestHandler<CancelRsvpRequest, bool>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUserService _currentUser;

    public CancelRsvpHandler(IUnitOfWork unitOfWork, ICurrentUserService currentUser)
    {
        _unitOfWork = unitOfWork;
        _currentUser = currentUser;
    }

    public async Task<bool> HandleAsync(CancelRsvpRequest request, CancellationToken cancellationToken = default)
    {
        if (!_currentUser.IsAuthenticated || _currentUser.UserId is null)
            throw new UnauthorizedException("Authentication required.");

        var rsvp = await _unitOfWork.Rsvps.GetByIdAsync(request.RsvpId, cancellationToken)
            ?? throw new NotFoundException("Rsvp", request.RsvpId);

        if (rsvp.UserId != _currentUser.UserId && !_currentUser.HasRole(Domain.Enums.UserRole.Admin))
            throw new ForbiddenException("You do not have permission to cancel this RSVP.");

        await _unitOfWork.Rsvps.DeleteAsync(rsvp, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return true;
    }
}
