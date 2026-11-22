using System.Linq;
using Content.Shared.Chemistry;
using Content.Shared.Chemistry.Components;
using Content.Shared.Damage;
using Content.Shared.Damage.Prototypes;
using Content.Shared.Damage.Systems;
using Content.Shared.FixedPoint;
using Content.Shared.Nutrition;
using Content.Shared.Nutrition.Components;
using Content.Shared.Nutrition.EntitySystems;
using Content.Shared.Tag;
using Robust.Shared.Prototypes;

namespace Content.Server.Nutrition.EntitySystems;

/// <summary>
/// Applies small nutrition modifiers when Uma consumes foods that suit or disagree with their
/// plant-forward digestive system. Heavy food is less filling rather than directly poisonous.
/// </summary>
public sealed partial class UmaDietSystem : EntitySystem
{
    [Dependency] private SatiationSystem _satiation = default!;
    [Dependency] private DamageableSystem _damageable = default!;
    [Dependency] private TagSystem _tags = default!;

    private static readonly ProtoId<TagPrototype> MeatTag = "Meat";
    private static readonly ProtoId<TagPrototype> CheeseTag = "Cheese";
    private static readonly ProtoId<TagPrototype> CakeTag = "Cake";
    private static readonly ProtoId<TagPrototype> BreadTag = "Bread";
    private static readonly ProtoId<DamageTypePrototype> BluntDamageType = "Blunt";

    private static readonly HashSet<string> PlantFlavors = new(StringComparer.OrdinalIgnoreCase)
    {
        "apple", "carrot", "hay", "grass", "oat", "oats", "wheat", "grain",
        "bread", "dough", "peas", "beans", "bean", "cabbage", "potato",
        "potatoes", "tomato", "eggplant", "avocado", "corn", "rice",
        "vegetable", "fruit", "pumpkin", "lettuce"
    };

    private static readonly HashSet<string> HeavyFlavors = new(StringComparer.OrdinalIgnoreCase)
    {
        "meat", "bacon", "sausage", "fat", "grease", "cheese", "fried", "chocolate"
    };

    private static readonly HashSet<string> HeavyReagents = new(StringComparer.OrdinalIgnoreCase)
    {
        "AnimalProteins", "UncookedAnimalProteins", "Fat", "Butter", "Milk",
        "CocoaPowder", "Theobromine", "Ethanol"
    };

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<UmaDietComponent, IngestingEvent>(OnFoodIngesting);
    }

    private void OnFoodIngesting(Entity<UmaDietComponent> eater, ref IngestingEvent args)
    {
        if (!TryComp<SatiationComponent>(eater.Owner, out var satiation))
            return;

        var plantFood = IsPlantFood(args.Food);
        var heavyFood = IsHeavyFood(args.Food, args.Split);

        // Heavy ingredients take precedence in mixed dishes (for example, meat pies).
        if (heavyFood)
        {
            _satiation.ModifyValue((eater.Owner, satiation), SatiationSystem.Hunger, -1.5f);
            return;
        }

        if (!plantFood)
            return;

        _satiation.ModifyValue((eater.Owner, satiation), SatiationSystem.Hunger, 2f);

        // A small recovery bonus rewards a suitable plant-based meal without making food a substitute for medicine.
        var recovery = new DamageSpecifier(
            ProtoMan.Index(BluntDamageType),
            FixedPoint2.New(-0.25));
        _damageable.TryChangeDamage(eater.Owner, recovery, origin: args.Food);
    }

    private bool IsPlantFood(EntityUid food)
    {
        if (_tags.HasTag(food, BreadTag))
            return true;

        return TryComp<FlavorProfileComponent>(food, out var flavor) &&
               flavor.Flavors.Any(entry => PlantFlavors.Contains(entry.Id));
    }

    private bool IsHeavyFood(EntityUid food, Solution split)
    {
        if (_tags.HasTag(food, MeatTag) ||
            _tags.HasTag(food, CheeseTag) ||
            _tags.HasTag(food, CakeTag))
            return true;

        if (TryComp<FlavorProfileComponent>(food, out var flavor) &&
            flavor.Flavors.Any(entry => HeavyFlavors.Contains(entry.Id)))
            return true;

        return split.Contents.Any(entry => HeavyReagents.Contains(entry.Reagent.Prototype.Id));
    }
}
