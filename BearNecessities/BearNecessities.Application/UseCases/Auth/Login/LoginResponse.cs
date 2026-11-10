using BearNecessities.Application.DTOs.Auth;

namespace BearNecessities.Application.UseCases.Auth.Login;

public record LoginResponse(string AccessToken, string RefreshToken, AuthUserDto User);
