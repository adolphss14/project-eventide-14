using Robust.Shared.GameStates;

namespace Content.Shared.DeviceNetwork.Components;

/// <summary>
/// Compatibility component for saved maps created before WirelessNetwork was renamed.
/// WirelessNetworkSystem migrates it to WirelessNetwork during map initialization.
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class WirelessNetworkConnectionComponent : Component
{
    [DataField]
    public float? Range;
}
