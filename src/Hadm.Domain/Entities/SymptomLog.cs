namespace Hadm.Domain.Entities;

public class SymptomLog
{
    public Guid Id { get; set; }
    public string UserId { get; set; } = "";
    public DateTime LoggedAt { get; set; }
    public Guid SymptomTypeId { get; set; }
    public SymptomType? SymptomType { get; set; }
    public int Severity { get; set; }
    public string? Note { get; set; }
}
