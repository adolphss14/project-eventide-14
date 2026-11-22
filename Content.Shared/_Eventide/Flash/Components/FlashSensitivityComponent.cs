using Robust.Shared.GameStates;

namespace Content.Shared.Flash.Components;

/// <summary>
/// Modifies how long flash effects last on the owning entity.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class FlashSensitivityComponent : Component
{
    /// <summary>
    /// Multiplier applied to flash and flash-induced stun durations.
    /// </summary>
    [DataField, AutoNetworkedField]
    public float DurationMultiplier = 1.25f;
}
