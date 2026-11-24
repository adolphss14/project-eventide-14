using System;
using System.Collections.Generic;
using System.Linq;
using Content.Shared.Humanoid;
using Robust.Client.UserInterface.Controls;
using Robust.Shared.Utility;

namespace Content.Client.Lobby.UI;

public sealed partial class HumanoidProfileEditor
{
    // Store exclusions rather than inclusions: layers discovered for a new species
    // are enabled by default, while explicit locks survive species changes.
    private readonly HashSet<MarkingLayerKey> _lockedMarkingStyles = new();
    private readonly HashSet<MarkingLayerKey> _lockedMarkingColors = new();

    private void UpdateRandomizerOptions()
    {
        RandomizeMarkingLayersContainer.RemoveAllChildren();

        foreach (var (organ, organData) in _markingsModel.OrganData
                     .OrderBy(entry => entry.Key.Id, StringComparer.Ordinal))
        {
            var layers = organData.Layers
                .Distinct()
                .OrderBy(layer => (byte) layer)
                .ToArray();

            if (layers.Length == 0)
                continue;

            var section = new BoxContainer
            {
                Orientation = BoxContainer.LayoutOrientation.Vertical,
                HorizontalExpand = true,
            };
            section.AddChild(new Label
            {
                Text = Loc.GetString("humanoid-profile-editor-randomizer-organ", ("organ", organ.Id)),
            });

            var options = new GridContainer
            {
                Columns = 2,
                HorizontalExpand = true,
            };

            foreach (var layer in layers)
            {
                var key = new MarkingLayerKey(organ, layer);
                var layerName = Loc.GetString($"humanoid-profile-editor-randomizer-layer-{layer.ToString().ToLowerInvariant()}");

                var style = new CheckBox
                {
                    Text = Loc.GetString("humanoid-profile-editor-randomizer-layer-style", ("layer", layerName)),
                    Pressed = !_lockedMarkingStyles.Contains(key),
                    HorizontalExpand = true,
                };
                style.OnToggled += args =>
                {
                    if (args.Pressed)
                        _lockedMarkingStyles.Remove(key);
                    else
                        _lockedMarkingStyles.Add(key);

                    UpdateMarkingsRandomizationFlag();
                };
                options.AddChild(style);

                var color = new CheckBox
                {
                    Text = Loc.GetString("humanoid-profile-editor-randomizer-layer-color", ("layer", layerName)),
                    Pressed = !_lockedMarkingColors.Contains(key),
                    HorizontalExpand = true,
                };
                color.OnToggled += args =>
                {
                    if (args.Pressed)
                        _lockedMarkingColors.Remove(key);
                    else
                        _lockedMarkingColors.Add(key);

                    UpdateMarkingsRandomizationFlag();
                };
                options.AddChild(color);
            }

            section.AddChild(options);
            RandomizeMarkingLayersContainer.AddChild(section);
        }

        UpdateMarkingsRandomizationFlag();
    }

    private MarkingRandomizationConfig GetMarkingRandomizationConfig()
    {
        var availableLayers = _markingsModel.OrganData
            .SelectMany(organ => organ.Value.Layers.Select(layer => new MarkingLayerKey(organ.Key, layer)))
            .ToHashSet();

        return new MarkingRandomizationConfig(
            availableLayers.Where(key => !_lockedMarkingStyles.Contains(key)).ToHashSet(),
            availableLayers.Where(key => !_lockedMarkingColors.Contains(key)).ToHashSet());
    }
}
