using Microsoft.AspNetCore.Identity;

namespace Hadm.Infrastructure.Persistence;

public sealed class HadmUser : IdentityUser
{
    public string PreferredLanguage { get; set; } = "en";
    public bool AiEnabled { get; set; }
    public DateTimeOffset? AiConsentAt { get; set; }
}
