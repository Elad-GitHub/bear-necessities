using System.Net.Mail;
using BearNecessities.Application.Exceptions;
using BearNecessities.Application.Interfaces;
using BearNecessities.Domain.Entities;
using BearNecessities.Domain.Enums;
using BearNecessities.Domain.Interfaces;

namespace BearNecessities.Application.UseCases.Feedback;

public sealed record SubmitFeedbackRequest(string Kind, string Message, string? PageUrl, string? UserAgent, string? ContactEmail);

public sealed record SubmitFeedbackResponse(Guid Id);

/// <summary>Stores a bug report or idea from anyone, signed in or not; signed-in senders are linked to their account.</summary>
public sealed class SubmitFeedbackHandler : IRequestHandler<SubmitFeedbackRequest, SubmitFeedbackResponse>
{
    private const int MinMessageLength = 5;

    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUserService _currentUser;

    public SubmitFeedbackHandler(IUnitOfWork unitOfWork, ICurrentUserService currentUser)
    {
        _unitOfWork = unitOfWork;
        _currentUser = currentUser;
    }

    public async Task<SubmitFeedbackResponse> HandleAsync(SubmitFeedbackRequest request, CancellationToken cancellationToken = default)
    {
        var kind = Validate(request);
        var feedback = FeedbackMessage.Create(
            kind, request.Message, request.PageUrl, request.UserAgent, request.ContactEmail,
            _currentUser.IsAuthenticated ? _currentUser.UserId : null);

        await _unitOfWork.Feedback.AddAsync(feedback, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return new SubmitFeedbackResponse(feedback.Id);
    }

    private static FeedbackKind Validate(SubmitFeedbackRequest request)
    {
        var errors = new Dictionary<string, string[]>();
        if (!Enum.TryParse<FeedbackKind>(request.Kind, ignoreCase: true, out var kind))
            errors["kind"] = ["Choose bug, idea or other."];
        var message = request.Message?.Trim() ?? string.Empty;
        if (message.Length < MinMessageLength)
            errors["message"] = [$"Tell us a little more (at least {MinMessageLength} characters)."];
        else if (message.Length > FeedbackMessage.MaxMessageLength)
            errors["message"] = [$"Keep it under {FeedbackMessage.MaxMessageLength} characters."];
        if (!string.IsNullOrWhiteSpace(request.ContactEmail) && !IsEmail(request.ContactEmail.Trim()))
            errors["contactEmail"] = ["That email doesn't look right."];

        if (errors.Count > 0)
            throw new ValidationException(errors);
        return kind;
    }

    private static bool IsEmail(string email) =>
        email.Length <= FeedbackMessage.MaxEmailLength && MailAddress.TryCreate(email, out var parsed) && parsed.Address == email;
}

public sealed record GetOpenFeedbackRequest;

public sealed record FeedbackDto(
    Guid Id, string Kind, string Message, string? PageUrl, string? UserAgent, string? ContactEmail,
    string? SentBy, string? SentByEmail, DateTime CreatedAt);

public sealed record GetOpenFeedbackResponse(List<FeedbackDto> Items);

/// <summary>The admin inbox: feedback not yet handled, newest first, with the sender's name when signed in.</summary>
public sealed class GetOpenFeedbackHandler : IRequestHandler<GetOpenFeedbackRequest, GetOpenFeedbackResponse>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUserService _currentUser;

    public GetOpenFeedbackHandler(IUnitOfWork unitOfWork, ICurrentUserService currentUser)
    {
        _unitOfWork = unitOfWork;
        _currentUser = currentUser;
    }

    public async Task<GetOpenFeedbackResponse> HandleAsync(GetOpenFeedbackRequest request, CancellationToken cancellationToken = default)
    {
        if (!_currentUser.HasRole(UserRole.Admin))
            throw new ForbiddenException();

        var open = await _unitOfWork.Feedback.GetOpenAsync(cancellationToken);
        var senderIds = open.Where(f => f.SentBy is not null).Select(f => f.SentBy!).Distinct().ToList();
        var senders = senderIds.Count == 0
            ? new Dictionary<Guid, User>()
            : (await _unitOfWork.Users.GetByIdsAsync(senderIds, cancellationToken)).ToDictionary(user => user.Id.Value);

        var items = open.Select(f =>
        {
            var sender = f.SentBy is null ? null : senders.GetValueOrDefault(f.SentBy.Value);
            return new FeedbackDto(f.Id, f.Kind.ToString(), f.Message, f.PageUrl, f.UserAgent, f.ContactEmail,
                sender?.DisplayName, sender?.Email, f.CreatedAt);
        }).ToList();
        return new GetOpenFeedbackResponse(items);
    }
}

public sealed record HandleFeedbackRequest(Guid Id);

/// <summary>An admin marks a message as dealt with; it leaves the inbox.</summary>
public sealed class HandleFeedbackHandler : IRequestHandler<HandleFeedbackRequest, bool>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUserService _currentUser;

    public HandleFeedbackHandler(IUnitOfWork unitOfWork, ICurrentUserService currentUser)
    {
        _unitOfWork = unitOfWork;
        _currentUser = currentUser;
    }

    public async Task<bool> HandleAsync(HandleFeedbackRequest request, CancellationToken cancellationToken = default)
    {
        if (!_currentUser.HasRole(UserRole.Admin) || _currentUser.UserId is null)
            throw new ForbiddenException();

        var feedback = await _unitOfWork.Feedback.GetByIdAsync(request.Id, cancellationToken)
            ?? throw new NotFoundException("Feedback", request.Id);
        feedback.MarkHandled(_currentUser.UserId);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return true;
    }
}
