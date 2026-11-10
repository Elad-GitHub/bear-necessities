using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using BearNecessities.Application.UseCases.Auth.Login;
using BearNecessities.Application.UseCases.Auth.RefreshToken;
using BearNecessities.Application.UseCases.Auth.Register;
using BearNecessities.Domain.Interfaces;

namespace BearNecessities.API.Controllers;

public class AuthController : BaseApiController
{
    private readonly RegisterHandler _registerHandler;
    private readonly LoginHandler _loginHandler;
    private readonly RefreshTokenHandler _refreshTokenHandler;
    private readonly IUnitOfWork _unitOfWork;

    public AuthController(
        RegisterHandler registerHandler,
        LoginHandler loginHandler,
        RefreshTokenHandler refreshTokenHandler,
        IUnitOfWork unitOfWork)
    {
        _registerHandler = registerHandler;
        _loginHandler = loginHandler;
        _refreshTokenHandler = refreshTokenHandler;
        _unitOfWork = unitOfWork;
    }

    [HttpPost("register")]
    [EnableRateLimiting("auth-endpoints")]
    public async Task<IActionResult> Register(
        [FromBody] RegisterRequest request,
        CancellationToken cancellationToken)
    {
        var response = await _registerHandler.HandleAsync(request, cancellationToken);
        return CreatedAtAction(nameof(Register), response);
    }

    [HttpPost("login")]
    [EnableRateLimiting("auth-endpoints")]
    public async Task<IActionResult> Login(
        [FromBody] LoginRequest request,
        CancellationToken cancellationToken)
    {
        var response = await _loginHandler.HandleAsync(request, cancellationToken);
        return Ok(response);
    }

    [HttpPost("refresh")]
    public async Task<IActionResult> Refresh(
        [FromBody] RefreshTokenRequest request,
        CancellationToken cancellationToken)
    {
        var response = await _refreshTokenHandler.HandleAsync(request, cancellationToken);
        return Ok(response);
    }

    [HttpPost("logout")]
    public async Task<IActionResult> Logout(
        [FromBody] LogoutRequest request,
        CancellationToken cancellationToken)
    {
        await _unitOfWork.Users.RevokeRefreshTokenAsync(request.RefreshToken, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return NoContent();
    }
}

public record LogoutRequest(string RefreshToken);
