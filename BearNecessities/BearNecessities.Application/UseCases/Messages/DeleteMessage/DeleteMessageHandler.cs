using BearNecessities.Application.Exceptions;
using BearNecessities.Application.Interfaces;
using BearNecessities.Domain.Interfaces;

namespace BearNecessities.Application.UseCases.Messages.DeleteMessage;

public sealed class DeleteMessageHandler : IRequestHandler<DeleteMessageRequest, bool>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUserService _currentUser;

    public DeleteMessageHandler(IUnitOfWork unitOfWork, ICurrentUserService currentUser)
    {
        _unitOfWork = unitOfWork;
        _currentUser = currentUser;
    }

    public async Task<bool> HandleAsync(DeleteMessageRequest request, CancellationToken cancellationToken = default)
    {
        if (!_currentUser.IsAuthenticated || _currentUser.UserId is null)
            throw new UnauthorizedException("Authentication required.");

        var message = await _unitOfWork.Conversations.GetMessageByIdAsync(request.MessageId, cancellationToken)
            ?? throw new NotFoundException("Message", request.MessageId);

        if (message.SenderId != _currentUser.UserId)
            throw new ForbiddenException("You can only delete your own messages.");

        if (message.IsDeleted)
            return true;

        message.MarkAsDeleted();
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return true;
    }
}
