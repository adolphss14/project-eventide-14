using System.Linq;
using Content.Shared.Body;
using Content.Shared.Antag;
using Content.Shared.Clothing;
using Content.Shared.GameTicking;
using Content.Shared.Inventory;
using Content.Shared.Preferences;
using Content.Shared.Preferences.Loadouts;
using Content.Shared.Roles;
using Content.Shared.Station.Systems;
using Robust.Shared.Map;
using Robust.Shared.Prototypes;
using Robust.Shared.Utility;

namespace Content.Client.Lobby.UI.ProfileEditorControls;

public sealed partial class ProfilePreviewSpriteView
{
    /// <summary>
    /// A slim reload that only updates the entity itself and not any of the job entities, etc.
    /// </summary>
    private void ReloadHumanoidEntity(HumanoidCharacterProfile humanoid)
    {
        if (!EntMan.EntityExists(PreviewDummy) ||
            !EntMan.HasComponent<VisualBodyComponent>(PreviewDummy))
            return;

        EntMan.System<SharedVisualBodySystem>().ApplyProfileTo(PreviewDummy, humanoid);
    }

    /// <summary>
    /// Loads the profile onto a dummy entity.
    /// </summary>
    // Genesis-Edit: optionally preview the role loadout currently edited, including antagonist equipment.
    private void LoadHumanoidEntity(
        HumanoidCharacterProfile? humanoid,
        JobPrototype? job,
        bool jobClothes,
        RoleLoadout? roleLoadoutOverride = null,
        RoleLoadoutPrototype? roleLoadoutPrototypeOverride = null)
    {
        EntProtoId? previewEntity = null;
        if (humanoid != null && jobClothes && roleLoadoutPrototypeOverride == null)
        {
            job ??= GetPreferredJob(humanoid);

            previewEntity = job.JobPreviewEntity ?? (EntProtoId?)job?.JobEntity;
        }

        if (previewEntity != null)
        {
            // Special type like borg or AI, do not spawn a human just spawn the entity.
            PreviewDummy = EntMan.SpawnEntity(previewEntity, MapCoordinates.Nullspace);
        }
        else if (humanoid is not null)
        {
            var dummy = _prototypeManager.Index(humanoid.Species).DollPrototype;
            PreviewDummy = EntMan.SpawnEntity(dummy, MapCoordinates.Nullspace);
            EntMan.System<SharedVisualBodySystem>().ApplyProfileTo(PreviewDummy, humanoid);
        }
        else
        {
            PreviewDummy = EntMan.SpawnEntity(_prototypeManager.Index(HumanoidCharacterProfile.DefaultSpecies).DollPrototype, MapCoordinates.Nullspace);
        }

        if (humanoid != null && jobClothes)
        {
            // Show the regular job kit only in the profession preview.
            // An antagonist preview must start from an empty species dummy, otherwise
            // the preferred station job's clothes can occupy slots that antag gear needs.
            if (roleLoadoutPrototypeOverride == null)
            {
                DebugTools.Assert(job != null);

                GiveDummyJobClothes(humanoid, job!);

                if (_prototypeManager.HasIndex<RoleLoadoutPrototype>(LoadoutSystem.GetJobPrototype(job!.ID)))
                {
                    var loadout = humanoid.GetLoadoutOrDefault(LoadoutSystem.GetJobPrototype(job.ID), _playerManager.LocalSession, humanoid.Species, EntMan, _prototypeManager);
                    GiveDummyLoadout(loadout);
                }
            }

            // Apply the selected role loadout for this preview. For antagonist previews,
            // then apply its specifier's StartingGear as fallback, matching LoadoutSystem.Equip.
            if (roleLoadoutPrototypeOverride != null)
            {
                var selectedRoleLoadout = roleLoadoutOverride;
                if (selectedRoleLoadout == null &&
                    humanoid.Loadouts.TryGetValue(roleLoadoutPrototypeOverride.ID, out var savedLoadout))
                {
                    selectedRoleLoadout = savedLoadout;
                }

                if (selectedRoleLoadout == null)
                {
                    selectedRoleLoadout = new RoleLoadout(roleLoadoutPrototypeOverride.ID);
                    selectedRoleLoadout.SetDefault(humanoid, _playerManager.LocalSession, _prototypeManager);
                }

                var spawning = EntMan.System<StationSpawningSystem>();
                spawning.EquipRoleLoadout(PreviewDummy, selectedRoleLoadout, roleLoadoutPrototypeOverride);

                var antagSpecifier = _prototypeManager.EnumeratePrototypes<AntagSpecifierPrototype>()
                    .FirstOrDefault(specifier =>
                        specifier.PrefRoles.Any(role => role.Id == roleLoadoutPrototypeOverride.ID) &&
                        specifier.RoleLoadout?.Any(roleLoadout => roleLoadout.Id == roleLoadoutPrototypeOverride.ID) == true);

                if (antagSpecifier?.StartingGear is { } startingGearId &&
                    _prototypeManager.Resolve(startingGearId, out var startingGear))
                {
                    // Keep the selected loadout's items, fill only its unclaimed slots, and
                    // insert storage contents after the loadout's chosen backpack is equipped.
                    spawning.EquipStartingGear(
                        PreviewDummy,
                        startingGear,
                        raiseEvent: false,
                        equipStorage: false,
                        selectedRoleLoadout: selectedRoleLoadout);
                    spawning.EquipStartingGearStorage(PreviewDummy, startingGear);
                }
            }
        }
    }

    /// <summary>
    /// Gets the highest priority job for the profile.
    /// </summary>
    private JobPrototype GetPreferredJob(HumanoidCharacterProfile profile)
    {
        var highPriorityJob = profile.JobPriorities.FirstOrDefault(p => p.Value == JobPriority.High).Key;
        // ReSharper disable once NullCoalescingConditionIsAlwaysNotNullAccordingToAPIContract (what is resharper smoking?)
        return _prototypeManager.Index<JobPrototype>(highPriorityJob.Id ?? GameTicker.FallbackOverflowJob);
    }

    private void GiveDummyLoadout(RoleLoadout? roleLoadout)
    {
        if (roleLoadout == null)
            return;

        var spawnSys = EntMan.System<StationSpawningSystem>();

        foreach (var group in roleLoadout.SelectedLoadouts.Values)
        {
            foreach (var loadout in group)
            {
                if (!_prototypeManager.Resolve(loadout.Prototype, out var loadoutProto))
                    continue;

                spawnSys.EquipStartingGear(PreviewDummy, loadoutProto);
            }
        }
    }

    /// <summary>
    /// Applies the specified job's clothes to the dummy.
    /// </summary>
    private void GiveDummyJobClothes(HumanoidCharacterProfile profile, JobPrototype job)
    {
        var inventorySys = EntMan.System<InventorySystem>();
        if (!inventorySys.TryGetSlots(PreviewDummy, out var slots))
            return;

        // Apply loadout
        if (profile.Loadouts.TryGetValue(job.ID, out var jobLoadout))
        {
            foreach (var loadouts in jobLoadout.SelectedLoadouts.Values)
            {
                foreach (var loadout in loadouts)
                {
                    if (!_prototypeManager.Resolve(loadout.Prototype, out var loadoutProto))
                        continue;

                    // TODO: Need some way to apply starting gear to an entity and replace existing stuff coz holy fucking shit dude.
                    foreach (var slot in slots)
                    {
                        // Try startinggear first
                        if (_prototypeManager.Resolve(loadoutProto.StartingGear, out var loadoutGear))
                        {
                            var itemType = ((IEquipmentLoadout) loadoutGear).GetGear(slot.Name);

                            if (inventorySys.TryUnequip(PreviewDummy, slot.Name, out var unequippedItem, silent: true, force: true, reparent: false))
                            {
                                EntMan.DeleteEntity(unequippedItem.Value);
                            }

                            if (itemType != string.Empty)
                            {
                                var item = EntMan.SpawnEntity(itemType, MapCoordinates.Nullspace);
                                inventorySys.TryEquip(PreviewDummy, item, slot.Name, true, true);
                            }
                        }
                        else
                        {
                            var itemType = ((IEquipmentLoadout) loadoutProto).GetGear(slot.Name);

                            if (inventorySys.TryUnequip(PreviewDummy, slot.Name, out var unequippedItem, silent: true, force: true, reparent: false))
                            {
                                EntMan.DeleteEntity(unequippedItem.Value);
                            }

                            if (itemType != string.Empty)
                            {
                                var item = EntMan.SpawnEntity(itemType, MapCoordinates.Nullspace);
                                inventorySys.TryEquip(PreviewDummy, item, slot.Name, true, true);
                            }
                        }
                    }
                }
            }
        }

        if (!_prototypeManager.Resolve(job.StartingGear, out var gear))
            return;

        foreach (var slot in slots)
        {
            var itemType = ((IEquipmentLoadout) gear).GetGear(slot.Name);

            if (inventorySys.TryUnequip(PreviewDummy, slot.Name, out var unequippedItem, silent: true, force: true, reparent: false))
            {
                EntMan.DeleteEntity(unequippedItem.Value);
            }

            if (itemType != string.Empty)
            {
                var item = EntMan.SpawnEntity(itemType, MapCoordinates.Nullspace);
                inventorySys.TryEquip(PreviewDummy, item, slot.Name, true, true);
            }
        }
    }
}
