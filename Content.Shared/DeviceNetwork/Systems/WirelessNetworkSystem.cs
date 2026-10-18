using Content.Shared.DeviceNetwork.Components;
using Content.Shared.DeviceNetwork.Events;

namespace Content.Shared.DeviceNetwork.Systems;

public sealed partial class WirelessNetworkSystem : EntitySystem
{
    [Dependency] private SharedTransformSystem _transformSystem = default!;

    [Dependency] private EntityQuery<WirelessNetworkComponent> _wirelessQuery;

    [SubscribeLocalEvent]
    private void OnLegacyWirelessNetworkMapInit(Entity<WirelessNetworkConnectionComponent> ent, ref MapInitEvent args)
    {
        var wireless = EnsureComp<WirelessNetworkComponent>(ent.Owner);
        wireless.Range = ent.Comp.Range;

        // Convert older saved maps to the current component name and schema at load time.
        RemCompDeferred<WirelessNetworkConnectionComponent>(ent.Owner);
    }

    [SubscribeLocalEvent]
    private void OnBeforePacketSent(Entity<WirelessNetworkComponent> ent, ref BeforePacketSentEvent args)
    {
        var ownPosition = args.SenderPosition;
        var xform = Transform(ent);

        // not a wireless to wireless connection, just let it happen
        if (!_wirelessQuery.TryComp(args.Sender, out var sendingComponent))
            return;

        if (xform.MapID != args.SenderTransform.MapID
            || sendingComponent.Range != null
            && (ownPosition - _transformSystem.GetWorldPosition(xform)).Length() > sendingComponent.Range)
        {
            args.Cancelled = true;
        }
    }
}
