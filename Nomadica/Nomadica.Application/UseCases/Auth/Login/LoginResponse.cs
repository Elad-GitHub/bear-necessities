using Nomadica.Application.DTOs.Auth;

namespace Nomadica.Application.UseCases.Auth.Login;

public record LoginResponse(string AccessToken, string RefreshToken, AuthUserDto User);
