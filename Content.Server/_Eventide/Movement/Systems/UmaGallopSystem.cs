using Content.Shared.Movement.Events;
using Content.Shared.Movement.Systems;
using Content.Shared.Nutrition.Components;

namespace Content.Server.Movement.Systems;

/// <summary>
/// Handles Uma's short, cooldown-limited sprint burst.
/// </summary>
public sealed partial class UmaGallopSystem : EntitySystem
{
    [Dependency] private MovementModStatusSystem _movement = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<UmaDietComponent, UmaGallopActionEvent>(OnGallop);
    }

    private void OnGallop(Entity<UmaDietComponent> entity, ref UmaGallopActionEvent args)
    {
        _movement.TryAddMovementSpeedModDuration(
            entity.Owner,
            "StatusEffectUmaGallop",
            TimeSpan.FromSeconds(4),
            1f,
            1.3f);
    }
}
