using System;
using System.Collections.Generic;
using System.Linq;
using Content.Shared.Humanoid;

namespace Content.Client.Lobby.UI;

public sealed partial class HumanoidProfileEditor
{
    // Keep settings when changing species; an unavailable layer may return on another species.
    private readonly HashSet<HumanoidVisualLayers> _randomizeMarkingStyles = Enum.GetValues<HumanoidVisualLayers>().ToHashSet();
    private readonly HashSet<HumanoidVisualLayers> _randomizeMarkingColors = Enum.GetValues<HumanoidVisualLayers>().ToHashSet();

    private void UpdateRandomizerOptions()
    {
        RandomizeMarkingLayersContainer.RemoveAllChildren();
        var layers = _markingsModel.OrganData.Values
            .SelectMany(data => data.Layers)
            .Distinct()
            .OrderBy(layer => (byte) layer);

        foreach (var layer in layers)
        {
            var layerName = Loc.GetString($"humanoid-profile-editor-randomizer-layer-{layer.ToString().ToLowerInvariant()}");
            var style = new RandomizeLockButton
            {
                For = default,
                LabelText = Loc.GetString("humanoid-profile-editor-randomizer-layer-style", ("layer", layerName)),
            };
            style.SetLocked(!_randomizeMarkingStyles.Contains(layer));
            style.RandomizationEnabledChanged += enabled =>
            {
                if (enabled) _randomizeMarkingStyles.Add(layer);
                else _randomizeMarkingStyles.Remove(layer);
            };
            RandomizeMarkingLayersContainer.AddChild(style);

            var color = new RandomizeLockButton
            {
                For = default,
                LabelText = Loc.GetString("humanoid-profile-editor-randomizer-layer-color", ("layer", layerName)),
            };
            color.SetLocked(!_randomizeMarkingColors.Contains(layer));
            color.RandomizationEnabledChanged += enabled =>
            {
                if (enabled) _randomizeMarkingColors.Add(layer);
                else _randomizeMarkingColors.Remove(layer);
            };
            RandomizeMarkingLayersContainer.AddChild(color);
        }
    }

    private MarkingRandomizationConfig GetMarkingRandomizationConfig()
    {
        return new MarkingRandomizationConfig(_randomizeMarkingStyles, _randomizeMarkingColors);
    }
}
