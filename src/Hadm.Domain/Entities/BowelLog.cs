using Hadm.Domain.Enums;

namespace Hadm.Domain.Entities;

public class BowelLog
{
    public Guid Id { get; set; }
    public string UserId { get; set; } = "";
    public DateTime LoggedAt { get; set; }
    public BristolType BristolType { get; set; }
    public Urgency Urgency { get; set; }
    public bool HasBlood { get; set; }
    public string? Note { get; set; }
}
