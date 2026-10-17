using System.Linq;
using Content.Shared.Body;
using Content.Shared.Clothing.Components;
using Content.Shared.Humanoid;
using Content.Shared.Preferences;
using Content.Shared.Preferences.Loadouts;
using Content.Shared.Roles;
using Content.Shared.Station.Systems;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;

namespace Content.Shared.Clothing;

/// <summary>
/// Assigns a loadout to an entity based on the RoleLoadout prototype
/// </summary>
public sealed partial class LoadoutSystem : EntitySystem
{
    // Shared so we can predict it for placement manager.

    [Dependency] private ActorSystem _actors = default!;
    [Dependency] private StationSpawningSystem _station = default!;
    [Dependency] private IRobustRandom _random = default!;

    public override void Initialize()
    {
        base.Initialize();

        // Wait until the character has all their organs before we give them their loadout
        SubscribeLocalEvent<LoadoutComponent, MapInitEvent>(OnMapInit, after: [typeof(InitialBodySystem)]);
    }

    public static string GetJobPrototype(string? loadout)
    {
        if (string.IsNullOrEmpty(loadout))
            return string.Empty;

        return "Job" + loadout;
    }

    public EntProtoId? GetFirstOrNull(LoadoutPrototype loadout)
    {
        EntProtoId? proto = null;

        if (ProtoMan.Resolve(loadout.StartingGear, out var gear))
        {
            proto = GetFirstOrNull(gear);
        }

        proto ??= GetFirstOrNull((IEquipmentLoadout)loadout);
        return proto;
    }

    /// <summary>
    /// Tries to get the first entity prototype for operations such as sprite drawing.
    /// </summary>
    public EntProtoId? GetFirstOrNull(IEquipmentLoadout? gear)
    {
        if (gear == null)
            return null;

        var count = gear.Equipment.Count + gear.Inhand.Count + gear.Storage.Values.Sum(x => x.Count);

        if (count == 1)
        {
            if (gear.Equipment.Count == 1 && ProtoMan.Resolve(gear.Equipment.Values.First(), out var proto))
            {
                return proto.ID;
            }

            if (gear.Inhand.Count == 1 && ProtoMan.Resolve(gear.Inhand[0], out proto))
            {
                return proto.ID;
            }

            // Storage moment
            foreach (var ents in gear.Storage.Values)
            {
                foreach (var ent in ents)
                {
                    return ent;
                }
            }
        }

        return null;
    }

    public string GetName(LoadoutPrototype loadout)
    {
        if (loadout.DummyEntity is not null && ProtoMan.Resolve(loadout.DummyEntity, out var proto))
            return proto.Name;

        if (ProtoMan.Resolve(loadout.StartingGear, out var gear))
        {
            return GetName(gear);
        }

        return GetName((IEquipmentLoadout) loadout);
    }

    /// <summary>
    /// Tries to get the name of a loadout.
    /// </summary>
    public string GetName(IEquipmentLoadout? gear)
    {
        if (gear == null)
            return string.Empty;

        var count = gear.Equipment.Count + gear.Storage.Values.Sum(o => o.Count) + gear.Inhand.Count;

        if (count == 1)
        {
            if (gear.Equipment.Count == 1 && ProtoMan.TryIndex<EntityPrototype>(gear.Equipment.Values.First(), out var proto))
            {
                return proto.Name;
            }

            if (gear.Inhand.Count == 1 && ProtoMan.TryIndex<EntityPrototype>(gear.Inhand[0], out proto))
            {
                return proto.Name;
            }

            foreach (var values in gear.Storage.Values)
            {
                if (values.Count != 1)
                    continue;

                if (ProtoMan.TryIndex<EntityPrototype>(values[0], out proto))
                {
                    return proto.Name;
                }

                break;
            }
        }

        return Loc.GetString($"unknown");
    }

    private void OnMapInit(EntityUid uid, LoadoutComponent component, MapInitEvent args)
    {
        Equip(uid, component.StartingGear, component.RoleLoadout);
    }

    // Genesis-Start: Apply saved role loadouts while keeping starting-gear contents in the selected backpack.
    public void Equip(EntityUid uid, List<ProtoId<StartingGearPrototype>>? startingGear,
        List<ProtoId<RoleLoadoutPrototype>>? loadoutGroups,
        RoleLoadout? selectedLoadout = null,
        HumanoidCharacterProfile? selectedProfile = null)
    {
        // Resolve the base gear once, but don't equip it until the role loadout is known.
        // That lets the selected role loadout win for overlapping inventory slots.
        StartingGearPrototype? selectedStartingGear = null;
        if (startingGear is { Count: > 0 })
            selectedStartingGear = ProtoMan.Index<StartingGearPrototype>(_random.Pick(startingGear));

        var profile = selectedProfile ?? GetProfile(uid);

        if (loadoutGroups == null || loadoutGroups.Count == 0)
        {
            if (selectedStartingGear != null)
                _station.EquipStartingGear(uid, selectedStartingGear, raiseEvent: false);

            GearEquipped(uid);
            return;
        }

        // Antagonist spawners don't pass the selected profile loadout explicitly.
        // Pick the saved selection matching one of the loadouts this specifier permits.
        if (selectedLoadout == null && profile != null)
        {
            foreach (var roleLoadoutId in loadoutGroups)
            {
                if (!profile.Loadouts.TryGetValue(roleLoadoutId.Id, out var savedLoadout))
                    continue;

                selectedLoadout = savedLoadout;
                break;
            }
        }

        // Only use a saved selection if this antag explicitly allows that role loadout.
        var useSelectedLoadout = selectedLoadout != null && loadoutGroups.Contains(selectedLoadout.Role);
        var id = useSelectedLoadout ? selectedLoadout!.Role : _random.Pick(loadoutGroups);
        var proto = ProtoMan.Index(id);
        var loadout = useSelectedLoadout ? selectedLoadout!.Clone() : new RoleLoadout(id);

        // Preserve saved choices and fill any newly added groups; non-player/default loadouts start clean.
        loadout.SetDefault(profile, _actors.GetSession(uid), ProtoMan, force: !useSelectedLoadout);

        // Apply the player's selected role loadout first so it has priority over the
        // antagonist's fallback starting gear (for example, belt and ID card).
        _station.EquipRoleLoadout(uid, loadout, proto);

        // Fill only slots not claimed by the selected role loadout. StationSpawningSystem
        // also checks actual slot occupancy, so fallback gear cannot replace a selected item.
        if (selectedStartingGear != null)
            _station.EquipStartingGear(uid, selectedStartingGear, raiseEvent: false, equipStorage: false, selectedRoleLoadout: loadout);

        // The selected backpack now exists, so starting-gear storage can be filled safely.
        if (selectedStartingGear != null)
            _station.EquipStartingGearStorage(uid, selectedStartingGear);

        GearEquipped(uid);
    }
    // Genesis-End

    public void GearEquipped(EntityUid uid)
    {
        var ev = new StartingGearEquippedEvent(uid);
        RaiseLocalEvent(uid, ref ev);
    }

    public HumanoidCharacterProfile GetProfile(EntityUid? uid)
    {
        return TryComp<HumanoidProfileComponent>(uid, out var profile)
            ? HumanoidCharacterProfile.DefaultWithSpecies(profile.Species, profile.Sex)
            : HumanoidCharacterProfile.Random();
    }
}
