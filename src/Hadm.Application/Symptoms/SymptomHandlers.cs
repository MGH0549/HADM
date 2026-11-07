using Hadm.Application.Contracts;

namespace Hadm.Application.Symptoms;

public sealed record GetSymptomTypesQuery : IRequest<IReadOnlyList<SymptomTypeDto>>;

public sealed class GetSymptomTypesHandler(IApplicationDbContext db)
    : IRequestHandler<GetSymptomTypesQuery, IReadOnlyList<SymptomTypeDto>>
{
    public async Task<IReadOnlyList<SymptomTypeDto>> Handle(GetSymptomTypesQuery request, CancellationToken cancellationToken)
    {
        var types = await db.ListAsync(db.SymptomTypes.OrderBy(t => t.Key), cancellationToken);
        return types.Select(t => new SymptomTypeDto(t.Id, t.Key, t.NameEn, t.NameAr, t.IsRedFlag)).ToList();
    }
}

public sealed record GetSymptomsQuery(DateTime? From, DateTime? To) : IRequest<IReadOnlyList<SymptomDto>>;

public sealed class GetSymptomsHandler(
    IApplicationDbContext db,
    ICurrentUserService currentUser,
    IDateTimeProvider clock,
    ITextLocalizer text) : IRequestHandler<GetSymptomsQuery, IReadOnlyList<SymptomDto>>
{
    public async Task<IReadOnlyList<SymptomDto>> Handle(GetSymptomsQuery request, CancellationToken cancellationToken)
    {
        var userId = TextRules.RequireUser(currentUser);
        var to = request.To is null ? clock.UtcNow.AddDays(1) : Time.AsUtc(request.To.Value);
        var from = request.From is null ? to.AddDays(-7) : Time.AsUtc(request.From.Value);
        var logs = await db.ListAsync(
            db.SymptomLogs.Where(s => s.UserId == userId && s.LoggedAt >= from && s.LoggedAt < to),
            cancellationToken);
        var types = await TypeMap.LoadAsync(db, cancellationToken);
        return logs.OrderByDescending(s => s.LoggedAt).Select(s => SymptomMapper.ToDto(s, types, text)).ToList();
    }
}

public sealed record CreateSymptomCommand(DateTime LoggedAt, Guid SymptomTypeId, int Severity, string? Note)
    : IRequest<SymptomDto>;

public sealed class CreateSymptomValidator : AbstractValidator<CreateSymptomCommand>
{
    public CreateSymptomValidator(ITextLocalizer text, IDateTimeProvider clock)
    {
        RuleFor(x => x.LoggedAt).ValidLogTime(clock, text);
        RuleFor(x => x.SymptomTypeId).NotEmpty().WithMessage(text.Get("validation.symptom_type_required"));
        RuleFor(x => x.Severity).InclusiveBetween(FieldLimits.SeverityMin, FieldLimits.SeverityMax)
            .WithMessage(text.Get("validation.severity_range"));
        RuleFor(x => x.Note).NoteLength(text);
    }
}

public sealed class CreateSymptomHandler(IApplicationDbContext db, ICurrentUserService currentUser, ITextLocalizer text)
    : IRequestHandler<CreateSymptomCommand, SymptomDto>
{
    public async Task<SymptomDto> Handle(CreateSymptomCommand request, CancellationToken cancellationToken)
    {
        var userId = TextRules.RequireUser(currentUser);
        var type = await db.FirstOrDefaultAsync(db.SymptomTypes.Where(t => t.Id == request.SymptomTypeId), cancellationToken)
            ?? throw new NotFoundException(text.Get("error.symptom_type_not_found"));
        var log = new SymptomLog
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            LoggedAt = Time.AsUtc(request.LoggedAt),
            SymptomTypeId = type.Id,
            Severity = request.Severity,
            Note = TextRules.CleanNote(request.Note)
        };
        db.Add(log);
        await db.SaveChangesAsync(cancellationToken);
        log.SymptomType = type;
        return SymptomMapper.ToDto(log, new Dictionary<Guid, SymptomType> { [type.Id] = type }, text);
    }
}

public sealed record UpdateSymptomCommand(Guid Id, DateTime LoggedAt, Guid SymptomTypeId, int Severity, string? Note)
    : IRequest<SymptomDto>;

public sealed class UpdateSymptomValidator : AbstractValidator<UpdateSymptomCommand>
{
    public UpdateSymptomValidator(ITextLocalizer text, IDateTimeProvider clock)
    {
        RuleFor(x => x.LoggedAt).ValidLogTime(clock, text);
        RuleFor(x => x.SymptomTypeId).NotEmpty().WithMessage(text.Get("validation.symptom_type_required"));
        RuleFor(x => x.Severity).InclusiveBetween(FieldLimits.SeverityMin, FieldLimits.SeverityMax)
            .WithMessage(text.Get("validation.severity_range"));
        RuleFor(x => x.Note).NoteLength(text);
    }
}

public sealed class UpdateSymptomHandler(IApplicationDbContext db, ICurrentUserService currentUser, ITextLocalizer text)
    : IRequestHandler<UpdateSymptomCommand, SymptomDto>
{
    public async Task<SymptomDto> Handle(UpdateSymptomCommand request, CancellationToken cancellationToken)
    {
        var userId = TextRules.RequireUser(currentUser);
        var log = await db.FirstOrDefaultAsync(
            db.SymptomLogs.Where(s => s.Id == request.Id && s.UserId == userId),
            cancellationToken) ?? throw new NotFoundException(text.Get("error.not_found"));
        var type = await db.FirstOrDefaultAsync(db.SymptomTypes.Where(t => t.Id == request.SymptomTypeId), cancellationToken)
            ?? throw new NotFoundException(text.Get("error.symptom_type_not_found"));
        log.LoggedAt = Time.AsUtc(request.LoggedAt);
        log.SymptomTypeId = type.Id;
        log.Severity = request.Severity;
        log.Note = TextRules.CleanNote(request.Note);
        await db.SaveChangesAsync(cancellationToken);
        return SymptomMapper.ToDto(log, new Dictionary<Guid, SymptomType> { [type.Id] = type }, text);
    }
}

public sealed record DeleteSymptomCommand(Guid Id) : IRequest;

public sealed class DeleteSymptomHandler(IApplicationDbContext db, ICurrentUserService currentUser, ITextLocalizer text)
    : IRequestHandler<DeleteSymptomCommand>
{
    public async Task Handle(DeleteSymptomCommand request, CancellationToken cancellationToken)
    {
        var userId = TextRules.RequireUser(currentUser);
        var log = await db.FirstOrDefaultAsync(
            db.SymptomLogs.Where(s => s.Id == request.Id && s.UserId == userId),
            cancellationToken) ?? throw new NotFoundException(text.Get("error.not_found"));
        db.Remove(log);
        await db.SaveChangesAsync(cancellationToken);
    }
}

internal static class TypeMap
{
    public static async Task<Dictionary<Guid, SymptomType>> LoadAsync(IApplicationDbContext db, CancellationToken cancellationToken)
    {
        var types = await db.ListAsync(db.SymptomTypes, cancellationToken);
        return types.ToDictionary(t => t.Id);
    }
}

internal static class SymptomMapper
{
    public static SymptomDto ToDto(SymptomLog log, IReadOnlyDictionary<Guid, SymptomType> types, ITextLocalizer text)
    {
        types.TryGetValue(log.SymptomTypeId, out var type);
        var key = type?.Key ?? "";
        var flagged = RedFlagRules.IsRedFlagSymptom(type?.IsRedFlag == true, key, log.Severity, log.Note);
        return new SymptomDto(
            log.Id,
            log.LoggedAt,
            log.SymptomTypeId,
            key,
            type?.NameEn ?? "",
            type?.NameAr ?? "",
            log.Severity,
            log.Note,
            flagged,
            flagged ? text.Get("redflag.notice") : null);
    }
}
