using Hadm.Application.Contracts;

namespace Hadm.Application.Auth;

public sealed record RegisterCommand(string Email, string Password, string PreferredLanguage) : IRequest<AuthResponse>;

public sealed class RegisterValidator : AbstractValidator<RegisterCommand>
{
    public RegisterValidator(ITextLocalizer text)
    {
        RuleFor(x => x.Email).NotEmpty().WithMessage(text.Get("validation.email_required"))
            .EmailAddress().WithMessage(text.Get("validation.email_invalid"));
        RuleFor(x => x.Password).NotEmpty().WithMessage(text.Get("validation.password_required"))
            .MinimumLength(8).WithMessage(text.Get("validation.password_length"));
        RuleFor(x => x.PreferredLanguage).Must(SupportedLanguages.IsSupported)
            .WithMessage(text.Get("validation.language_invalid"));
    }
}

public sealed class RegisterHandler(IIdentityService identity, ITextLocalizer text)
    : IRequestHandler<RegisterCommand, AuthResponse>
{
    public async Task<AuthResponse> Handle(RegisterCommand request, CancellationToken cancellationToken)
    {
        var language = request.PreferredLanguage.Trim().ToLowerInvariant();
        var (user, errors) = await identity.RegisterAsync(request.Email, request.Password, language, cancellationToken);
        if (user is null)
        {
            var messages = errors.Select(code => text.Get(Map(code))).Distinct().ToArray();
            throw new AppValidationException(new Dictionary<string, string[]>
            {
                ["Password"] = messages
            });
        }

        return new AuthResponse(identity.CreateToken(user), user.Email, user.PreferredLanguage);
    }

    private static string Map(string code) => code switch
    {
        "DuplicateUserName" or "DuplicateEmail" => "auth.email_taken",
        "PasswordTooShort" => "validation.password_length",
        "PasswordRequiresDigit" => "validation.password_digit",
        "PasswordRequiresUpper" => "validation.password_upper",
        "PasswordRequiresLower" => "validation.password_lower",
        _ => "auth.registration_failed"
    };
}

public sealed record LoginCommand(string Email, string Password) : IRequest<AuthResponse>;

public sealed class LoginValidator : AbstractValidator<LoginCommand>
{
    public LoginValidator(ITextLocalizer text)
    {
        RuleFor(x => x.Email).NotEmpty().WithMessage(text.Get("validation.email_required"));
        RuleFor(x => x.Password).NotEmpty().WithMessage(text.Get("validation.password_required"));
    }
}

public sealed class LoginHandler(IIdentityService identity, ITextLocalizer text)
    : IRequestHandler<LoginCommand, AuthResponse>
{
    public async Task<AuthResponse> Handle(LoginCommand request, CancellationToken cancellationToken)
    {
        var user = await identity.ValidateCredentialsAsync(request.Email, request.Password, cancellationToken);
        if (user is null)
        {
            throw new UnauthorizedAppException(text.Get("auth.invalid_credentials"));
        }

        return new AuthResponse(identity.CreateToken(user), user.Email, user.PreferredLanguage);
    }
}
