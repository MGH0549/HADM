namespace Hadm.Application.Common;

public static class RuleExtensions
{
    public static IRuleBuilderOptions<T, DateTime> ValidLogTime<T>(
        this IRuleBuilder<T, DateTime> rule,
        IDateTimeProvider clock,
        ITextLocalizer text)
    {
        return rule
            .Must(value => value != default)
            .WithMessage(text.Get("validation.logged_at_required"))
            .Must(value =>
            {
                var utc = Time.AsUtc(value);
                return utc > clock.UtcNow.AddYears(-2) && utc < clock.UtcNow.AddDays(1);
            })
            .WithMessage(text.Get("validation.logged_at_range"));
    }

    public static IRuleBuilderOptions<T, string?> NoteLength<T>(this IRuleBuilder<T, string?> rule, ITextLocalizer text) =>
        rule.MaximumLength(FieldLimits.NoteMaxLength).WithMessage(text.Get("validation.note_too_long"));
}
