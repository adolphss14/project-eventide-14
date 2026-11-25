using System.Collections.Generic;
using Content.Shared.Body;
using Robust.Shared.Prototypes;

namespace Content.Shared.Humanoid;

/// <summary>
/// Identifies a randomizable appearance parameter by both its owning organ and visual layer.
/// This avoids coupling unrelated organs that happen to use the same visual layer.
/// </summary>
public readonly record struct MarkingLayerKey(
    ProtoId<OrganCategoryPrototype> Organ,
    HumanoidVisualLayers Layer);

/// <summary>
/// Per-organ, per-layer randomization switches for the character editor.
/// Style and color randomization are configured independently.
/// </summary>
public sealed record MarkingRandomizationConfig(
    IReadOnlySet<MarkingLayerKey> StyleLayers,
    IReadOnlySet<MarkingLayerKey> ColorLayers);
