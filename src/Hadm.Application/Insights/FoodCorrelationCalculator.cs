namespace Hadm.Application.Insights;

public sealed record MealObservation(DateTime LoggedAt, IReadOnlyList<Guid> FoodIds);

public sealed record SymptomObservation(DateTime LoggedAt, int Severity);

public sealed record FoodCorrelationRow(
    Guid FoodId,
    double AverageInside,
    double AverageOutside,
    double Difference,
    int InsideCount,
    int OutsideCount);

public sealed record FoodCorrelationMath(
    bool EnoughData,
    int MealCount,
    int DayCount,
    int SymptomCount,
    IReadOnlyList<FoodCorrelationRow> Rows);

public static class FoodCorrelationCalculator
{
    public const int MinimumMeals = 20;
    public const int MinimumDays = 14;
    public const int MinimumSymptoms = 5;
    public const int MinimumInside = 3;
    public const int MinimumOutside = 3;
    public const int MaxRows = 8;

    public static FoodCorrelationMath Calculate(
        IReadOnlyList<MealObservation> meals,
        IReadOnlyList<SymptomObservation> symptoms,
        int windowHours,
        int offsetMinutes)
    {
        var dayCount = meals.Select(m => Time.LocalDate(m.LoggedAt, offsetMinutes))
            .Union(symptoms.Select(s => Time.LocalDate(s.LoggedAt, offsetMinutes)))
            .Distinct()
            .Count();

        var enough = (meals.Count >= MinimumMeals || dayCount >= MinimumDays)
            && symptoms.Count >= MinimumSymptoms;

        if (!enough || windowHours <= 0)
        {
            return new FoodCorrelationMath(false, meals.Count, dayCount, symptoms.Count, []);
        }

        var window = TimeSpan.FromHours(windowHours);
        var rows = new List<FoodCorrelationRow>();
        foreach (var foodId in meals.SelectMany(m => m.FoodIds).Distinct())
        {
            var mealTimes = meals.Where(m => m.FoodIds.Contains(foodId)).Select(m => Time.AsUtc(m.LoggedAt)).ToList();
            bool Inside(DateTime loggedAt)
            {
                var instant = Time.AsUtc(loggedAt);
                return mealTimes.Any(meal => instant >= meal && instant <= meal.Add(window));
            }

            var inside = symptoms.Where(s => Inside(s.LoggedAt)).ToList();
            var outside = symptoms.Where(s => !Inside(s.LoggedAt)).ToList();
            if (inside.Count < MinimumInside || outside.Count < MinimumOutside)
            {
                continue;
            }

            var averageInside = inside.Average(s => s.Severity);
            var averageOutside = outside.Average(s => s.Severity);
            rows.Add(new FoodCorrelationRow(
                foodId,
                averageInside,
                averageOutside,
                averageInside - averageOutside,
                inside.Count,
                outside.Count));
        }

        var ordered = rows
            .OrderByDescending(r => Math.Abs(r.Difference))
            .ThenByDescending(r => r.InsideCount)
            .Take(MaxRows)
            .ToList();

        return new FoodCorrelationMath(true, meals.Count, dayCount, symptoms.Count, ordered);
    }
}
