using Hadm.Application.Contracts;
using Hadm.Application.Daily;
using Hadm.Application.Meals;

namespace Hadm.Application.Timeline;

public sealed record GetTimelineQuery(DateOnly Date, int OffsetMinutes) : IRequest<TimelineDto>;

public sealed class GetTimelineHandler(IApplicationDbContext db, ICurrentUserService currentUser)
    : IRequestHandler<GetTimelineQuery, TimelineDto>
{
    public async Task<TimelineDto> Handle(GetTimelineQuery request, CancellationToken cancellationToken)
    {
        var userId = TextRules.RequireUser(currentUser);
        var start = Time.LocalDayStartUtc(request.Date, request.OffsetMinutes);
        var end = start.AddDays(1);

        var meals = await db.ListAsync(
            db.MealLogs.Where(m => m.UserId == userId && m.LoggedAt >= start && m.LoggedAt < end),
            cancellationToken);
        var mealDtos = await MealMapper.MapAsync(db, meals, cancellationToken);
        var symptoms = await db.ListAsync(
            db.SymptomLogs.Where(s => s.UserId == userId && s.LoggedAt >= start && s.LoggedAt < end),
            cancellationToken);
        var types = await db.ListAsync(db.SymptomTypes, cancellationToken);
        var typeMap = types.ToDictionary(t => t.Id);
        var bowel = await db.ListAsync(
            db.BowelLogs.Where(b => b.UserId == userId && b.LoggedAt >= start && b.LoggedAt < end),
            cancellationToken);
        var daily = await db.FirstOrDefaultAsync(
            db.DailyLogs.Where(d => d.UserId == userId && d.Date == request.Date),
            cancellationToken);

        var events = new List<TimelineEventDto>();
        foreach (var meal in mealDtos)
        {
            var namesEn = string.Join(", ", meal.Foods.Select(f => f.NameEn));
            var namesAr = string.Join("، ", meal.Foods.Select(f => f.NameAr));
            events.Add(new TimelineEventDto(
                "meal", meal.Id, meal.LoggedAt, meal.MealType.ToString(), null, "", "",
                namesEn, namesAr, null, false));
        }

        foreach (var symptom in symptoms)
        {
            typeMap.TryGetValue(symptom.SymptomTypeId, out var type);
            var flagged = RedFlagRules.IsRedFlagSymptom(type?.IsRedFlag == true, type?.Key, symptom.Severity, symptom.Note);
            events.Add(new TimelineEventDto(
                "symptom",
                symptom.Id,
                symptom.LoggedAt,
                "symptom",
                type?.Key,
                type?.NameEn ?? "",
                type?.NameAr ?? "",
                symptom.Note ?? "",
                symptom.Note ?? "",
                symptom.Severity,
                flagged));
        }

        foreach (var item in bowel)
        {
            var flagged = RedFlagRules.IsRedFlagBowel(item.HasBlood, item.Note);
            events.Add(new TimelineEventDto(
                "bowel",
                item.Id,
                item.LoggedAt,
                item.BristolType.ToString(),
                null,
                item.Urgency.ToString(),
                item.Urgency.ToString(),
                item.HasBlood ? "blood" : item.Note ?? "",
                item.HasBlood ? "blood" : item.Note ?? "",
                null,
                flagged));
        }

        return new TimelineDto(
            request.Date,
            daily is null ? null : DailyMapper.ToDto(daily),
            events.OrderBy(e => e.LoggedAt).ToList());
    }
}
