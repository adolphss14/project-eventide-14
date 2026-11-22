namespace Content.Shared.Stunnable;

/// <summary>
/// Gives Uma a small chance to keep their footing when a knockdown is attempted.
/// </summary>
[RegisterComponent]
public sealed partial class UmaStabilityComponent : Component
{
    /// <summary>
    /// Chance to resist a knockdown attempt, from 0 to 1.
    /// </summary>
    [DataField]
    public float KnockdownResistanceChance = 0.12f;
}
