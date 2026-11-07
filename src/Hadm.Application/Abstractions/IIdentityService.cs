namespace Hadm.Application.Abstractions;

public sealed record IdentityUserInfo(string UserId, string Email, string PreferredLanguage);

public interface IIdentityService
{
    Task<(IdentityUserInfo? User, IReadOnlyList<string> ErrorCodes)> RegisterAsync(
        string email,
        string password,
        string preferredLanguage,
        CancellationToken cancellationToken);

    Task<IdentityUserInfo?> ValidateCredentialsAsync(
        string email,
        string password,
        CancellationToken cancellationToken);

    string CreateToken(IdentityUserInfo user);

    Task<IdentityUserInfo?> GetAsync(string userId, CancellationToken cancellationToken);

    Task SetPreferredLanguageAsync(string userId, string language, CancellationToken cancellationToken);

    Task DeleteAsync(string userId, CancellationToken cancellationToken);
}
