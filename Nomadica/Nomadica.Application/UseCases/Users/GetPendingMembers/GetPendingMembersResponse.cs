namespace Nomadica.Application.UseCases.Users.GetPendingMembers;

public record PendingMemberDto(
    Guid Id,
    string Email,
    string DisplayName,
    string? SocialProfileUrl,
    DateTime AppliedAt);

public record GetPendingMembersResponse(IReadOnlyList<PendingMemberDto> Items);
