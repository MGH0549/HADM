using System.Security.Claims;
using Hadm.Application.Abstractions;
using Hadm.Application.Account;
using Hadm.Application.Auth;
using Hadm.Application.Bowel;
using Hadm.Application.Contracts;
using Hadm.Application.Common;
using Hadm.Application.Daily;
using Hadm.Application.Foods;
using Hadm.Application.Meals;
using Hadm.Application.Symptoms;
using Hadm.Application.Timeline;
using Hadm.Application.Insights;
using Hadm.Domain.Entities;
using Hadm.Domain.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MediatR;

namespace Hadm.WebApi.Controllers;

[ApiController, Route("api/auth")]
public sealed class AuthController(ISender sender) : ControllerBase
{
    [AllowAnonymous, HttpPost("register")]
    public async Task<ActionResult<AuthResponse>> Register(RegisterCommand command, CancellationToken ct) =>
        Ok(await sender.Send(command, ct));

    [AllowAnonymous, HttpPost("login")]
    public async Task<ActionResult<AuthResponse>> Login(LoginCommand command, CancellationToken ct) =>
        Ok(await sender.Send(command, ct));
}

[ApiController, Authorize, Route("api/meals")]
public sealed class MealsController(ISender sender) : ControllerBase
{
    [HttpGet] public async Task<ActionResult<IReadOnlyList<MealDto>>> Get([FromQuery] DateTime? from, [FromQuery] DateTime? to, CancellationToken ct) => Ok(await sender.Send(new GetMealsQuery(from, to), ct));
    [HttpPost] public async Task<ActionResult<MealDto>> Create(CreateMealCommand command, CancellationToken ct) => Ok(await sender.Send(command, ct));
    [HttpPut("{id:guid}")] public async Task<ActionResult<MealDto>> Update(Guid id, UpdateMealRequest body, CancellationToken ct) => Ok(await sender.Send(new UpdateMealCommand(id, body.LoggedAt, body.MealType, body.Note, body.FoodIds), ct));
    [HttpDelete("{id:guid}")] public async Task<IActionResult> Delete(Guid id, CancellationToken ct) { await sender.Send(new DeleteMealCommand(id), ct); return NoContent(); }
}

public sealed record UpdateMealRequest(DateTime LoggedAt, MealType MealType, string? Note, IReadOnlyList<Guid> FoodIds);

[ApiController, Authorize, Route("api/symptoms")]
public sealed class SymptomsController(ISender sender) : ControllerBase
{
    [HttpGet] public async Task<ActionResult<IReadOnlyList<SymptomDto>>> Get([FromQuery] DateTime? from, [FromQuery] DateTime? to, CancellationToken ct) => Ok(await sender.Send(new GetSymptomsQuery(from, to), ct));
    [HttpPost] public async Task<ActionResult<SymptomDto>> Create(CreateSymptomCommand command, CancellationToken ct) => Ok(await sender.Send(command, ct));
    [HttpPut("{id:guid}")] public async Task<ActionResult<SymptomDto>> Update(Guid id, UpdateSymptomRequest body, CancellationToken ct) => Ok(await sender.Send(new UpdateSymptomCommand(id, body.LoggedAt, body.SymptomTypeId, body.Severity, body.Note), ct));
    [HttpDelete("{id:guid}")] public async Task<IActionResult> Delete(Guid id, CancellationToken ct) { await sender.Send(new DeleteSymptomCommand(id), ct); return NoContent(); }
}
public sealed record UpdateSymptomRequest(DateTime LoggedAt, Guid SymptomTypeId, int Severity, string? Note);

[ApiController, Authorize, Route("api/bowel")]
public sealed class BowelController(ISender sender) : ControllerBase
{
    [HttpGet] public async Task<ActionResult<IReadOnlyList<BowelDto>>> Get([FromQuery] DateTime? from, [FromQuery] DateTime? to, CancellationToken ct) => Ok(await sender.Send(new GetBowelQuery(from, to), ct));
    [HttpPost] public async Task<ActionResult<BowelDto>> Create(CreateBowelCommand command, CancellationToken ct) => Ok(await sender.Send(command, ct));
    [HttpPut("{id:guid}")] public async Task<ActionResult<BowelDto>> Update(Guid id, UpdateBowelRequest body, CancellationToken ct) => Ok(await sender.Send(new UpdateBowelCommand(id, body.LoggedAt, body.BristolType, body.Urgency, body.HasBlood, body.Note), ct));
    [HttpDelete("{id:guid}")] public async Task<IActionResult> Delete(Guid id, CancellationToken ct) { await sender.Send(new DeleteBowelCommand(id), ct); return NoContent(); }
}
public sealed record UpdateBowelRequest(DateTime LoggedAt, BristolType BristolType, Urgency Urgency, bool HasBlood, string? Note);

[ApiController, Authorize, Route("api")]
public sealed class DiaryController(ISender sender, IApplicationDbContext db, ICurrentUserService currentUser, IReportGenerator reports, ITextLocalizer text, IAiService ai, IAiPreferencesService aiPreferences) : ControllerBase
{
    [HttpGet("foods")] public async Task<ActionResult<IReadOnlyList<FoodDto>>> Foods(CancellationToken ct) => Ok(await sender.Send(new GetFoodsQuery(), ct));
    [HttpPost("foods")] public async Task<ActionResult<FoodDto>> CreateFood(CreateFoodCommand command, CancellationToken ct) => Ok(await sender.Send(command, ct));
    [HttpGet("symptom-types")] public async Task<ActionResult<IReadOnlyList<SymptomTypeDto>>> SymptomTypes(CancellationToken ct) => Ok(await sender.Send(new GetSymptomTypesQuery(), ct));
    [HttpGet("daily/{date}")] public async Task<ActionResult<DailyDto?>> Daily(DateOnly date, CancellationToken ct) => Ok(await sender.Send(new GetDailyQuery(date), ct));
    [HttpPut("daily/{date}")] public async Task<ActionResult<DailyDto>> SaveDaily(DateOnly date, DailyRequest body, CancellationToken ct) => Ok(await sender.Send(new UpsertDailyCommand(date, body.SleepHours, body.StressLevel, body.Exercise, body.MedicationTaken, body.Note), ct));
    [HttpGet("timeline")] public async Task<ActionResult<TimelineDto>> Timeline([FromQuery] DateOnly? date, [FromQuery] int offsetMinutes = 0, CancellationToken ct = default) => Ok(await sender.Send(new GetTimelineQuery(date ?? DateOnly.FromDateTime(DateTime.UtcNow), Math.Clamp(offsetMinutes, -840, 840)), ct));
    [HttpGet("insights/severity-trend")] public async Task<ActionResult<SeverityTrendDto>> SeverityTrend([FromQuery] int days = 30, [FromQuery] int offsetMinutes = 0, CancellationToken ct = default)
    {
        var uid = RequiredUser(); days = Math.Clamp(days, 7, 90); offsetMinutes = Time.ClampOffset(offsetMinutes);
        var endDate = Time.LocalDate(DateTime.UtcNow, offsetMinutes); var from = Time.LocalDayStartUtc(endDate.AddDays(-days + 1), offsetMinutes);
        var logs = await db.ListAsync(db.SymptomLogs.Where(x => x.UserId == uid && x.LoggedAt >= from), ct);
        var points = Enumerable.Range(0, days).Select(i => { var date = endDate.AddDays(-days + 1 + i); var entries = logs.Where(x => Time.LocalDate(x.LoggedAt, offsetMinutes) == date).ToList(); return new SeverityPointDto(date, entries.Count == 0 ? null : entries.Average(x => x.Severity), entries.Count); }).ToList();
        return Ok(new SeverityTrendDto(days, points));
    }
    [HttpGet("insights/symptom-frequency")] public async Task<ActionResult<SymptomFrequencyDto>> SymptomFrequency([FromQuery] int days = 30, [FromQuery] int offsetMinutes = 0, CancellationToken ct = default)
    {
        var uid = RequiredUser(); days = Math.Clamp(days, 7, 90); offsetMinutes = Time.ClampOffset(offsetMinutes);
        var endDate = Time.LocalDate(DateTime.UtcNow, offsetMinutes); var from = Time.LocalDayStartUtc(endDate.AddDays(-days + 1), offsetMinutes);
        var logs = await db.ListAsync(db.SymptomLogs.Where(x => x.UserId == uid && x.LoggedAt >= from), ct);
        var types = await db.ListAsync(db.SymptomTypes, ct); var map = types.ToDictionary(x => x.Id);
        var items = logs.GroupBy(x => x.SymptomTypeId).Where(group => map.ContainsKey(group.Key)).Select(group =>
        {
            var type = map[group.Key];
            return new SymptomFrequencyItemDto(type.Id, type.Key, type.NameEn, type.NameAr, group.Count(), group.Average(x => x.Severity));
        }).OrderByDescending(x => x.Count).ToList();
        return Ok(new SymptomFrequencyDto(days, items));
    }
    [HttpGet("insights/bristol-distribution")] public async Task<ActionResult<BristolDistributionDto>> BristolDistribution([FromQuery] int days = 30, [FromQuery] int offsetMinutes = 0, CancellationToken ct = default)
    {
        var uid = RequiredUser(); days = Math.Clamp(days, 7, 90); offsetMinutes = Time.ClampOffset(offsetMinutes);
        var endDate = Time.LocalDate(DateTime.UtcNow, offsetMinutes); var from = Time.LocalDayStartUtc(endDate.AddDays(-days + 1), offsetMinutes);
        var logs = await db.ListAsync(db.BowelLogs.Where(x => x.UserId == uid && x.LoggedAt >= from), ct);
        var counts = logs.GroupBy(x => x.BristolType).ToDictionary(x => x.Key, x => x.Count());
        var items = Enum.GetValues<BristolType>().Select(type => new BristolSliceDto(type, counts.GetValueOrDefault(type))).ToList();
        return Ok(new BristolDistributionDto(days, items));
    }
    [HttpGet("insights/context")] public async Task<ActionResult<ContextInsightDto>> ContextInsight([FromQuery] int days = 30, [FromQuery] int offsetMinutes = 0, CancellationToken ct = default)
    {
        var uid = RequiredUser(); days = Math.Clamp(days, 7, 90); offsetMinutes = Time.ClampOffset(offsetMinutes); var end = Time.LocalDate(DateTime.UtcNow, offsetMinutes); var start = end.AddDays(-days + 1);
        var startUtc = Time.LocalDayStartUtc(start, offsetMinutes);
        var daily = await db.ListAsync(db.DailyLogs.Where(x => x.UserId == uid && x.Date >= start && x.Date <= end), ct);
        var symptoms = await db.ListAsync(db.SymptomLogs.Where(x => x.UserId == uid && x.LoggedAt >= startUtc), ct);
        var byDate = symptoms.GroupBy(x => Time.LocalDate(x.LoggedAt, offsetMinutes)).ToDictionary(x => x.Key, x => (Average: x.Average(s => s.Severity), Count: x.Count()));
        var entries = daily.Where(x => byDate.ContainsKey(x.Date)).Select(x => (Daily: x, Severity: byDate[x.Date].Average, Count: byDate[x.Date].Count)).ToList();
        static NamedAverageDto Make(string key, IEnumerable<(DailyLog Daily, double Severity, int Count)> group)
        {
            var rows = group.ToList(); var count = rows.Sum(x => x.Count);
            return new NamedAverageDto(key, count == 0 ? 0 : rows.Sum(x => x.Severity * x.Count) / count, count);
        }
        var stress = Enumerable.Range(1, 5).Select(level => Make($"stress.{level}", entries.Where(x => x.Daily.StressLevel == level))).Where(x => x.Count > 0).ToList();
        var sleep = new[] { Make("sleep.under6", entries.Where(x => x.Daily.SleepHours is < 6)), Make("sleep.6to8", entries.Where(x => x.Daily.SleepHours is >= 6 and <= 8)), Make("sleep.over8", entries.Where(x => x.Daily.SleepHours is > 8)) }.Where(x => x.Count > 0).ToList();
        var exercise = new[] { Make("exercise.yes", entries.Where(x => x.Daily.Exercise)), Make("exercise.no", entries.Where(x => !x.Daily.Exercise)) }.Where(x => x.Count > 0).ToList();
        return Ok(new ContextInsightDto(days, stress, sleep, exercise));
    }
    [HttpGet("insights/food-correlation")] public async Task<ActionResult<FoodCorrelationDto>> Correlation([FromQuery] int windowHours = 6, [FromQuery] int offsetMinutes = 0, CancellationToken ct = default)
    {
        var uid = RequiredUser(); windowHours = new[] { 2, 6, 24 }.Contains(windowHours) ? windowHours : 6; offsetMinutes = Math.Clamp(offsetMinutes, -840, 840);
        var since = DateTime.UtcNow.AddDays(-90);
        var meals = await db.ListAsync(db.MealLogs.Where(x => x.UserId == uid && x.LoggedAt >= since), ct);
        var links = await db.ListAsync(db.MealLogFoods.Where(x => meals.Select(m => m.Id).Contains(x.MealLogId)), ct);
        var symptoms = await db.ListAsync(db.SymptomLogs.Where(x => x.UserId == uid && x.LoggedAt >= since), ct);
        var math = FoodCorrelationCalculator.Calculate(meals.Select(m => new MealObservation(m.LoggedAt, links.Where(l => l.MealLogId == m.Id).Select(l => l.FoodId).ToArray())).ToList(), symptoms.Select(s => new SymptomObservation(s.LoggedAt, s.Severity)).ToList(), windowHours, offsetMinutes);
        var ids = math.Rows.Select(x => x.FoodId).ToList(); var foods = await db.ListAsync(db.Foods.Where(f => ids.Contains(f.Id)), ct); var map = foods.ToDictionary(x => x.Id);
        var items = math.Rows.Where(x => map.ContainsKey(x.FoodId)).Select(x => { var f = map[x.FoodId]; return new FoodCorrelationItemDto(f.Id, f.Key, f.NameEn, f.NameAr, x.AverageInside, x.AverageOutside, x.Difference, x.InsideCount, x.OutsideCount); }).ToList();
        return Ok(new FoodCorrelationDto(math.EnoughData, math.MealCount, math.DayCount, math.SymptomCount, FoodCorrelationCalculator.MinimumMeals, FoodCorrelationCalculator.MinimumDays, FoodCorrelationCalculator.MinimumSymptoms, windowHours, items));
    }
    [HttpGet("reports/pdf")] public async Task<IActionResult> Report([FromQuery] DateOnly from, [FromQuery] DateOnly to, [FromQuery] string? lang, [FromQuery] int offsetMinutes = 0, CancellationToken ct = default)
    {
        if (to < from || to.DayNumber - from.DayNumber > 366) return BadRequest(new { message = text.Get("validation.date_range") });
        var uid = RequiredUser(); offsetMinutes = Time.ClampOffset(offsetMinutes); var start = Time.LocalDayStartUtc(from, offsetMinutes); var end = Time.LocalDayStartUtc(to.AddDays(1), offsetMinutes);
        var meals = await db.ListAsync(db.MealLogs.Where(x => x.UserId == uid && x.LoggedAt >= start && x.LoggedAt < end), ct);
        var symptoms = await db.ListAsync(db.SymptomLogs.Where(x => x.UserId == uid && x.LoggedAt >= start && x.LoggedAt < end), ct);
        var bowel = await db.ListAsync(db.BowelLogs.Where(x => x.UserId == uid && x.LoggedAt >= start && x.LoggedAt < end), ct);
        var isAr = (lang ?? HttpContext.Request.Headers.AcceptLanguage.ToString()).StartsWith("ar", StringComparison.OrdinalIgnoreCase);
        var mealIds = meals.Select(x => x.Id).ToList();
        var mealLinks = await db.ListAsync(db.MealLogFoods.Where(x => mealIds.Contains(x.MealLogId)), ct);
        var foodIds = mealLinks.Select(x => x.FoodId).Distinct().ToList();
        var reportFoods = await db.ListAsync(db.Foods.Where(x => foodIds.Contains(x.Id)), ct);
        var foodNames = reportFoods.ToDictionary(x => x.Id, x => isAr ? x.NameAr : x.NameEn);
        var mealLines = meals.OrderBy(x => x.LoggedAt).Select(meal =>
            $"{meal.LoggedAt:yyyy-MM-dd HH:mm} ? {meal.MealType} ? {string.Join(", ", mealLinks.Where(link => link.MealLogId == meal.Id).Select(link => foodNames.GetValueOrDefault(link.FoodId, "")))}")
            .ToList();
        var symptomTypes = await db.ListAsync(db.SymptomTypes, ct);
        var typeNames = symptomTypes.ToDictionary(x => x.Id, x => isAr ? x.NameAr : x.NameEn);
        var sections = new List<ReportSection>
        {
            new(isAr ? "\u0627\u0644\u0648\u062c\u0628\u0627\u062a" : "Meals", mealLines),
            new(isAr ? "\u0627\u0644\u0623\u0639\u0631\u0627\u0636" : "Symptoms", symptoms.OrderBy(x => x.LoggedAt).Select(x => $"{x.LoggedAt:yyyy-MM-dd HH:mm} ? {typeNames.GetValueOrDefault(x.SymptomTypeId, "Symptom")} ? {x.Severity}/10 ? {x.Note}").ToList()),
            new(isAr ? "\u062d\u0631\u0643\u0627\u062a \u0627\u0644\u0623\u0645\u0639\u0627\u0621" : "Bowel movements", bowel.OrderBy(x => x.LoggedAt).Select(x => $"{x.LoggedAt:yyyy-MM-dd HH:mm} ? Bristol {(int)x.BristolType} ? {(x.HasBlood ? "Blood noted" : x.Urgency.ToString())} ? {x.Note}").ToList())
        };
        var summaryLines = new List<string>
        {
            isAr ? $"\u0627\u0644\u0648\u062c\u0628\u0627\u062a: {meals.Count}" : $"Meals: {meals.Count}",
            isAr ? $"\u0627\u0644\u0623\u0639\u0631\u0627\u0636: {symptoms.Count}" : $"Symptoms: {symptoms.Count}",
            isAr ? $"\u062d\u0631\u0643\u0627\u062a \u0627\u0644\u0623\u0645\u0639\u0627\u0621: {bowel.Count}" : $"Bowel movements: {bowel.Count}"
        };
        if (ai.IsConfigured)
        {
            try
            {
                if ((await aiPreferences.GetAsync(uid, ct)).Enabled)
                {
                    var facts = System.Text.Json.JsonSerializer.Serialize(new
                    {
                        from, to, meals = meals.Count, symptoms = symptoms.Count,
                        averageSeverity = symptoms.Count == 0 ? (double?)null : Math.Round(symptoms.Average(x => x.Severity), 1),
                        bowelMovements = bowel.Count,
                        bristolCounts = bowel.GroupBy(x => (int)x.BristolType).Select(g => new { type = g.Key, count = g.Count() })
                    });
                    var prompt = "Write one short, neutral paragraph explaining only these SQL-computed diary counts for a doctor report. Do not infer causes, diagnose, or advise treatment. Say the counts describe recorded entries only. Write in " + (isAr ? "Arabic" : "English") + ". Return the required JSON.";
                    var schema = new { type = "object", properties = new { summary = new { type = "string" } }, required = new[] { "summary" }, additionalProperties = false };
                    using var generated = System.Text.Json.JsonDocument.Parse(await ai.GenerateJsonAsync(prompt, facts, schema, 350, ct));
                    var paragraph = generated.RootElement.GetProperty("summary").GetString();
                    if (!string.IsNullOrWhiteSpace(paragraph)) summaryLines.Add(paragraph);
                }
            }
            catch (Exception exception) when (exception is HttpRequestException or System.Text.Json.JsonException or InvalidOperationException or TaskCanceledException or Microsoft.Data.SqlClient.SqlException)
            {
                // A report remains available when the optional AI provider is unavailable.
            }
        }
        var document = new ReportDocument(isAr, isAr ? "\u0645\u0630\u0643\u0631\u0627\u062a \u0647\u0636\u0645" : "Hadm digestive diary", isAr ? "\u0645\u0644\u062e\u0635 \u0642\u0627\u0628\u0644 \u0644\u0644\u0645\u0634\u0627\u0631\u0643\u0629 \u0645\u0639 \u0637\u0628\u064a\u0628\u0643" : "A summary to share with your doctor", $"{from:yyyy-MM-dd} ? {to:yyyy-MM-dd}", isAr ? "\u0647\u0630\u0627 \u0627\u0644\u062a\u0642\u0631\u064a\u0631 \u0644\u0627 \u064a\u0642\u062f\u0645 \u062a\u0634\u062e\u064a\u0635\u0627\u064b \u0623\u0648 \u0646\u0635\u064a\u062d\u0629 \u0637\u0628\u064a\u0629. \u0631\u0627\u062c\u0639 \u0637\u0628\u064a\u0628\u0643 \u0628\u0634\u0623\u0646 \u0645\u062e\u0627\u0648\u0641\u0643." : "This report does not provide a diagnosis or medical advice. Consult your doctor about concerns.", isAr ? "\u0645\u0644\u062e\u0635 \u0627\u0644\u0641\u062a\u0631\u0629" : "Period summary", summaryLines, sections, isAr ? "\u0623\u0646\u0634\u0623\u0647 \u062a\u0637\u0628\u064a\u0642 \u0647\u0636\u0645" : "Generated by Hadm");
        return File(reports.Generate(document), "application/pdf", $"hadm-report-{from:yyyyMMdd}-{to:yyyyMMdd}.pdf");
    }
    private string RequiredUser() => currentUser.UserId ?? throw new UnauthorizedAccessException();
}
public sealed record DailyRequest(decimal? SleepHours, int? StressLevel, bool Exercise, bool MedicationTaken, string? Note);

[ApiController, Authorize, Route("api/me")]
public sealed class AccountController(ISender sender) : ControllerBase
{
    [HttpGet] public async Task<ActionResult<ProfileDto>> Profile(CancellationToken ct) => Ok(await sender.Send(new GetProfileQuery(), ct));
    [HttpPut("language")] public async Task<ActionResult<ProfileDto>> Language(LanguageRequest request, CancellationToken ct) => Ok(await sender.Send(new SetLanguageCommand(request.PreferredLanguage), ct));
    [HttpDelete] public async Task<IActionResult> Delete(CancellationToken ct) { await sender.Send(new DeleteAccountCommand(), ct); return NoContent(); }
}
public sealed record LanguageRequest(string PreferredLanguage);
