using System;
using System.Collections.Generic;
using System.Linq;
using Content.Shared.Humanoid;
using Content.Shared.Preferences;
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
            .OrderBy(GetRandomizerCategoryOrder)
            .ThenBy(layer => (byte) layer);

        foreach (var group in layers.GroupBy(GetRandomizerCategory))
        {
            var section = new BoxContainer
            {
                Orientation = BoxContainer.LayoutOrientation.Vertical,
                HorizontalExpand = true,
            };
            section.AddChild(new Label
            {
                Text = Loc.GetString($"humanoid-profile-editor-randomizer-category-{group.Key}"),
            });

            var options = new GridContainer
            {
                Columns = 2,
                HorizontalExpand = true,
            };

            foreach (var layer in group)
            {
                var layerName = Loc.GetString($"humanoid-profile-editor-randomizer-layer-{layer.ToString().ToLowerInvariant()}");
                var style = new CheckBox
                {
                    Text = Loc.GetString("humanoid-profile-editor-randomizer-layer-style", ("layer", layerName)),
                    Pressed = _randomizeMarkingStyles.Contains(layer),
                    HorizontalExpand = true,
                };
                style.OnToggled += args =>
                {
                    if (args.Pressed)
                        _randomizeMarkingStyles.Add(layer);
                    else
                        _randomizeMarkingStyles.Remove(layer);

                    UpdateMarkingsRandomizationFlag();
                };
                options.AddChild(style);

                var color = new CheckBox
                {
                    Text = Loc.GetString("humanoid-profile-editor-randomizer-layer-color", ("layer", layerName)),
                    Pressed = _randomizeMarkingColors.Contains(layer),
                    HorizontalExpand = true,
                };
                color.OnToggled += args =>
                {
                    if (args.Pressed)
                        _randomizeMarkingColors.Add(layer);
                    else
                        _randomizeMarkingColors.Remove(layer);

                    UpdateMarkingsRandomizationFlag();
                };
                options.AddChild(color);
            }

            section.AddChild(options);
            RandomizeMarkingLayersContainer.AddChild(section);
        }

        UpdateMarkingsRandomizationFlag();
    }

    private static string GetRandomizerCategory(HumanoidVisualLayers layer)
    {
        return layer switch
        {
            HumanoidVisualLayers.Hair or HumanoidVisualLayers.FacialHair => "hair",
            HumanoidVisualLayers.Head or HumanoidVisualLayers.HeadTop or
                HumanoidVisualLayers.HeadSide or HumanoidVisualLayers.Snout or
                HumanoidVisualLayers.SnoutCover or HumanoidVisualLayers.Eyes or
                HumanoidVisualLayers.Eyelids or HumanoidVisualLayers.OverEyes => "head",
            HumanoidVisualLayers.Chest => "body",
            HumanoidVisualLayers.RArm or HumanoidVisualLayers.LArm or
                HumanoidVisualLayers.RHand or HumanoidVisualLayers.LHand => "arms",
            HumanoidVisualLayers.RLeg or HumanoidVisualLayers.LLeg or
                HumanoidVisualLayers.RFoot or HumanoidVisualLayers.LFoot => "legs",
            HumanoidVisualLayers.Tail or HumanoidVisualLayers.TailOverlay => "tail",
            HumanoidVisualLayers.UndergarmentTop or HumanoidVisualLayers.UndergarmentBottom => "clothing",
            _ => "other",
        };
    }

    private static int GetRandomizerCategoryOrder(HumanoidVisualLayers layer)
    {
        return GetRandomizerCategory(layer) switch
        {
            "head" => 0,
            "hair" => 1,
            "body" => 2,
            "arms" => 3,
            "legs" => 4,
            "tail" => 5,
            "clothing" => 6,
            _ => 7,
        };
    }

    private MarkingRandomizationConfig GetMarkingRandomizationConfig()
    {
        return new MarkingRandomizationConfig(_randomizeMarkingStyles, _randomizeMarkingColors);
    }
}
