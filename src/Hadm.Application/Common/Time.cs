namespace Hadm.Application.Common;

public static class Time
{
    public static DateTime AsUtc(DateTime value) => value.Kind switch
    {
        DateTimeKind.Utc => value,
        DateTimeKind.Local => value.ToUniversalTime(),
        _ => DateTime.SpecifyKind(value, DateTimeKind.Utc)
    };

    public static int ClampOffset(int offsetMinutes) => Math.Clamp(offsetMinutes, -14 * 60, 14 * 60);

    public static DateTime LocalDayStartUtc(DateOnly date, int offsetMinutes)
    {
        var offset = ClampOffset(offsetMinutes);
        return DateTime.SpecifyKind(date.ToDateTime(TimeOnly.MinValue).AddMinutes(-offset), DateTimeKind.Utc);
    }

    public static DateOnly LocalDate(DateTime utc, int offsetMinutes) =>
        DateOnly.FromDateTime(AsUtc(utc).AddMinutes(ClampOffset(offsetMinutes)));
}
