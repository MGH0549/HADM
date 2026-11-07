namespace Hadm.Domain.Entities;

public class DailyLog
{
    public Guid Id { get; set; }
    public string UserId { get; set; } = "";
    public DateOnly Date { get; set; }
    public decimal? SleepHours { get; set; }
    public int? StressLevel { get; set; }
    public bool Exercise { get; set; }
    public bool MedicationTaken { get; set; }
    public string? Note { get; set; }
}
