using Content.Shared.Stunnable;
using Robust.Shared.Random;

namespace Content.Server.Stunnable;

/// <summary>
/// Gives Uma a modest chance to resist knockdown without granting knockdown immunity.
/// </summary>
public sealed partial class UmaStabilitySystem : EntitySystem
{
    [Dependency] private IRobustRandom _random = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<UmaStabilityComponent, KnockDownAttemptEvent>(OnKnockdownAttempt);
    }

    private void OnKnockdownAttempt(Entity<UmaStabilityComponent> entity, ref KnockDownAttemptEvent args)
    {
        if (_random.NextFloat(0f, 1f) < Math.Clamp(entity.Comp.KnockdownResistanceChance, 0f, 1f))
            args.Cancelled = true;
    }
}
