using Hadm.Domain.Enums;

namespace Hadm.Domain.Entities;

public class MealLog
{
    public Guid Id { get; set; }
    public string UserId { get; set; } = "";
    public DateTime LoggedAt { get; set; }
    public MealType MealType { get; set; }
    public string? Note { get; set; }
    public ICollection<MealLogFood> Foods { get; set; } = new List<MealLogFood>();
}
