using Hadm.Application.Contracts;

namespace Hadm.Application.Daily;

public sealed record GetDailyQuery(DateOnly Date) : IRequest<DailyDto?>;

public sealed class GetDailyHandler(IApplicationDbContext db, ICurrentUserService currentUser)
    : IRequestHandler<GetDailyQuery, DailyDto?>
{
    public async Task<DailyDto?> Handle(GetDailyQuery request, CancellationToken cancellationToken)
    {
        var userId = TextRules.RequireUser(currentUser);
        var log = await db.FirstOrDefaultAsync(
            db.DailyLogs.Where(d => d.UserId == userId && d.Date == request.Date),
            cancellationToken);
        return log is null ? null : DailyMapper.ToDto(log);
    }
}

public sealed record UpsertDailyCommand(
    DateOnly Date,
    decimal? SleepHours,
    int? StressLevel,
    bool Exercise,
    bool MedicationTaken,
    string? Note) : IRequest<DailyDto>;

public sealed class UpsertDailyValidator : AbstractValidator<UpsertDailyCommand>
{
    public UpsertDailyValidator(ITextLocalizer text, IDateTimeProvider clock)
    {
        RuleFor(x => x.Date)
            .Must(date => date > DateOnly.FromDateTime(clock.UtcNow).AddYears(-2) && date < DateOnly.FromDateTime(clock.UtcNow).AddDays(2))
            .WithMessage(text.Get("validation.date_range"));
        RuleFor(x => x.SleepHours).InclusiveBetween(FieldLimits.SleepMin, FieldLimits.SleepMax)
            .When(x => x.SleepHours.HasValue)
            .WithMessage(text.Get("validation.sleep_range"));
        RuleFor(x => x.StressLevel).InclusiveBetween(FieldLimits.StressMin, FieldLimits.StressMax)
            .When(x => x.StressLevel.HasValue)
            .WithMessage(text.Get("validation.stress_range"));
        RuleFor(x => x.Note).NoteLength(text);
    }
}

public sealed class UpsertDailyHandler(IApplicationDbContext db, ICurrentUserService currentUser)
    : IRequestHandler<UpsertDailyCommand, DailyDto>
{
    public async Task<DailyDto> Handle(UpsertDailyCommand request, CancellationToken cancellationToken)
    {
        var userId = TextRules.RequireUser(currentUser);
        var log = await db.FirstOrDefaultAsync(
            db.DailyLogs.Where(d => d.UserId == userId && d.Date == request.Date),
            cancellationToken);
        if (log is null)
        {
            log = new DailyLog { Id = Guid.NewGuid(), UserId = userId, Date = request.Date };
            db.Add(log);
        }

        log.SleepHours = request.SleepHours;
        log.StressLevel = request.StressLevel;
        log.Exercise = request.Exercise;
        log.MedicationTaken = request.MedicationTaken;
        log.Note = TextRules.CleanNote(request.Note);
        await db.SaveChangesAsync(cancellationToken);
        return DailyMapper.ToDto(log);
    }
}

internal static class DailyMapper
{
    public static DailyDto ToDto(DailyLog log) =>
        new(log.Id, log.Date, log.SleepHours, log.StressLevel, log.Exercise, log.MedicationTaken, log.Note);
}
