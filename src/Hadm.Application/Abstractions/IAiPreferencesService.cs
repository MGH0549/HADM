namespace Hadm.Application.Abstractions;

public sealed record AiPreferences(bool Enabled, DateTimeOffset? ConsentAt);

public interface IAiPreferencesService
{
    Task<AiPreferences> GetAsync(string userId, CancellationToken cancellationToken);
    Task<AiPreferences> SetAsync(string userId, bool enabled, CancellationToken cancellationToken);
}
