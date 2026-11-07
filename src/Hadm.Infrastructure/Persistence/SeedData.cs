using Hadm.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Hadm.Infrastructure.Persistence;

public static class SeedData
{
    public static async Task EnsureCreatedAsync(HadmDbContext db, CancellationToken cancellationToken = default)
    {
        await db.Database.MigrateAsync(cancellationToken);
        if (!await db.SymptomTypes.AnyAsync(cancellationToken))
        {
            db.SymptomTypes.AddRange(
                Symptom("bloating", "Bloating", "انتفاخ"), Symptom("abdominal-pain", "Abdominal pain", "ألم البطن"),
                Symptom("gas", "Gas", "غازات"), Symptom("nausea", "Nausea", "غثيان"),
                Symptom("heartburn", "Heartburn or reflux", "حرقة أو ارتجاع"), Symptom("constipation", "Constipation", "إمساك"),
                Symptom("diarrhea", "Diarrhea", "إسهال"), Symptom("cramps", "Cramps", "تقلصات"),
                Symptom("severe-pain", "Severe pain", "ألم شديد", true), Symptom("persistent-vomiting", "Persistent vomiting", "قيء مستمر", true),
                Symptom("blood-in-stool", "Blood in stool", "دم في البراز", true), Symptom("black-stools", "Black stools", "براز أسود", true),
                Symptom("weight-loss", "Unexplained weight loss", "فقدان وزن غير مبرر", true));
        }

        if (!await db.Foods.AnyAsync(f => f.IsBuiltIn, cancellationToken))
        {
            var foods = new (string Key, string En, string Ar)[]
            {
                ("dairy", "Dairy", "ألبان"), ("gluten", "Gluten", "غلوتين"), ("onion-garlic", "Onions and garlic", "بصل وثوم"),
                ("beans", "Beans and legumes", "بقوليات"), ("spicy", "Spicy food", "طعام حار"), ("fried", "Fried food", "طعام مقلي"),
                ("coffee", "Coffee", "قهوة"), ("alcohol", "Alcohol", "كحول"), ("sweeteners", "Artificial sweeteners", "محليات صناعية"),
                ("carbonated", "Carbonated drinks", "مشروبات غازية"), ("rice", "Rice", "أرز"), ("bread", "Bread", "خبز"),
                ("chicken", "Chicken", "دجاج"), ("tomato", "Tomato", "طماطم"), ("mint", "Mint", "نعناع")
            };
            db.Foods.AddRange(foods.Select(x => new Food { Id = Guid.NewGuid(), Key = x.Key, NameEn = x.En, NameAr = x.Ar, IsBuiltIn = true }));
        }
        await db.SaveChangesAsync(cancellationToken);
    }

    private static SymptomType Symptom(string key, string en, string ar, bool redFlag = false) =>
        new() { Id = Guid.NewGuid(), Key = key, NameEn = en, NameAr = ar, IsRedFlag = redFlag };
}
