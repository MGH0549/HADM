namespace Hadm.Domain.Entities;

public class SymptomType
{
    public Guid Id { get; set; }
    public string Key { get; set; } = "";
    public string NameEn { get; set; } = "";
    public string NameAr { get; set; } = "";
    public bool IsRedFlag { get; set; }
}
