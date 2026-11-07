namespace Hadm.Domain.Entities;

public class MealLogFood
{
    public Guid MealLogId { get; set; }
    public Guid FoodId { get; set; }
    public MealLog? MealLog { get; set; }
    public Food? Food { get; set; }
}
