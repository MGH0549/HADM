using Hadm.Domain.Common;

namespace Hadm.Domain.RedFlags;

public static class RedFlagRules
{
    private static readonly string[] Needles =
    [
        "blood", "black stool", "black stools", "melena", "vomit", "vomiting", "weight loss", "severe pain",
        "دم", "براز أسود", "براز اسود", "قيء", "تقيؤ", "فقدان الوزن", "نقص الوزن", "نزيف", "ألم شديد"
    ];

    private static readonly string[] BloodNegations = ["no blood", "without blood", "not blood", "لا دم", "بدون دم", "دون دم"];
    private static readonly string[] OtherNegations =
    [
        "no vomiting", "not vomiting", "without vomiting", "no vomit", "no severe pain", "without severe pain",
        "no weight loss", "without weight loss", "لا قيء", "لا يوجد قيء", "لا ألم شديد", "لا يوجد ألم شديد",
        "لا فقدان للوزن", "لا يوجد فقدان للوزن"
    ];

    public static bool NoteSuggestsRedFlag(string? note)
    {
        if (string.IsNullOrWhiteSpace(note)) return false;
        var bloodNegated = BloodNegations.Any(n => note.Contains(n, StringComparison.OrdinalIgnoreCase));
        var otherNegated = OtherNegations.Any(n => note.Contains(n, StringComparison.OrdinalIgnoreCase));
        foreach (var needle in Needles)
        {
            if (!note.Contains(needle, StringComparison.OrdinalIgnoreCase)) continue;
            if (bloodNegated && IsBloodNeedle(needle)) continue;
            if (otherNegated && needle is "severe pain" or "ألم شديد" or "vomit" or "vomiting" or "weight loss" or "فقدان الوزن" or "نقص الوزن") continue;
            return true;
        }
        return false;
    }

    public static bool IsRedFlagSymptom(bool typeIsRedFlag, string? symptomKey, int severity, string? note) =>
        typeIsRedFlag
        || (SymptomKeys.IsPain(symptomKey) && severity >= FieldLimits.SeverePainThreshold)
        || NoteSuggestsRedFlag(note);

    public static bool IsRedFlagBowel(bool hasBlood, string? note) => hasBlood || NoteSuggestsRedFlag(note);

    private static bool IsBloodNeedle(string needle) =>
        needle.Equals("blood", StringComparison.OrdinalIgnoreCase) || needle is "دم" or "نزيف";
}
