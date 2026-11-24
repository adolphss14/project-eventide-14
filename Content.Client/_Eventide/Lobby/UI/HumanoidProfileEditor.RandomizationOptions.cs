using System;
using System.Collections.Generic;
using System.Linq;
using Content.Shared.Humanoid;
using Robust.Client.UserInterface.Controls;
using Robust.Shared.Utility;

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
            var style = new CheckBox
            {
                Text = Loc.GetString("humanoid-profile-editor-randomizer-layer-style", ("layer", layerName)),
                Pressed = _randomizeMarkingStyles.Contains(layer),
            };
            style.OnToggled += args =>
            {
                if (args.Pressed)
                    _randomizeMarkingStyles.Add(layer);
                else
                    _randomizeMarkingStyles.Remove(layer);
            };
            RandomizeMarkingLayersContainer.AddChild(style);

            var color = new CheckBox
            {
                Text = Loc.GetString("humanoid-profile-editor-randomizer-layer-color", ("layer", layerName)),
                Pressed = _randomizeMarkingColors.Contains(layer),
            };
            color.OnToggled += args =>
            {
                if (args.Pressed)
                    _randomizeMarkingColors.Add(layer);
                else
                    _randomizeMarkingColors.Remove(layer);
            };
            RandomizeMarkingLayersContainer.AddChild(color);
        }
    }

    private MarkingRandomizationConfig GetMarkingRandomizationConfig()
    {
        return new MarkingRandomizationConfig(_randomizeMarkingStyles, _randomizeMarkingColors);
    }
}
