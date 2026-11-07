using Hadm.Application.Contracts;

namespace Hadm.Application.Bowel;

public sealed record GetBowelQuery(DateTime? From, DateTime? To) : IRequest<IReadOnlyList<BowelDto>>;

public sealed class GetBowelHandler(IApplicationDbContext db, ICurrentUserService currentUser, IDateTimeProvider clock, ITextLocalizer text)
    : IRequestHandler<GetBowelQuery, IReadOnlyList<BowelDto>>
{
    public async Task<IReadOnlyList<BowelDto>> Handle(GetBowelQuery request, CancellationToken cancellationToken)
    {
        var userId = TextRules.RequireUser(currentUser);
        var to = request.To is null ? clock.UtcNow.AddDays(1) : Time.AsUtc(request.To.Value);
        var from = request.From is null ? to.AddDays(-7) : Time.AsUtc(request.From.Value);
        var logs = await db.ListAsync(
            db.BowelLogs.Where(b => b.UserId == userId && b.LoggedAt >= from && b.LoggedAt < to),
            cancellationToken);
        return logs.OrderByDescending(b => b.LoggedAt).Select(b => BowelMapper.ToDto(b, text)).ToList();
    }
}

public sealed record CreateBowelCommand(DateTime LoggedAt, BristolType BristolType, Urgency Urgency, bool HasBlood, string? Note)
    : IRequest<BowelDto>;

public sealed class CreateBowelValidator : AbstractValidator<CreateBowelCommand>
{
    public CreateBowelValidator(ITextLocalizer text, IDateTimeProvider clock)
    {
        RuleFor(x => x.LoggedAt).ValidLogTime(clock, text);
        RuleFor(x => x.BristolType).IsInEnum().WithMessage(text.Get("validation.bristol_invalid"));
        RuleFor(x => x.Urgency).IsInEnum().WithMessage(text.Get("validation.urgency_invalid"));
        RuleFor(x => x.Note).NoteLength(text);
    }
}

public sealed class CreateBowelHandler(IApplicationDbContext db, ICurrentUserService currentUser, ITextLocalizer text)
    : IRequestHandler<CreateBowelCommand, BowelDto>
{
    public async Task<BowelDto> Handle(CreateBowelCommand request, CancellationToken cancellationToken)
    {
        var userId = TextRules.RequireUser(currentUser);
        var log = new BowelLog
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            LoggedAt = Time.AsUtc(request.LoggedAt),
            BristolType = request.BristolType,
            Urgency = request.Urgency,
            HasBlood = request.HasBlood,
            Note = TextRules.CleanNote(request.Note)
        };
        db.Add(log);
        await db.SaveChangesAsync(cancellationToken);
        return BowelMapper.ToDto(log, text);
    }
}

public sealed record UpdateBowelCommand(Guid Id, DateTime LoggedAt, BristolType BristolType, Urgency Urgency, bool HasBlood, string? Note)
    : IRequest<BowelDto>;

public sealed class UpdateBowelValidator : AbstractValidator<UpdateBowelCommand>
{
    public UpdateBowelValidator(ITextLocalizer text, IDateTimeProvider clock)
    {
        RuleFor(x => x.LoggedAt).ValidLogTime(clock, text);
        RuleFor(x => x.BristolType).IsInEnum().WithMessage(text.Get("validation.bristol_invalid"));
        RuleFor(x => x.Urgency).IsInEnum().WithMessage(text.Get("validation.urgency_invalid"));
        RuleFor(x => x.Note).NoteLength(text);
    }
}

public sealed class UpdateBowelHandler(IApplicationDbContext db, ICurrentUserService currentUser, ITextLocalizer text)
    : IRequestHandler<UpdateBowelCommand, BowelDto>
{
    public async Task<BowelDto> Handle(UpdateBowelCommand request, CancellationToken cancellationToken)
    {
        var userId = TextRules.RequireUser(currentUser);
        var log = await db.FirstOrDefaultAsync(
            db.BowelLogs.Where(b => b.Id == request.Id && b.UserId == userId),
            cancellationToken) ?? throw new NotFoundException(text.Get("error.not_found"));
        log.LoggedAt = Time.AsUtc(request.LoggedAt);
        log.BristolType = request.BristolType;
        log.Urgency = request.Urgency;
        log.HasBlood = request.HasBlood;
        log.Note = TextRules.CleanNote(request.Note);
        await db.SaveChangesAsync(cancellationToken);
        return BowelMapper.ToDto(log, text);
    }
}

public sealed record DeleteBowelCommand(Guid Id) : IRequest;

public sealed class DeleteBowelHandler(IApplicationDbContext db, ICurrentUserService currentUser, ITextLocalizer text)
    : IRequestHandler<DeleteBowelCommand>
{
    public async Task Handle(DeleteBowelCommand request, CancellationToken cancellationToken)
    {
        var userId = TextRules.RequireUser(currentUser);
        var log = await db.FirstOrDefaultAsync(
            db.BowelLogs.Where(b => b.Id == request.Id && b.UserId == userId),
            cancellationToken) ?? throw new NotFoundException(text.Get("error.not_found"));
        db.Remove(log);
        await db.SaveChangesAsync(cancellationToken);
    }
}

internal static class BowelMapper
{
    public static BowelDto ToDto(BowelLog log, ITextLocalizer text)
    {
        var flagged = RedFlagRules.IsRedFlagBowel(log.HasBlood, log.Note);
        return new BowelDto(
            log.Id,
            log.LoggedAt,
            log.BristolType,
            log.Urgency,
            log.HasBlood,
            log.Note,
            flagged,
            flagged ? text.Get("redflag.notice") : null);
    }
}
