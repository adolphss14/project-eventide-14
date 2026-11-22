using Robust.Shared.Prototypes;

namespace Content.Shared.Preferences.Loadouts;

/// <summary>
/// Broad clothing-style classification used by the character loadout randomizer.
/// Unmarked items are neutral and remain available for every style.
/// </summary>
public enum LoadoutClothingStyle
{
    Any,
    Masculine,
    Feminine
}

public sealed partial class LoadoutPrototype
{
    /// <summary>
    /// Optional style tag for clothing variants outside the uniform slot.
    /// Only clothing prototypes with a non-neutral style are restricted by the style selector.
    /// </summary>
    [DataField]
    public LoadoutClothingStyle ClothingStyle = LoadoutClothingStyle.Any;
}
