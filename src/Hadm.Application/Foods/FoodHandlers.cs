using Hadm.Application.Contracts;
using Hadm.Application.Meals;

namespace Hadm.Application.Foods;

public sealed record GetFoodsQuery : IRequest<IReadOnlyList<FoodDto>>;

public sealed class GetFoodsHandler(IApplicationDbContext db, ICurrentUserService currentUser)
    : IRequestHandler<GetFoodsQuery, IReadOnlyList<FoodDto>>
{
    public async Task<IReadOnlyList<FoodDto>> Handle(GetFoodsQuery request, CancellationToken cancellationToken)
    {
        var userId = TextRules.RequireUser(currentUser);
        var foods = await db.ListAsync(
            db.Foods.Where(f => f.IsBuiltIn || f.UserId == userId).OrderBy(f => f.NameEn),
            cancellationToken);
        return foods.Select(FoodMapper.ToDto).ToList();
    }
}

public sealed record CreateFoodCommand(string Name) : IRequest<FoodDto>;

public sealed class CreateFoodValidator : AbstractValidator<CreateFoodCommand>
{
    public CreateFoodValidator(ITextLocalizer text)
    {
        RuleFor(x => x.Name).NotEmpty().WithMessage(text.Get("validation.food_name_required"))
            .MaximumLength(FieldLimits.FoodNameMaxLength).WithMessage(text.Get("validation.food_name_length"));
    }
}

public sealed class CreateFoodHandler(IApplicationDbContext db, ICurrentUserService currentUser, ITextLocalizer text)
    : IRequestHandler<CreateFoodCommand, FoodDto>
{
    public async Task<FoodDto> Handle(CreateFoodCommand request, CancellationToken cancellationToken)
    {
        var userId = TextRules.RequireUser(currentUser);
        var name = string.Join(' ', request.Name.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries));
        var exists = await db.AnyAsync(
            db.Foods.Where(f => (f.IsBuiltIn || f.UserId == userId) && (f.NameEn == name || f.NameAr == name)),
            cancellationToken);
        if (exists)
        {
            throw new AppValidationException(new Dictionary<string, string[]>
            {
                ["Name"] = [text.Get("error.duplicate_food")]
            });
        }

        var food = new Food
        {
            Id = Guid.NewGuid(),
            Key = $"custom-{Guid.NewGuid():N}",
            NameEn = name,
            NameAr = name,
            IsBuiltIn = false,
            UserId = userId
        };
        db.Add(food);
        await db.SaveChangesAsync(cancellationToken);
        return FoodMapper.ToDto(food);
    }
}
