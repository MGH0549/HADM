using FluentValidation;
using Hadm.Application.Auth;
using Hadm.Application.Behaviors;
using Hadm.Infrastructure;
using Hadm.Infrastructure.Persistence;
using MediatR;
using Microsoft.AspNetCore.Localization;
using Microsoft.AspNetCore.RateLimiting;
using System.Security.Claims;
using System.Threading.RateLimiting;

var builder = WebApplication.CreateBuilder(args);
if (builder.Environment.IsDevelopment() && string.IsNullOrWhiteSpace(builder.Configuration["Jwt:Key"]))
    builder.Configuration["Jwt:Key"] = "Hadm-local-development-key-never-use-in-production-2026";
builder.Services.AddControllers();
builder.Services.AddMemoryCache();
builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    options.AddPolicy("ai", context => RateLimitPartition.GetFixedWindowLimiter(
        context.User.FindFirstValue(ClaimTypes.NameIdentifier) ?? context.Connection.RemoteIpAddress?.ToString() ?? "anonymous",
        _ => new FixedWindowRateLimiterOptions { PermitLimit = 10, Window = TimeSpan.FromMinutes(1), QueueLimit = 0 }));
});
builder.Services.AddOpenApi();
builder.Services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(typeof(RegisterCommand).Assembly));
builder.Services.AddValidatorsFromAssembly(typeof(RegisterCommand).Assembly);
builder.Services.AddTransient(typeof(IPipelineBehavior<,>), typeof(ValidationBehavior<,>));
builder.Services.AddTransient(typeof(IPipelineBehavior<,>), typeof(LoggingBehavior<,>));
builder.Services.AddHadmInfrastructure(builder.Configuration);
builder.Services.AddCors(options => options.AddPolicy("client", policy => policy
    .WithOrigins(builder.Configuration.GetSection("ClientOrigins").Get<string[]>() ?? ["http://localhost:4200", "https://localhost:4200"])
    .AllowAnyHeader().AllowAnyMethod()));

var app = builder.Build();
using (var scope = app.Services.CreateScope())
    await SeedData.EnsureCreatedAsync(scope.ServiceProvider.GetRequiredService<HadmDbContext>());

app.UseExceptionHandler(error => error.Run(async context =>
{
    var exception = context.Features.Get<Microsoft.AspNetCore.Diagnostics.IExceptionHandlerFeature>()?.Error;
    if (exception is not null)
        context.RequestServices.GetRequiredService<ILoggerFactory>().CreateLogger("GlobalExceptionHandler")
            .LogError(exception, "Unhandled request failure for {Method} {Path}", context.Request.Method, context.Request.Path);
    var (status, title) = exception switch
    {
        Hadm.Application.Common.AppValidationException => (StatusCodes.Status400BadRequest, "Validation failed"),
        Hadm.Application.Common.NotFoundException => (StatusCodes.Status404NotFound, "Not found"),
        Hadm.Application.Common.UnauthorizedAppException or UnauthorizedAccessException => (StatusCodes.Status401Unauthorized, "Unauthorized"),
        _ => (StatusCodes.Status500InternalServerError, "Unexpected error")
    };
    context.Response.StatusCode = status;
    context.Response.ContentType = "application/problem+json";
    var errors = exception is Hadm.Application.Common.AppValidationException validation ? validation.Errors : null;
    await context.Response.WriteAsJsonAsync(new { type = "about:blank", title, status, detail = status == 500 ? "An unexpected error occurred." : exception?.Message, errors });
}));

if (app.Environment.IsDevelopment()) app.MapOpenApi();
app.UseCors("client");
app.UseRequestLocalization(new RequestLocalizationOptions()
    .SetDefaultCulture("en")
    .AddSupportedCultures("en", "ar")
    .AddSupportedUICultures("en", "ar"));
if (!app.Environment.IsDevelopment()) app.UseHttpsRedirection();
app.UseAuthentication();
app.UseAuthorization();
app.UseRateLimiter();
app.MapControllers();
app.Run();
