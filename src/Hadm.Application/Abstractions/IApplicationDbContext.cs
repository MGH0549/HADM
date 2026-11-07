namespace Hadm.Application.Abstractions;

public interface IApplicationDbContext
{
    IQueryable<MealLog> MealLogs { get; }
    IQueryable<Food> Foods { get; }
    IQueryable<MealLogFood> MealLogFoods { get; }
    IQueryable<SymptomLog> SymptomLogs { get; }
    IQueryable<SymptomType> SymptomTypes { get; }
    IQueryable<BowelLog> BowelLogs { get; }
    IQueryable<DailyLog> DailyLogs { get; }

    void Add(object entity);
    void Remove(object entity);
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
    Task<List<T>> ListAsync<T>(IQueryable<T> query, CancellationToken cancellationToken = default);
    Task<T?> FirstOrDefaultAsync<T>(IQueryable<T> query, CancellationToken cancellationToken = default) where T : class;
    Task<bool> AnyAsync<T>(IQueryable<T> query, CancellationToken cancellationToken = default);
}
