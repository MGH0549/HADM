namespace Hadm.Application.Contracts;

public sealed record AuthResponse(string Token, string Email, string PreferredLanguage);

public sealed record ProfileDto(string Email, string PreferredLanguage);

public sealed record FoodDto(Guid Id, string Key, string NameEn, string NameAr, bool IsBuiltIn);

public sealed record MealDto(Guid Id, DateTime LoggedAt, MealType MealType, string? Note, IReadOnlyList<FoodDto> Foods);

public sealed record SymptomTypeDto(Guid Id, string Key, string NameEn, string NameAr, bool IsRedFlag);

public sealed record SymptomDto(
    Guid Id,
    DateTime LoggedAt,
    Guid SymptomTypeId,
    string SymptomKey,
    string NameEn,
    string NameAr,
    int Severity,
    string? Note,
    bool RedFlag,
    string? RedFlagMessage);

public sealed record BowelDto(
    Guid Id,
    DateTime LoggedAt,
    BristolType BristolType,
    Urgency Urgency,
    bool HasBlood,
    string? Note,
    bool RedFlag,
    string? RedFlagMessage);

public sealed record DailyDto(
    Guid Id,
    DateOnly Date,
    decimal? SleepHours,
    int? StressLevel,
    bool Exercise,
    bool MedicationTaken,
    string? Note);

public sealed record TimelineEventDto(
    string Kind,
    Guid Id,
    DateTime LoggedAt,
    string TitleKey,
    string? SymptomKey,
    string NameEn,
    string NameAr,
    string DetailEn,
    string DetailAr,
    int? Severity,
    bool RedFlag);

public sealed record TimelineDto(DateOnly Date, DailyDto? Daily, IReadOnlyList<TimelineEventDto> Events);

public sealed record SeverityPointDto(DateOnly Date, double? AverageSeverity, int Count);

public sealed record SeverityTrendDto(int Days, IReadOnlyList<SeverityPointDto> Points);

public sealed record FoodCorrelationItemDto(
    Guid FoodId,
    string Key,
    string NameEn,
    string NameAr,
    double AverageInside,
    double AverageOutside,
    double Difference,
    int InsideCount,
    int OutsideCount);

public sealed record FoodCorrelationDto(
    bool EnoughData,
    int MealCount,
    int DayCount,
    int SymptomCount,
    int RequiredMeals,
    int RequiredDays,
    int RequiredSymptoms,
    int WindowHours,
    IReadOnlyList<FoodCorrelationItemDto> Items);

public sealed record SymptomFrequencyItemDto(
    Guid SymptomTypeId,
    string Key,
    string NameEn,
    string NameAr,
    int Count,
    double AverageSeverity);

public sealed record SymptomFrequencyDto(int Days, IReadOnlyList<SymptomFrequencyItemDto> Items);

public sealed record BristolSliceDto(BristolType Type, int Count);

public sealed record BristolDistributionDto(int Days, IReadOnlyList<BristolSliceDto> Items);

public sealed record NamedAverageDto(string Key, double AverageSeverity, int Count);

public sealed record ContextInsightDto(
    int Days,
    IReadOnlyList<NamedAverageDto> ByStress,
    IReadOnlyList<NamedAverageDto> BySleep,
    IReadOnlyList<NamedAverageDto> ByExercise);

public sealed record ReportFile(byte[] Content, string FileName);
