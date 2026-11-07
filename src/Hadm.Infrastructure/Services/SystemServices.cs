using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Hadm.Application.Abstractions;
using Hadm.Infrastructure.Persistence;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;

namespace Hadm.Infrastructure.Services;

public sealed class SystemDateTimeProvider : IDateTimeProvider
{
    public DateTime UtcNow => DateTime.UtcNow;
}

public sealed class CurrentUserService(IHttpContextAccessor context) : ICurrentUserService
{
    public string? UserId => context.HttpContext?.User.FindFirstValue(ClaimTypes.NameIdentifier)
        ?? context.HttpContext?.User.FindFirstValue(JwtRegisteredClaimNames.Sub);
}

public sealed class IdentityService(UserManager<HadmUser> users, HadmDbContext db, IConfiguration configuration) : IIdentityService
{
    public async Task<(IdentityUserInfo? User, IReadOnlyList<string> ErrorCodes)> RegisterAsync(
        string email, string password, string preferredLanguage, CancellationToken cancellationToken)
    {
        var user = new HadmUser { Id = Guid.NewGuid().ToString("N"), UserName = email.Trim(), Email = email.Trim(), PreferredLanguage = preferredLanguage };
        var result = await users.CreateAsync(user, password);
        return result.Succeeded
            ? (ToInfo(user), [])
            : (null, result.Errors.Select(e => e.Code).ToArray());
    }

    public async Task<IdentityUserInfo?> ValidateCredentialsAsync(string email, string password, CancellationToken cancellationToken)
    {
        var user = await users.FindByEmailAsync(email.Trim());
        return user is not null && await users.CheckPasswordAsync(user, password) ? ToInfo(user) : null;
    }

    public string CreateToken(IdentityUserInfo user)
    {
        var key = configuration["Jwt:Key"];
        if (string.IsNullOrWhiteSpace(key) || Encoding.UTF8.GetByteCount(key) < 32)
            throw new InvalidOperationException("Jwt:Key must be configured with at least 32 bytes.");
        var issuer = configuration["Jwt:Issuer"] ?? "Hadm";
        var audience = configuration["Jwt:Audience"] ?? "Hadm.Client";
        var claims = new[]
        {
            new Claim(JwtRegisteredClaimNames.Sub, user.UserId), new Claim(ClaimTypes.NameIdentifier, user.UserId),
            new Claim(JwtRegisteredClaimNames.Email, user.Email), new Claim("preferred_language", user.PreferredLanguage)
        };
        var credentials = new SigningCredentials(new SymmetricSecurityKey(Encoding.UTF8.GetBytes(key)), SecurityAlgorithms.HmacSha256);
        var token = new JwtSecurityToken(issuer, audience, claims, DateTime.UtcNow, DateTime.UtcNow.AddHours(12), credentials);
        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    public async Task<IdentityUserInfo?> GetAsync(string userId, CancellationToken cancellationToken) =>
        await users.FindByIdAsync(userId) is { } user ? ToInfo(user) : null;

    public async Task SetPreferredLanguageAsync(string userId, string language, CancellationToken cancellationToken)
    {
        var user = await users.FindByIdAsync(userId) ?? throw new InvalidOperationException("Account not found.");
        user.PreferredLanguage = language;
        await users.UpdateAsync(user);
    }

    public async Task DeleteAsync(string userId, CancellationToken cancellationToken)
    {
        var user = await users.FindByIdAsync(userId);
        if (user is null) return;
        var meals = await db.MealLogs.Where(x => x.UserId == userId).ToListAsync(cancellationToken);
        var mealIds = meals.Select(x => x.Id).ToList();
        db.MealLogFoods.RemoveRange(db.MealLogFoods.Where(x => mealIds.Contains(x.MealLogId)));
        db.MealLogs.RemoveRange(meals);
        db.SymptomLogs.RemoveRange(db.SymptomLogs.Where(x => x.UserId == userId));
        db.BowelLogs.RemoveRange(db.BowelLogs.Where(x => x.UserId == userId));
        db.DailyLogs.RemoveRange(db.DailyLogs.Where(x => x.UserId == userId));
        db.Foods.RemoveRange(db.Foods.Where(x => x.UserId == userId));
        await db.SaveChangesAsync(cancellationToken);
        await users.DeleteAsync(user);
    }

    private static IdentityUserInfo ToInfo(HadmUser user) => new(user.Id, user.Email ?? "", user.PreferredLanguage);
}

public sealed class AiPreferencesService(HadmDbContext db) : IAiPreferencesService
{
    public async Task<AiPreferences> GetAsync(string userId, CancellationToken cancellationToken)
    {
        var user = await db.Users.FindAsync([userId], cancellationToken)
            ?? throw new KeyNotFoundException("Account was not found.");
        return new AiPreferences(user.AiEnabled, user.AiConsentAt);
    }

    public async Task<AiPreferences> SetAsync(string userId, bool enabled, CancellationToken cancellationToken)
    {
        var user = await db.Users.FindAsync([userId], cancellationToken)
            ?? throw new KeyNotFoundException("Account was not found.");
        user.AiEnabled = enabled;
        user.AiConsentAt = enabled ? DateTimeOffset.UtcNow : null;
        await db.SaveChangesAsync(cancellationToken);
        return new AiPreferences(user.AiEnabled, user.AiConsentAt);
    }
}

public sealed class TextLocalizer(IHttpContextAccessor context) : ITextLocalizer
{
    private static readonly IReadOnlyDictionary<string, (string En, string Ar)> Messages = new Dictionary<string, (string, string)>
    {
        ["error.not_found"] = ("The requested item was not found.", "العنصر المطلوب غير موجود."),
        ["error.food_not_found"] = ("One or more foods were not found.", "لم يتم العثور على بعض الأطعمة."),
        ["error.symptom_type_not_found"] = ("The symptom type was not found.", "نوع العرض غير موجود."),
        ["error.duplicate_food"] = ("This food is already in your list.", "هذا الطعام موجود بالفعل في قائمتك."),
        ["auth.invalid_credentials"] = ("Email or password is incorrect.", "البريد الإلكتروني أو كلمة المرور غير صحيحة."),
        ["auth.email_taken"] = ("An account with this email already exists.", "يوجد حساب بهذا البريد الإلكتروني بالفعل."),
        ["auth.registration_failed"] = ("We could not create this account.", "تعذر إنشاء الحساب."),
        ["redflag.notice"] = ("This may need medical attention. Please contact a doctor.", "قد يتطلب ذلك عناية طبية. يُرجى التواصل مع الطبيب."),
        ["validation.language_invalid"] = ("Choose English or Arabic.", "اختر الإنجليزية أو العربية."),
        ["validation.email_required"] = ("Email is required.", "البريد الإلكتروني مطلوب."),
        ["validation.email_invalid"] = ("Enter a valid email address.", "أدخل بريداً إلكترونياً صحيحاً."),
        ["validation.password_required"] = ("Password is required.", "كلمة المرور مطلوبة."),
        ["validation.password_length"] = ("Password must contain at least 8 characters.", "يجب ألا تقل كلمة المرور عن 8 أحرف."),
        ["validation.password_digit"] = ("Password must contain a number.", "يجب أن تحتوي كلمة المرور على رقم."),
        ["validation.password_upper"] = ("Password must contain an uppercase letter.", "يجب أن تحتوي كلمة المرور على حرف كبير."),
        ["validation.password_lower"] = ("Password must contain a lowercase letter.", "يجب أن تحتوي كلمة المرور على حرف صغير."),
        ["validation.food_name_required"] = ("Food name is required.", "اسم الطعام مطلوب."),
        ["validation.food_name_length"] = ("Food name is too long.", "اسم الطعام طويل جداً."),
        ["validation.note_length"] = ("Note is too long.", "الملاحظة طويلة جداً."),
        ["validation.note_too_long"] = ("Note is too long.", "الملاحظة طويلة جداً."),
        ["validation.logged_at_required"] = ("Choose a date and time.", "اختر التاريخ والوقت."),
        ["validation.logged_at_range"] = ("Choose a valid recent date and time.", "اختر تاريخاً ووقتاً صالحين."),
        ["validation.meal_type_invalid"] = ("Choose a valid meal type.", "اختر نوع وجبة صالحاً."),
        ["validation.foods_required"] = ("Add between 1 and 20 foods.", "أضف من 1 إلى 20 نوعاً من الطعام."),
        ["validation.log_time"] = ("Choose a valid recent date and time.", "اختر تاريخاً ووقتاً صالحين."),
        ["validation.severity_range"] = ("Severity must be between 0 and 10.", "يجب أن تكون الشدة بين 0 و10."),
        ["validation.symptom_type_required"] = ("Choose a symptom.", "اختر عرضاً."),
        ["validation.bristol_invalid"] = ("Choose a Bristol type from 1 to 7.", "اختر نوعاً من 1 إلى 7."),
        ["validation.urgency_invalid"] = ("Choose a valid urgency level.", "اختر مستوى إلحاح صالحاً."),
        ["validation.date_range"] = ("Choose a valid date.", "اختر تاريخاً صالحاً."),
        ["validation.sleep_range"] = ("Sleep must be between 0 and 24 hours.", "يجب أن يكون النوم بين 0 و24 ساعة."),
        ["validation.stress_range"] = ("Stress must be between 1 and 5.", "يجب أن يكون التوتر بين 1 و5."),
        ["validation.meal_type"] = ("Choose a valid meal type.", "اختر نوع وجبة صالحاً."),
        ["validation.food_required"] = ("Add at least one food.", "أضف طعاماً واحداً على الأقل.")
    };

    public string Get(string key) => Get(Language, key);
    public string Get(string language, string key) => Messages.TryGetValue(key, out var message)
        ? language.Equals("ar", StringComparison.OrdinalIgnoreCase) ? message.Ar : message.En
        : key;
    public string Format(string language, string key, params object[] args) => string.Format(Get(language, key), args);

    private string Language => context.HttpContext?.Request.Headers.AcceptLanguage.ToString().StartsWith("ar", StringComparison.OrdinalIgnoreCase) == true ? "ar" : "en";
}
