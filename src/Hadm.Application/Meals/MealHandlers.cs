using Hadm.Application.Contracts;

namespace Hadm.Application.Meals;

public sealed record GetMealsQuery(DateTime? From, DateTime? To) : IRequest<IReadOnlyList<MealDto>>;

public sealed class GetMealsHandler(IApplicationDbContext db, ICurrentUserService currentUser, IDateTimeProvider clock)
    : IRequestHandler<GetMealsQuery, IReadOnlyList<MealDto>>
{
    public async Task<IReadOnlyList<MealDto>> Handle(GetMealsQuery request, CancellationToken cancellationToken)
    {
        var userId = TextRules.RequireUser(currentUser);
        var to = request.To is null ? clock.UtcNow.AddDays(1) : Time.AsUtc(request.To.Value);
        var from = request.From is null ? to.AddDays(-7) : Time.AsUtc(request.From.Value);
        var meals = await db.ListAsync(
            db.MealLogs.Where(m => m.UserId == userId && m.LoggedAt >= from && m.LoggedAt < to),
            cancellationToken);
        return await MealMapper.MapAsync(db, meals, cancellationToken);
    }
}

public sealed record CreateMealCommand(DateTime LoggedAt, MealType MealType, string? Note, IReadOnlyList<Guid> FoodIds)
    : IRequest<MealDto>;

public sealed class CreateMealValidator : AbstractValidator<CreateMealCommand>
{
    public CreateMealValidator(ITextLocalizer text, IDateTimeProvider clock)
    {
        RuleFor(x => x.LoggedAt).ValidLogTime(clock, text);
        RuleFor(x => x.MealType).IsInEnum().WithMessage(text.Get("validation.meal_type_invalid"));
        RuleFor(x => x.Note).NoteLength(text);
        RuleFor(x => x.FoodIds).NotNull().Must(ids => ids.Count is > 0 and <= FieldLimits.MaxFoodsPerMeal)
            .WithMessage(text.Get("validation.foods_required"));
    }
}

public sealed class CreateMealHandler(IApplicationDbContext db, ICurrentUserService currentUser, ITextLocalizer text)
    : IRequestHandler<CreateMealCommand, MealDto>
{
    public async Task<MealDto> Handle(CreateMealCommand request, CancellationToken cancellationToken)
    {
        var userId = TextRules.RequireUser(currentUser);
        var foods = await FoodGuard.RequireAsync(db, userId, request.FoodIds, text, cancellationToken);
        var meal = new MealLog
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            LoggedAt = Time.AsUtc(request.LoggedAt),
            MealType = request.MealType,
            Note = TextRules.CleanNote(request.Note)
        };
        db.Add(meal);
        foreach (var food in foods)
        {
            db.Add(new MealLogFood { MealLogId = meal.Id, FoodId = food.Id });
        }

        await db.SaveChangesAsync(cancellationToken);
        return new MealDto(meal.Id, meal.LoggedAt, meal.MealType, meal.Note, foods.Select(FoodMapper.ToDto).ToList());
    }
}

public sealed record UpdateMealCommand(Guid Id, DateTime LoggedAt, MealType MealType, string? Note, IReadOnlyList<Guid> FoodIds)
    : IRequest<MealDto>;

public sealed class UpdateMealValidator : AbstractValidator<UpdateMealCommand>
{
    public UpdateMealValidator(ITextLocalizer text, IDateTimeProvider clock)
    {
        RuleFor(x => x.LoggedAt).ValidLogTime(clock, text);
        RuleFor(x => x.MealType).IsInEnum().WithMessage(text.Get("validation.meal_type_invalid"));
        RuleFor(x => x.Note).NoteLength(text);
        RuleFor(x => x.FoodIds).NotNull().Must(ids => ids.Count is > 0 and <= FieldLimits.MaxFoodsPerMeal)
            .WithMessage(text.Get("validation.foods_required"));
    }
}

public sealed class UpdateMealHandler(IApplicationDbContext db, ICurrentUserService currentUser, ITextLocalizer text)
    : IRequestHandler<UpdateMealCommand, MealDto>
{
    public async Task<MealDto> Handle(UpdateMealCommand request, CancellationToken cancellationToken)
    {
        var userId = TextRules.RequireUser(currentUser);
        var meal = await db.FirstOrDefaultAsync(
            db.MealLogs.Where(m => m.Id == request.Id && m.UserId == userId),
            cancellationToken) ?? throw new NotFoundException(text.Get("error.not_found"));
        var foods = await FoodGuard.RequireAsync(db, userId, request.FoodIds, text, cancellationToken);
        meal.LoggedAt = Time.AsUtc(request.LoggedAt);
        meal.MealType = request.MealType;
        meal.Note = TextRules.CleanNote(request.Note);
        var existing = await db.ListAsync(db.MealLogFoods.Where(x => x.MealLogId == meal.Id), cancellationToken);
        foreach (var link in existing)
        {
            db.Remove(link);
        }

        foreach (var food in foods)
        {
            db.Add(new MealLogFood { MealLogId = meal.Id, FoodId = food.Id });
        }

        await db.SaveChangesAsync(cancellationToken);
        return new MealDto(meal.Id, meal.LoggedAt, meal.MealType, meal.Note, foods.Select(FoodMapper.ToDto).ToList());
    }
}

public sealed record DeleteMealCommand(Guid Id) : IRequest;

public sealed class DeleteMealHandler(IApplicationDbContext db, ICurrentUserService currentUser, ITextLocalizer text)
    : IRequestHandler<DeleteMealCommand>
{
    public async Task Handle(DeleteMealCommand request, CancellationToken cancellationToken)
    {
        var userId = TextRules.RequireUser(currentUser);
        var meal = await db.FirstOrDefaultAsync(
            db.MealLogs.Where(m => m.Id == request.Id && m.UserId == userId),
            cancellationToken) ?? throw new NotFoundException(text.Get("error.not_found"));
        var links = await db.ListAsync(db.MealLogFoods.Where(x => x.MealLogId == meal.Id), cancellationToken);
        foreach (var link in links)
        {
            db.Remove(link);
        }

        db.Remove(meal);
        await db.SaveChangesAsync(cancellationToken);
    }
}

internal static class FoodGuard
{
    public static async Task<List<Food>> RequireAsync(
        IApplicationDbContext db,
        string userId,
        IReadOnlyList<Guid> foodIds,
        ITextLocalizer text,
        CancellationToken cancellationToken)
    {
        var ids = foodIds.Distinct().ToList();
        var foods = await db.ListAsync(
            db.Foods.Where(f => ids.Contains(f.Id) && (f.IsBuiltIn || f.UserId == userId)),
            cancellationToken);
        if (foods.Count != ids.Count)
        {
            throw new NotFoundException(text.Get("error.food_not_found"));
        }

        return foods;
    }
}

internal static class FoodMapper
{
    public static FoodDto ToDto(Food food) => new(food.Id, food.Key, food.NameEn, food.NameAr, food.IsBuiltIn);
}

internal static class MealMapper
{
    public static async Task<IReadOnlyList<MealDto>> MapAsync(
        IApplicationDbContext db,
        List<MealLog> meals,
        CancellationToken cancellationToken)
    {
        var ids = meals.Select(m => m.Id).ToList();
        var links = ids.Count == 0
            ? []
            : await db.ListAsync(db.MealLogFoods.Where(x => ids.Contains(x.MealLogId)), cancellationToken);
        var foodIds = links.Select(l => l.FoodId).Distinct().ToList();
        var foods = foodIds.Count == 0
            ? []
            : await db.ListAsync(db.Foods.Where(f => foodIds.Contains(f.Id)), cancellationToken);
        var foodMap = foods.ToDictionary(f => f.Id);

        return meals
            .OrderByDescending(m => m.LoggedAt)
            .Select(m => new MealDto(
                m.Id,
                m.LoggedAt,
                m.MealType,
                m.Note,
                links.Where(l => l.MealLogId == m.Id)
                    .Select(l => foodMap.TryGetValue(l.FoodId, out var food) ? FoodMapper.ToDto(food) : null)
                    .Where(f => f is not null)
                    .Cast<FoodDto>()
                    .ToList()))
            .ToList();
    }
}
