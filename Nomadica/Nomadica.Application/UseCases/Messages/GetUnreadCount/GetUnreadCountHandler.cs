using Nomadica.Application.Exceptions;
using Nomadica.Application.Interfaces;
using Nomadica.Domain.Interfaces;

namespace Nomadica.Application.UseCases.Messages.GetUnreadCount;

public sealed class GetUnreadCountHandler
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUserService _currentUser;

    public GetUnreadCountHandler(IUnitOfWork unitOfWork, ICurrentUserService currentUser)
    {
        _unitOfWork = unitOfWork;
        _currentUser = currentUser;
    }

    public async Task<GetUnreadCountResponse> HandleAsync(CancellationToken cancellationToken = default)
    {
        if (!_currentUser.IsAuthenticated || _currentUser.UserId is null)
            throw new UnauthorizedException("Authentication required.");

        var count = await _unitOfWork.Conversations.GetUnreadCountAsync(
            _currentUser.UserId, cancellationToken);

        return new GetUnreadCountResponse(count);
    }
}
