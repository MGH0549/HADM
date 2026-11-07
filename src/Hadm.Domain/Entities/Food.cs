namespace Hadm.Domain.Entities;

public class Food
{
    public Guid Id { get; set; }
    public string Key { get; set; } = "";
    public string NameEn { get; set; } = "";
    public string NameAr { get; set; } = "";
    public bool IsBuiltIn { get; set; }
    public string? UserId { get; set; }
}
