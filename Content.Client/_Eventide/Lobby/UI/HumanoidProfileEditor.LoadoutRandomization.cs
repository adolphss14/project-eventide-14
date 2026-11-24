using System;
using System.Collections.Generic;
using System.Linq;
using Content.Shared.Clothing;
using Content.Shared.Preferences;
using Content.Shared.Preferences.Loadouts;
using Content.Shared.Roles;
using Robust.Shared;
using Robust.Shared.Random;

namespace Content.Client.Lobby.UI;

public sealed partial class HumanoidProfileEditor
{
    private enum UniformRandomizationStyle
    {
        Any,
        Jumpsuit,
        Jumpskirt
    }

    /// <summary>
    /// Randomizes wearable clothing for preferred jobs and selected antagonist roles.
    /// Uniform style and the style of all other clothing slots are deliberately independent.
    /// Disabling style filters allows all available clothing variants; it does not disable randomization.
    /// Non-clothing equipment groups are never changed by this option.
    /// </summary>
    private void RandomizeRoleLoadouts(
        bool applyStyleFilters,
        LoadoutClothingStyle clothingStyle,
        UniformRandomizationStyle uniformStyle,
        IRobustRandom random)
    {
        if (Profile is null ||
            _playerManager.LocalSession is not { } session ||
            IoCManager.Instance is not { } collection)
            return;

        if (!applyStyleFilters)
        {
            clothingStyle = LoadoutClothingStyle.Any;
            uniformStyle = UniformRandomizationStyle.Any;
        }

        var roleIds = new HashSet<string>(StringComparer.Ordinal);

        foreach (var (jobId, priority) in Profile.JobPriorities)
        {
            if (priority == JobPriority.Never || !_prototypeManager.TryIndex(jobId, out var job))
                continue;

            roleIds.Add(LoadoutSystem.GetJobPrototype(job.ID));
        }

        foreach (var antag in Profile.AntagPreferences)
            roleIds.Add(antag.Id);

        foreach (var roleId in roleIds)
        {
            if (!_prototypeManager.TryIndex<RoleLoadoutPrototype>(roleId, out var roleProto) ||
                roleProto.Groups.Count == 0)
                continue;

            var loadout = Profile.Loadouts.TryGetValue(roleId, out var existing)
                ? existing.Clone()
                : new RoleLoadout(roleProto.ID);

            loadout.SetDefault(Profile, session, _prototypeManager);

            foreach (var groupId in roleProto.Groups)
            {
                if (!_prototypeManager.TryIndex(groupId, out var groupProto) ||
                    groupProto.Hidden ||
                    !IsClothingLoadoutGroup(groupId.Id))
                    continue;

                // Validate prototypes and role restrictions before applying style filters.
                // This prevents stale or unavailable loadout IDs from reaching Index() below.
                var candidates = groupProto.Loadouts
                    .Where(candidate => loadout.IsValid(Profile, session, candidate, collection, out _))
                    .ToList();
                if (candidates.Count == 0)
                    continue;

                if (IsUniformLoadoutGroup(groupId.Id) && uniformStyle != UniformRandomizationStyle.Any)
                {
                    candidates = uniformStyle switch
                    {
                        UniformRandomizationStyle.Jumpsuit => candidates
                            .Where(candidate => !IsSkirtOrDress(candidate.Id))
                            .ToList(),
                        UniformRandomizationStyle.Jumpskirt => candidates
                            .Where(candidate => IsSkirtOrDress(candidate.Id))
                            .ToList(),
                        _ => candidates
                    };

                    // Some jobs have only one uniform style. Do not silently choose the opposite style.
                    if (candidates.Count == 0)
                        continue;
                }
                else if (clothingStyle != LoadoutClothingStyle.Any)
                {
                    var explicitlyStyledCandidates = candidates
                        .Where(candidate => _prototypeManager.Index(candidate).ClothingStyle == clothingStyle)
                        .ToList();

                    if (explicitlyStyledCandidates.Count > 0)
                    {
                        candidates = explicitlyStyledCandidates;
                    }
                    else
                    {
                        // Neutral clothing fills slots that do not yet have an explicitly styled variant.
                        var neutralCandidates = candidates
                            .Where(candidate => _prototypeManager.Index(candidate).ClothingStyle == LoadoutClothingStyle.Any)
                            .ToList();

                        if (neutralCandidates.Count > 0)
                        {
                            candidates = neutralCandidates;
                        }
                        else
                        {
                            // No compatible variant exists for this slot. Keep the current selection
                            // rather than silently violating the requested style filter.
                            continue;
                        }
                    }
                }

                if (!loadout.SelectedLoadouts.TryGetValue(groupId, out var selectedLoadouts))
                {
                    selectedLoadouts = new List<Loadout>();
                    loadout.SelectedLoadouts[groupId] = selectedLoadouts;
                }

                // Keep optional groups empty unless the player already selected something there.
                // Required and default-selected groups keep their configured minimum count.
                var desiredCount = Math.Max(
                    selectedLoadouts.Count,
                    Math.Max(groupProto.MinLimit, groupProto.DefaultSelected));

                if (desiredCount <= 0)
                    continue;

                if (groupProto.MaxLimit > 0)
                    desiredCount = Math.Min(desiredCount, groupProto.MaxLimit);

                desiredCount = Math.Min(desiredCount, candidates.Count);
                if (desiredCount <= 0)
                    continue;

                // Select distinct entries and preserve multi-item groups rather than forcing every slot to one item.
                var selectedCandidates = candidates
                    .OrderBy(_ => random.NextFloat(0f, 1f))
                    .Take(desiredCount)
                    .ToArray();

                selectedLoadouts.Clear();
                foreach (var candidate in selectedCandidates)
                    loadout.AddLoadout(groupId, candidate, _prototypeManager);
            }

            loadout.EnsureValid(Profile, session, collection);
            Profile = Profile.WithLoadout(loadout);
        }
    }

    private static bool IsUniformLoadoutGroup(string groupId)
    {
        return groupId.Contains("Jumpsuit", StringComparison.OrdinalIgnoreCase) ||
               groupId.Contains("Uniform", StringComparison.OrdinalIgnoreCase) ||
               groupId.Contains("Skirt", StringComparison.OrdinalIgnoreCase);
    }

    private static bool IsSkirtOrDress(string loadoutId)
    {
        return loadoutId.Contains("skirt", StringComparison.OrdinalIgnoreCase) ||
               loadoutId.Contains("dress", StringComparison.OrdinalIgnoreCase);
    }

    private static bool IsClothingLoadoutGroup(string groupId)
    {
        string[] clothingSlots =
        [
            "Jumpsuit", "Uniform", "OuterClothing", "Head", "Neck", "Shoes", "Back",
            "Gloves", "Mask", "Glasses", "Ears", "Belt", "Wrist"
        ];

        return clothingSlots.Any(slot =>
            groupId.Contains(slot, StringComparison.OrdinalIgnoreCase));
    }
}
