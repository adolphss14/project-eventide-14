using System.Collections.Generic;
using System.Linq;
using Content.Shared.Body;
using Content.Shared.Humanoid;
using Content.Shared.Humanoid.Markings;
using Content.Shared.Humanoid.Markings.ColoringTypes;
using Robust.Shared.Maths;
using Robust.Shared.Prototypes;

namespace Content.Client.Humanoid;

public sealed partial class MarkingsViewModel
{
    private readonly Dictionary<(ProtoId<OrganCategoryPrototype> Organ, HumanoidVisualLayers Layer), List<Color?>> _previousLayerColors = new();

    /// <summary>
    /// Clears transient appearance color history when the editor switches to another character profile.
    /// </summary>
    public void ResetColorMemory()
    {
        _previousColors.Clear();
        _previousLayerColors.Clear();
    }

    /// <summary>
    /// Returns the coloring definition actually used by one sprite channel.
    /// New-style per-sprite coloring takes priority over the legacy per-state map,
    /// which in turn takes priority over the marking-wide default.
    /// </summary>
    private static LayerColoringDefinition GetLayerColoring(
        MarkingPrototype markingPrototype,
        int colorIndex,
        out bool hasExplicitColoring)
    {
        var sprite = markingPrototype.Sprites[colorIndex];
        var coloring = sprite.Coloring;
        hasExplicitColoring = coloring is not null;

        if (coloring is null &&
            markingPrototype.Coloring.Layers is { } layers &&
            layers.TryGetValue(sprite.GetLayerStateId(), out var layerColoring))
        {
            coloring = layerColoring;
            hasExplicitColoring = true;
        }

        return coloring ?? markingPrototype.Coloring.Default;
    }

    /// <summary>
    /// Checks whether a marking channel derives its default from the species skin color.
    /// This deliberately ignores hair and facial hair: their palette should remain
    /// independent while players switch hairstyles or facial-hair styles.
    /// </summary>
    private static bool UsesSpeciesSkinColorByDefault(MarkingPrototype markingPrototype, int colorIndex)
    {
        if (markingPrototype.BodyPart is HumanoidVisualLayers.Hair or HumanoidVisualLayers.FacialHair ||
            markingPrototype.ForcedColoring ||
            colorIndex < 0 ||
            colorIndex >= markingPrototype.Sprites.Count)
        {
            return false;
        }

        var coloring = GetLayerColoring(markingPrototype, colorIndex, out var hasExplicitColoring);

        // Skin and tattoo channels are directly tied to the current skin tone.
        // CategoryColoring is also contextual: when its source marking is missing,
        // its fallback must be recalculated instead of restoring an old cached value.
        if (coloring.Type is SkinColoring or TattooColoring or CategoryColoring)
            return true;

        // LayerColoringDefinition has an implicit SkinColoring fallback by default.
        // That implicit fallback is not a skin-linked definition: ordinary channels
        // use the generated palette and must retain the player's custom color.
        return coloring.Type is null &&
               hasExplicitColoring &&
               coloring.FallbackTypes.Any(type => type is SkinColoring or TattooColoring);
    }

    /// <summary>
    /// Stores only colors that should be restored later. In species with a full-color
    /// skin selector, body-color-derived channels are left unset so their prototype
    /// defaults are recalculated from the current skin tone when selected again.
    /// </summary>
    private List<Color?> GetRememberableColors(Marking marking)
    {
        var colors = new List<Color?>(marking.MarkingColors.Count);

        // Saved appearances can contain stale markings whose prototype ID is empty
        // or whose prototype is no longer available. Preserve their stored colors
        // without trying to resolve metadata that cannot exist.
        if (string.IsNullOrEmpty(marking.MarkingId.Id) ||
            !_prototype.TryIndex(marking.MarkingId, out var markingPrototype))
        {
            colors.AddRange(marking.MarkingColors.Select(color => (Color?) color));
            return colors;
        }

        for (var i = 0; i < marking.MarkingColors.Count; i++)
        {
            var forced = markingPrototype.ForcedColoring ||
                         i < markingPrototype.Sprites.Count && markingPrototype.Sprites[i].ForcedColoring;
            var followsSpeciesSkin = _usesFullColorSkinInput &&
                                     UsesSpeciesSkinColorByDefault(markingPrototype, i);

            colors.Add(forced || followsSpeciesSkin ? null : marking.MarkingColors[i]);
        }

        return colors;
    }

    private void RememberMarkingColors(
        ProtoId<OrganCategoryPrototype> organ,
        HumanoidVisualLayers layer,
        Marking marking)
    {
        // A default ProtoId has no usable key. Its colors can still be handled by
        // GetRememberableColors, but it must not be inserted into the keyed history.
        if (string.IsNullOrEmpty(marking.MarkingId.Id))
            return;

        _previousColors[(organ, layer, marking.MarkingId)] = GetRememberableColors(marking);
    }

    private void ApplyRememberedColors(
        List<Color> colors,
        List<Color?>? rememberedColors,
        MarkingPrototype markingPrototype)
    {
        if (rememberedColors == null || markingPrototype.ForcedColoring)
            return;

        for (var i = 0; i < Math.Min(colors.Count, rememberedColors.Count); i++)
        {
            var rememberedColor = rememberedColors[i];
            if (rememberedColor is null ||
                i < markingPrototype.Sprites.Count && markingPrototype.Sprites[i].ForcedColoring)
            {
                continue;
            }

            // A body-color-derived channel on a full-color species must use the
            // current profile skin color, not a remembered custom value.
            if (_usesFullColorSkinInput && UsesSpeciesSkinColorByDefault(markingPrototype, i))
                continue;

            colors[i] = rememberedColor.Value;
        }
    }

    public bool TrySelectMarking(ProtoId<OrganCategoryPrototype> organ,
        HumanoidVisualLayers layer,
        ProtoId<MarkingPrototype> markingId)
    {
        if (string.IsNullOrEmpty(markingId.Id) || !_prototype.TryIndex(markingId, out var markingProto))
            return false;

        if (!_organData.TryGetValue(organ, out var organData) || !_organProfileData.TryGetValue(organ, out var profileData))
            return false;

        if (!organData.Layers.Contains(layer))
            return false;

        if (!_prototype.TryIndex(organData.Group, out var groupPrototype))
            return false;

        if (EnforceGroupAndSexRestrictions && !_marking.CanBeApplied(organData.Group, profileData.Sex, markingProto))
            return false;

        _markings[organ] = _markings.GetValueOrDefault(organ) ?? [];
        var organMarkings = _markings[organ];
        organMarkings[layer] = organMarkings.GetValueOrDefault(layer) ?? [];
        var layerMarkings = organMarkings[layer];

        var limits = groupPrototype.Limits.GetValueOrDefault(layer);
        var usesLayerPalette = EnforceLimits && limits is { Limit: 1 };
        var layerKey = (organ, layer);
        var markingKey = (organ, layer, markingId);

        // Preserve the shared palette only for style-like, single-choice layers.
        // Every other layer remembers each marking independently.
        if (limits is { Limit: 1 } && layerMarkings.FirstOrDefault() is { } currentMarking)
        {
            // A direct replacement is also a removal for color-memory purposes.
            // Keep this marking's own palette even when the layer does not share
            // colors between styles (for example, snouts or single body markings).
            RememberMarkingColors(organ, layer, currentMarking);

            if (usesLayerPalette &&
                !string.IsNullOrEmpty(currentMarking.MarkingId.Id) &&
                IsMarkingColorCustomizable(organ, layer, currentMarking.MarkingId))
                _previousLayerColors[layerKey] = GetRememberableColors(currentMarking);
        }

        var colors = MarkingColoring.GetMarkingLayerColors(markingProto, profileData.SkinColor, profileData.EyeColor, layerMarkings);
        var canCustomizeColors = IsMarkingColorCustomizable(organ, layer, markingId);
        var rememberedColors = usesLayerPalette
            ? _previousLayerColors.GetValueOrDefault(layerKey) ?? _previousColors.GetValueOrDefault(markingKey)
            : _previousColors.GetValueOrDefault(markingKey);

        if (canCustomizeColors)
            ApplyRememberedColors(colors, rememberedColors, markingProto);

        var newMarking = new Marking(markingId, colors) { Forced = AnyEnforcementsLifted };

        if (limits is null || !EnforceLimits)
        {
            layerMarkings.Add(newMarking);
            RememberMarkingColors(organ, layer, newMarking);
            MarkingsChanged?.Invoke(organ, layer);
            return true;
        }

        if (limits.Limit == 1 && layerMarkings.Count == 1)
        {
            layerMarkings.Clear();
            layerMarkings.Add(newMarking);
            RememberMarkingColors(organ, layer, newMarking);

            if (usesLayerPalette && canCustomizeColors)
                _previousLayerColors[layerKey] = GetRememberableColors(newMarking);

            MarkingsChanged?.Invoke(organ, layer);
            return true;
        }

        if (layerMarkings.Count < limits.Limit)
        {
            layerMarkings.Add(newMarking);
            RememberMarkingColors(organ, layer, newMarking);

            if (usesLayerPalette && canCustomizeColors)
                _previousLayerColors[layerKey] = GetRememberableColors(newMarking);

            MarkingsChanged?.Invoke(organ, layer);
            return true;
        }

        return false;
    }

    public bool TryDeselectMarking(ProtoId<OrganCategoryPrototype> organ,
        HumanoidVisualLayers layer,
        ProtoId<MarkingPrototype> markingId)
    {
        if (!_organData.TryGetValue(organ, out var organData))
            return false;

        if (!organData.Layers.Contains(layer))
            return false;

        if (!_prototype.TryIndex(organData.Group, out var groupPrototype))
            return false;

        var limits = groupPrototype.Limits.GetValueOrDefault(layer);
        var usesLayerPalette = EnforceLimits && limits is { Limit: 1 };

        _markings[organ] = _markings.GetValueOrDefault(organ) ?? [];
        var organMarkings = _markings[organ];
        organMarkings[layer] = organMarkings.GetValueOrDefault(layer) ?? [];
        var layerMarkings = organMarkings[layer];

        var count = layerMarkings.Count(marking => marking.MarkingId == markingId);
        if (count == 0)
            return false;

        if (EnforceLimits && limits is not null && limits.Required && (layerMarkings.Count - count) <= 0)
            return false;

        if (layerMarkings.Find(marking => marking.MarkingId == markingId) is { } removingMarking)
        {
            RememberMarkingColors(organ, layer, removingMarking);

            if (usesLayerPalette &&
                !string.IsNullOrEmpty(markingId.Id) &&
                IsMarkingColorCustomizable(organ, layer, markingId))
                _previousLayerColors[(organ, layer)] = GetRememberableColors(removingMarking);
        }

        layerMarkings.RemoveAll(marking => marking.MarkingId == markingId);
        MarkingsChanged?.Invoke(organ, layer);

        return true;
    }

    public void TrySetMarkingColor(ProtoId<OrganCategoryPrototype> organ,
        HumanoidVisualLayers layer,
        ProtoId<MarkingPrototype> markingId,
        int colorIndex,
        Color color)
    {
        if (!_markings.TryGetValue(organ, out var markingSet))
            return;

        if (!markingSet.TryGetValue(layer, out var markings))
            return;

        var markingIdx = markings.FindIndex(it => it.MarkingId == markingId);
        if (markingIdx == -1)
            return;

        if (string.IsNullOrEmpty(markingId.Id) || !_prototype.TryIndex(markingId, out var markingProto))
            return;
        if (markingProto.ForcedColoring ||
            colorIndex < 0 ||
            colorIndex >= markingProto.Sprites.Count ||
            colorIndex >= markings[markingIdx].MarkingColors.Count ||
            markingProto.Sprites[colorIndex].ForcedColoring)
        {
            return;
        }

        var newColor = color;
        if (markingProto.Strategy != null)
            newColor = markingProto.Strategy.ClosestSkinColor(color);

        markings[markingIdx] = markings[markingIdx].WithColorAt(colorIndex, newColor);
        RememberMarkingColors(organ, layer, markings[markingIdx]);

        var usesLayerPalette = EnforceLimits &&
                               _organData.TryGetValue(organ, out var organData) &&
                               _prototype.TryIndex(organData.Group, out var groupPrototype) &&
                               groupPrototype.Limits.GetValueOrDefault(layer) is { Limit: 1 };

        if (usesLayerPalette)
            _previousLayerColors[(organ, layer)] = GetRememberableColors(markings[markingIdx]);

        MarkingsChanged?.Invoke(organ, layer);
    }
}
