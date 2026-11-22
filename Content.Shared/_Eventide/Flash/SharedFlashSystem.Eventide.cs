using System;
using Content.Shared.Flash.Components;

namespace Content.Shared.Flash;

public abstract partial class SharedFlashSystem
{
    private void ApplyFlashSensitivity(EntityUid target, ref TimeSpan flashDuration, ref TimeSpan? stunDuration)
    {
        if (!TryComp<FlashSensitivityComponent>(target, out var sensitivity))
            return;

        var multiplier = Math.Max(1f, sensitivity.DurationMultiplier);
        flashDuration = TimeSpan.FromTicks((long) (flashDuration.Ticks * multiplier));
        if (stunDuration.HasValue)
            stunDuration = TimeSpan.FromTicks((long) (stunDuration.Value.Ticks * multiplier));
    }
}
