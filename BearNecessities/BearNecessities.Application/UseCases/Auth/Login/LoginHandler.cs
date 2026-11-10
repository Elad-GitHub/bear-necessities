using BearNecessities.Application.DTOs.Auth;
using BearNecessities.Application.Exceptions;
using BearNecessities.Application.Interfaces;
using BearNecessities.Domain.Interfaces;

namespace BearNecessities.Application.UseCases.Auth.Login;

public sealed class LoginHandler : IRequestHandler<LoginRequest, LoginResponse>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IJwtService _jwtService;
    private readonly IPasswordHasher _passwordHasher;

    public LoginHandler(IUnitOfWork unitOfWork, IJwtService jwtService, IPasswordHasher passwordHasher)
    {
        _unitOfWork = unitOfWork;
        _jwtService = jwtService;
        _passwordHasher = passwordHasher;
    }

    public async Task<LoginResponse> HandleAsync(LoginRequest request, CancellationToken cancellationToken = default)
    {
        const string invalidMessage = "Invalid email or password.";

        var user = await _unitOfWork.Users.GetByEmailAsync(request.Email, cancellationToken);

        // Same message for "not found" and "wrong password" to prevent user enumeration.
        if (user is null || user.PasswordHash is null || !_passwordHasher.Verify(request.Password, user.PasswordHash))
            throw new UnauthorizedException(invalidMessage);

        if (!user.IsActive)
            throw new UnauthorizedException(invalidMessage);

        if (user.MembershipStatus == Domain.Enums.MembershipStatus.Pending)
            throw new UnauthorizedException("Your membership is still being reviewed. We'll let you know once you're approved!");

        if (user.MembershipStatus == Domain.Enums.MembershipStatus.Rejected)
            throw new UnauthorizedException("Your membership request was not approved.");

        await _unitOfWork.Users.RevokeAllRefreshTokensForUserAsync(user.Id, cancellationToken);

        var refreshToken = _jwtService.CreateRefreshToken(user.Id);
        await _unitOfWork.Users.AddRefreshTokenAsync(refreshToken, cancellationToken);

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return new LoginResponse(
            AccessToken: _jwtService.GenerateAccessToken(user),
            RefreshToken: refreshToken.Token,
            User: MapToDto(user));
    }

    private static AuthUserDto MapToDto(Domain.Entities.User user) =>
        new(
            Id: user.Id.Value,
            Email: user.Email,
            DisplayName: user.DisplayName,
            AvatarUrl: user.AvatarUrl,
            Roles: user.Roles.Select(r => r.ToString()).ToList(),
            SubscriptionPlan: user.SubscriptionPlan.ToString(),
            IsActive: user.IsActive);
}
