namespace BearNecessities.Application.UseCases.Users.ReviewMembership;

public record ReviewMembershipRequest(Guid UserId, bool Approve);
