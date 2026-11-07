using Hadm.Application.Abstractions;
using Hadm.Infrastructure.Persistence;
using Hadm.Infrastructure.Reports;
using Hadm.Infrastructure.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;
using System.Text;

namespace Hadm.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddHadmInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        var connection = configuration.GetConnectionString("Hadm")
            ?? "Server=(localdb)\\MSSQLLocalDB;Database=Hadm;Trusted_Connection=True;MultipleActiveResultSets=true;TrustServerCertificate=True";
        services.AddDbContext<HadmDbContext>(options => options.UseSqlServer(connection));
        services.AddScoped<IApplicationDbContext>(provider => provider.GetRequiredService<HadmDbContext>());
        services.AddIdentityCore<HadmUser>(options =>
            {
                options.User.RequireUniqueEmail = true;
                options.Password.RequiredLength = 8;
                options.Password.RequireDigit = true;
                options.Password.RequireUppercase = true;
                options.Password.RequireLowercase = true;
                options.Password.RequireNonAlphanumeric = false;
            })
            .AddEntityFrameworkStores<HadmDbContext>()
            .AddSignInManager();

        var key = configuration["Jwt:Key"] ?? throw new InvalidOperationException("Configure Jwt:Key as a secret containing at least 32 bytes.");
        var issuer = configuration["Jwt:Issuer"] ?? "Hadm";
        var audience = configuration["Jwt:Audience"] ?? "Hadm.Client";
        services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme).AddJwtBearer(options =>
        {
            options.TokenValidationParameters = new TokenValidationParameters
            {
                ValidateIssuer = true, ValidIssuer = issuer,
                ValidateAudience = true, ValidAudience = audience,
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(key)),
                ValidateLifetime = true, ClockSkew = TimeSpan.FromSeconds(30)
            };
        });
        services.AddAuthorization();
        services.AddHttpContextAccessor();
        services.AddScoped<ICurrentUserService, CurrentUserService>();
        services.AddScoped<IIdentityService, IdentityService>();
        services.AddScoped<IAiPreferencesService, AiPreferencesService>();
        services.AddScoped<ITextLocalizer, TextLocalizer>();
        services.AddSingleton<IDateTimeProvider, SystemDateTimeProvider>();
        services.AddSingleton<IReportGenerator, QuestPdfReportGenerator>();
        services.AddHttpClient<IAiService, AnthropicAiService>();
        return services;
    }
}
