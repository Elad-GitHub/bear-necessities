namespace Nomadica.Application.UseCases.Auth.Register;

public record RegisterRequest(string Email, string DisplayName, string Password, string SocialProfileUrl);
