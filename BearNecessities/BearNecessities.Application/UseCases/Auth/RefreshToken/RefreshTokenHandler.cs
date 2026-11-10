using BearNecessities.Application.Exceptions;
using BearNecessities.Application.Interfaces;
using BearNecessities.Domain.Interfaces;

namespace BearNecessities.Application.UseCases.Auth.RefreshToken;

public sealed class RefreshTokenHandler : IRequestHandler<RefreshTokenRequest, RefreshTokenResponse>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IJwtService _jwtService;

    public RefreshTokenHandler(IUnitOfWork unitOfWork, IJwtService jwtService)
    {
        _unitOfWork = unitOfWork;
        _jwtService = jwtService;
    }

    public async Task<RefreshTokenResponse> HandleAsync(RefreshTokenRequest request, CancellationToken cancellationToken = default)
    {
        var stored = await _unitOfWork.Users.GetRefreshTokenAsync(request.RefreshToken, cancellationToken);

        if (stored is null || !stored.IsActive)
            throw new UnauthorizedException("Invalid or expired refresh token.");

        var user = await _unitOfWork.Users.GetByIdAsync(stored.UserId, cancellationToken);

        if (user is null || !user.IsActive)
            throw new UnauthorizedException("Invalid or expired refresh token.");

        await _unitOfWork.Users.RevokeRefreshTokenAsync(request.RefreshToken, cancellationToken);

        var newRefreshToken = _jwtService.CreateRefreshToken(user.Id);
        await _unitOfWork.Users.AddRefreshTokenAsync(newRefreshToken, cancellationToken);

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return new RefreshTokenResponse(
            AccessToken: _jwtService.GenerateAccessToken(user),
            RefreshToken: newRefreshToken.Token);
    }
}
