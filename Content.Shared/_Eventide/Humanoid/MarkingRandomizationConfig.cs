using System.Collections.Generic;

namespace Content.Shared.Humanoid;

/// <summary>
/// Per-visual-layer randomization switches for the character editor.
/// Style and color randomization are configured independently.
/// </summary>
public sealed record MarkingRandomizationConfig(
    IReadOnlySet<HumanoidVisualLayers> StyleLayers,
    IReadOnlySet<HumanoidVisualLayers> ColorLayers);
