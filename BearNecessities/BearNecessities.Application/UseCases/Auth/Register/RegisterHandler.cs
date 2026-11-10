using BearNecessities.Application.Exceptions;
using BearNecessities.Application.Interfaces;
using BearNecessities.Domain.Entities;
using BearNecessities.Domain.Interfaces;

namespace BearNecessities.Application.UseCases.Auth.Register;

public sealed class RegisterHandler : IRequestHandler<RegisterRequest, RegisterResponse>
{
    private static readonly string[] AllowedSocialHosts =
        ["instagram.com", "www.instagram.com", "facebook.com", "www.facebook.com", "m.facebook.com"];

    private readonly IUnitOfWork _unitOfWork;
    private readonly IPasswordHasher _passwordHasher;

    public RegisterHandler(IUnitOfWork unitOfWork, IPasswordHasher passwordHasher)
    {
        _unitOfWork = unitOfWork;
        _passwordHasher = passwordHasher;
    }

    public async Task<RegisterResponse> HandleAsync(RegisterRequest request, CancellationToken cancellationToken = default)
    {
        ValidateInput(request);

        if (await _unitOfWork.Users.ExistsByEmailAsync(request.Email, cancellationToken))
            throw new ValidationException(new Dictionary<string, string[]>
            {
                ["email"] = ["Email already exists."]
            });

        // New members wait for manual approval — community exclusivity by design
        var user = User.Create(request.Email, request.DisplayName);
        user.SetPasswordHash(_passwordHasher.Hash(request.Password));
        user.SetSocialProfileUrl(request.SocialProfileUrl.Trim());

        await _unitOfWork.Users.AddAsync(user, cancellationToken);
        await _unitOfWork.Users.AddRoleAssignmentAsync(
            UserRoleAssignment.Create(user.Id, Domain.Enums.UserRole.Traveler), cancellationToken);

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return new RegisterResponse(
            MembershipStatus: user.MembershipStatus.ToString(),
            Message: "Thanks for applying! We review every profile to keep the community authentic. You'll be able to log in once you're approved.");
    }

    private static void ValidateInput(RegisterRequest request)
    {
        var errors = new Dictionary<string, string[]>();

        if (string.IsNullOrWhiteSpace(request.Email) || !IsValidEmail(request.Email))
            errors["email"] = ["Email must be a valid email address."];

        if (string.IsNullOrWhiteSpace(request.DisplayName))
            errors["displayName"] = ["Display name is required."];

        if (string.IsNullOrWhiteSpace(request.Password) || request.Password.Length < 8)
            errors["password"] = ["Password must be at least 8 characters."];

        if (!IsValidSocialProfileUrl(request.SocialProfileUrl))
            errors["socialProfileUrl"] = ["Please provide a link to your Instagram or Facebook profile."];

        if (errors.Count > 0)
            throw new ValidationException(errors);
    }

    private static bool IsValidSocialProfileUrl(string? url)
    {
        if (string.IsNullOrWhiteSpace(url)) return false;
        if (!Uri.TryCreate(url.Trim(), UriKind.Absolute, out var parsed)) return false;
        if (parsed.Scheme != Uri.UriSchemeHttps && parsed.Scheme != Uri.UriSchemeHttp) return false;
        return AllowedSocialHosts.Contains(parsed.Host.ToLowerInvariant());
    }

    private static bool IsValidEmail(string email)
    {
        try
        {
            var addr = new System.Net.Mail.MailAddress(email);
            return addr.Address == email;
        }
        catch
        {
            return false;
        }
    }
}
