using Hadm.Application.Abstractions;
using Hadm.Domain.Entities;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace Hadm.Infrastructure.Persistence;

public sealed class HadmDbContext(DbContextOptions<HadmDbContext> options)
    : IdentityDbContext<HadmUser>(options), IApplicationDbContext
{
    public DbSet<MealLog> MealLogs => Set<MealLog>();
    public DbSet<Food> Foods => Set<Food>();
    public DbSet<MealLogFood> MealLogFoods => Set<MealLogFood>();
    public DbSet<SymptomLog> SymptomLogs => Set<SymptomLog>();
    public DbSet<SymptomType> SymptomTypes => Set<SymptomType>();
    public DbSet<BowelLog> BowelLogs => Set<BowelLog>();
    public DbSet<DailyLog> DailyLogs => Set<DailyLog>();

    IQueryable<MealLog> IApplicationDbContext.MealLogs => MealLogs;
    IQueryable<Food> IApplicationDbContext.Foods => Foods;
    IQueryable<MealLogFood> IApplicationDbContext.MealLogFoods => MealLogFoods;
    IQueryable<SymptomLog> IApplicationDbContext.SymptomLogs => SymptomLogs;
    IQueryable<SymptomType> IApplicationDbContext.SymptomTypes => SymptomTypes;
    IQueryable<BowelLog> IApplicationDbContext.BowelLogs => BowelLogs;
    IQueryable<DailyLog> IApplicationDbContext.DailyLogs => DailyLogs;

    protected override void OnModelCreating(ModelBuilder model)
    {
        base.OnModelCreating(model);
        model.Entity<MealLog>().HasIndex(x => new { x.UserId, x.LoggedAt });
        model.Entity<SymptomLog>().HasIndex(x => new { x.UserId, x.LoggedAt });
        model.Entity<BowelLog>().HasIndex(x => new { x.UserId, x.LoggedAt });
        model.Entity<DailyLog>().HasIndex(x => new { x.UserId, x.Date }).IsUnique();
        model.Entity<Food>().HasIndex(x => x.Key).IsUnique();
        model.Entity<SymptomType>().HasIndex(x => x.Key).IsUnique();
        model.Entity<MealLogFood>().HasKey(x => new { x.MealLogId, x.FoodId });
        model.Entity<MealLogFood>().HasOne(x => x.MealLog).WithMany(x => x.Foods)
            .HasForeignKey(x => x.MealLogId).OnDelete(DeleteBehavior.Cascade);
        model.Entity<MealLogFood>().HasOne(x => x.Food).WithMany().HasForeignKey(x => x.FoodId)
            .OnDelete(DeleteBehavior.Restrict);
        model.Entity<SymptomLog>().HasOne(x => x.SymptomType).WithMany().HasForeignKey(x => x.SymptomTypeId)
            .OnDelete(DeleteBehavior.Restrict);
        model.Entity<Food>().Property(x => x.Key).HasMaxLength(100);
        model.Entity<Food>().Property(x => x.NameEn).HasMaxLength(100);
        model.Entity<Food>().Property(x => x.NameAr).HasMaxLength(100);
        model.Entity<SymptomType>().Property(x => x.Key).HasMaxLength(100);
    }

    void IApplicationDbContext.Add(object entity) => Add(entity);
    void IApplicationDbContext.Remove(object entity) => Remove(entity);
    public async Task<List<T>> ListAsync<T>(IQueryable<T> query, CancellationToken cancellationToken = default) =>
        await EntityFrameworkQueryableExtensions.ToListAsync(query, cancellationToken);
    public async Task<T?> FirstOrDefaultAsync<T>(IQueryable<T> query, CancellationToken cancellationToken = default) where T : class =>
        await EntityFrameworkQueryableExtensions.FirstOrDefaultAsync(query, cancellationToken);
    public async Task<bool> AnyAsync<T>(IQueryable<T> query, CancellationToken cancellationToken = default) =>
        await EntityFrameworkQueryableExtensions.AnyAsync(query, cancellationToken);
}
