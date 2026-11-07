using Hadm.Application.Contracts;

namespace Hadm.Application.Account;

public sealed record GetProfileQuery : IRequest<ProfileDto>;

public sealed class GetProfileHandler(IIdentityService identity, ICurrentUserService currentUser, ITextLocalizer text)
    : IRequestHandler<GetProfileQuery, ProfileDto>
{
    public async Task<ProfileDto> Handle(GetProfileQuery request, CancellationToken cancellationToken)
    {
        var userId = TextRules.RequireUser(currentUser);
        var user = await identity.GetAsync(userId, cancellationToken)
            ?? throw new NotFoundException(text.Get("error.not_found"));
        return new ProfileDto(user.Email, user.PreferredLanguage);
    }
}

public sealed record SetLanguageCommand(string PreferredLanguage) : IRequest<ProfileDto>;

public sealed class SetLanguageValidator : AbstractValidator<SetLanguageCommand>
{
    public SetLanguageValidator(ITextLocalizer text)
    {
        RuleFor(x => x.PreferredLanguage).Must(SupportedLanguages.IsSupported)
            .WithMessage(text.Get("validation.language_invalid"));
    }
}

public sealed class SetLanguageHandler(IIdentityService identity, ICurrentUserService currentUser, ITextLocalizer text)
    : IRequestHandler<SetLanguageCommand, ProfileDto>
{
    public async Task<ProfileDto> Handle(SetLanguageCommand request, CancellationToken cancellationToken)
    {
        var userId = TextRules.RequireUser(currentUser);
        var language = request.PreferredLanguage.Trim().ToLowerInvariant();
        await identity.SetPreferredLanguageAsync(userId, language, cancellationToken);
        var user = await identity.GetAsync(userId, cancellationToken)
            ?? throw new NotFoundException(text.Get("error.not_found"));
        return new ProfileDto(user.Email, user.PreferredLanguage);
    }
}

public sealed record DeleteAccountCommand : IRequest;

public sealed class DeleteAccountHandler(IIdentityService identity, ICurrentUserService currentUser)
    : IRequestHandler<DeleteAccountCommand>
{
    public async Task Handle(DeleteAccountCommand request, CancellationToken cancellationToken)
    {
        var userId = TextRules.RequireUser(currentUser);
        await identity.DeleteAsync(userId, cancellationToken);
    }
}
