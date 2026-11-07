namespace Hadm.Domain.Common;

public static class FieldLimits
{
    public const int NoteMaxLength = 1000;
    public const int FoodNameMaxLength = 80;
    public const int SeverityMin = 0;
    public const int SeverityMax = 10;
    public const int SeverePainThreshold = 8;
    public const int StressMin = 1;
    public const int StressMax = 5;
    public const decimal SleepMin = 0;
    public const decimal SleepMax = 24;
    public const int MaxFoodsPerMeal = 20;
}

public static class SupportedLanguages
{
    public const string English = "en";
    public const string Arabic = "ar";

    public static bool IsSupported(string? language) =>
        language is not null &&
        (language.Equals(English, StringComparison.OrdinalIgnoreCase) ||
         language.Equals(Arabic, StringComparison.OrdinalIgnoreCase));
}

public static class SymptomKeys
{
    public const string Bloating = "bloating";
    public const string AbdominalPain = "abdominal-pain";
    public const string Gas = "gas";
    public const string Nausea = "nausea";
    public const string Heartburn = "heartburn";
    public const string Constipation = "constipation";
    public const string Diarrhea = "diarrhea";
    public const string Cramps = "cramps";
    public const string BloodInStool = "blood-in-stool";
    public const string BlackStools = "black-stools";
    public const string PersistentVomiting = "persistent-vomiting";
    public const string UnexplainedWeightLoss = "unexplained-weight-loss";

    public static bool IsPain(string? key) =>
        key is AbdominalPain or Cramps;
}
