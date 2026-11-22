using System;
using System.Collections.Generic;
using System.Linq;
using Content.Shared.Body;
using Content.Shared.Humanoid.Markings;
using Content.Shared.Humanoid.Prototypes;
using Robust.Shared.Maths;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;
using static Content.Shared.Preferences.HumanoidCharacterProfile;

namespace Content.Shared.Humanoid;

public sealed partial class HumanoidCharacterAppearance
{
    private static Dictionary<ProtoId<OrganCategoryPrototype>, Dictionary<HumanoidVisualLayers, List<Marking>>> RandomizeAppearanceMarkings(
        SpeciesPrototype species,
        Sex sex,
        CharacterPalette palette,
        IPrototypeManager protoMan,
        IRobustRandom random,
        RandomizeCfg? charEditorRandomizeConfig,
        HumanoidCharacterAppearance? baseAppearance,
        MarkingRandomizationConfig? markingRandomizationConfig)
    {
        var randomizeCfg = charEditorRandomizeConfig ?? RandomizeConfigAll;
        var markingData = IoCManager.Resolve<MarkingManager>().GetMarkingData(species);

        var markings = baseAppearance?.Markings.ToDictionary(
            entry => entry.Key,
            entry => entry.Value.ToDictionary(layer => layer.Key, layer => layer.Value.ToList()))
            ?? new Dictionary<ProtoId<OrganCategoryPrototype>, Dictionary<HumanoidVisualLayers, List<Marking>>>();

        // Build a complete candidate appearance, then apply only layers whose
        // style/color controls are enabled. This also respects locks for new profiles.
        var randomized = RandomizeMarkings(species, sex, palette, protoMan, random);

        foreach (var organ in markings.Keys.Union(randomized.Keys).ToList())
        {
            var currentLayers = markings.GetValueOrDefault(organ) ?? new();
            var randomLayers = randomized.GetValueOrDefault(organ);
            foreach (var layer in Enum.GetValues<HumanoidVisualLayers>())
            {
                var randomizeStyle = ShouldRandomizeLayer(layer, randomizeCfg, markingRandomizationConfig);
                var randomizeColor = ShouldRandomizeLayerColor(layer, randomizeCfg, markingRandomizationConfig);
                if (!randomizeStyle && !randomizeColor)
                    continue;

                if (randomizeStyle)
                {
                    if (randomLayers != null && randomLayers.TryGetValue(layer, out var selected))
                    {
                        var selectedMarkings = selected.ToList();
                        if (!randomizeColor)
                            selectedMarkings = PreserveLayerColors(selectedMarkings, currentLayers.GetValueOrDefault(layer), protoMan);

                        currentLayers[layer] = selectedMarkings;
                    }
                    else
                    {
                        currentLayers.Remove(layer);
                    }
                }
                else if (currentLayers.TryGetValue(layer, out var existingMarkings))
                {
                    var matchSkin = markingData.TryGetValue(organ, out var organData) &&
                                    protoMan.TryIndex(organData.Group, out MarkingsGroupPrototype? groupPrototype) &&
                                    groupPrototype.Appearances.TryGetValue(layer, out var appearance) &&
                                    appearance.MatchSkin;

                    currentLayers[layer] = RandomizeLayerColors(existingMarkings.ToList(), layer, palette, protoMan, random, matchSkin);
                }
            }

            if (currentLayers.Count > 0)
                markings[organ] = currentLayers;
            else
                markings.Remove(organ);
        }

        return markings;
    }

    private static bool ShouldRandomizeLayer(HumanoidVisualLayers layer, RandomizeCfg config, MarkingRandomizationConfig? perLayerConfig)
    {
        if ((config & RandomizeCfg.Markings) == 0)
            return false;

        if (perLayerConfig != null)
            return perLayerConfig.StyleLayers.Contains(layer);

        return layer switch
        {
            HumanoidVisualLayers.Hair => (config & RandomizeCfg.Hair) != 0,
            HumanoidVisualLayers.FacialHair => (config & RandomizeCfg.FacialHair) != 0,
            HumanoidVisualLayers.HeadTop => (config & RandomizeCfg.HeadTop) != 0,
            HumanoidVisualLayers.Tail or HumanoidVisualLayers.TailOverlay => (config & RandomizeCfg.Tail) != 0,
            _ => (config & RandomizeCfg.BodyMarkings) != 0,
        };
    }

    private static bool ShouldRandomizeLayerColor(HumanoidVisualLayers layer, RandomizeCfg config, MarkingRandomizationConfig? perLayerConfig)
    {
        if ((config & RandomizeCfg.Markings) == 0)
            return false;

        if (perLayerConfig != null)
            return perLayerConfig.ColorLayers.Contains(layer);

        return layer switch
        {
            HumanoidVisualLayers.Hair => (config & RandomizeCfg.HairColor) != 0,
            HumanoidVisualLayers.FacialHair => (config & RandomizeCfg.FacialHairColor) != 0,
            HumanoidVisualLayers.HeadTop => (config & RandomizeCfg.HeadTopColor) != 0,
            HumanoidVisualLayers.Tail or HumanoidVisualLayers.TailOverlay => (config & RandomizeCfg.TailColor) != 0,
            _ => (config & RandomizeCfg.BodyMarkingsColor) != 0,
        };
    }

    private static List<Marking> PreserveLayerColors(List<Marking> randomized, List<Marking>? previous, IPrototypeManager protoMan)
    {
        if (previous is null || previous.Count == 0)
            return randomized;

        for (var markingIndex = 0; markingIndex < randomized.Count; markingIndex++)
        {
            var oldMarking = previous[Math.Min(markingIndex, previous.Count - 1)];
            var newMarking = randomized[markingIndex];
            if (!protoMan.TryIndex(newMarking.MarkingId, out MarkingPrototype? prototype) || prototype.ForcedColoring)
                continue;

            for (var colorIndex = 0; colorIndex < Math.Min(oldMarking.MarkingColors.Count, newMarking.MarkingColors.Count); colorIndex++)
            {
                if (colorIndex >= prototype.Sprites.Count ||
                    prototype.Sprites[colorIndex].ForcedColoring ||
                    UsesContextualMarkingColor(prototype, colorIndex))
                {
                    continue;
                }

                newMarking = newMarking.WithColorAt(colorIndex, oldMarking.MarkingColors[colorIndex]);
            }

            randomized[markingIndex] = newMarking;
        }

        return randomized;
    }

    private static List<Marking> RandomizeLayerColors(
        List<Marking> markings,
        HumanoidVisualLayers layer,
        CharacterPalette palette,
        IPrototypeManager protoMan,
        IRobustRandom random,
        bool matchSkin)
    {
        for (var markingIndex = 0; markingIndex < markings.Count; markingIndex++)
        {
            var otherMarkings = markings.Where((_, index) => index != markingIndex).ToList();
            markings[markingIndex] = RandomizeMarkingColors(
                markings[markingIndex],
                layer,
                palette,
                protoMan,
                random,
                matchSkin,
                otherMarkings);
        }

        return markings;
    }
}
