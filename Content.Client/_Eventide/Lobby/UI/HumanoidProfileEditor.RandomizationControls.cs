using Robust.Shared.Utility;

namespace Content.Client.Lobby.UI;

public sealed partial class HumanoidProfileEditor
{
    private void InitializeRandomizationControls()
    {
                    RandomizeUnlockedButton.OnPressed += args => { RandomizeProfile(); };
                    RandomizeNameButton.OnPressed += _ => RandomizeNameOnly();
                    NameRandomizeSettingsButton.OnPressed += _ => ToggleRandomizationSettings();
                    RandomizeSettingsButton.OnPressed += _ => ToggleRandomizationSettings();
                    ConsiderClothingCheckBox.OnToggled += args =>
                    {
                        ClothingStyleButton.Visible = args.Pressed;
                        UniformStyleButton.Visible = args.Pressed;
                    };
                    ClothingStyleButton.AddItem(Loc.GetString("humanoid-profile-editor-randomizer-clothing-any"), 0);
                    ClothingStyleButton.AddItem(Loc.GetString("humanoid-profile-editor-randomizer-clothing-male"), 1);
                    ClothingStyleButton.AddItem(Loc.GetString("humanoid-profile-editor-randomizer-clothing-female"), 2);
                    ClothingStyleButton.SelectId(0);
                    UniformStyleButton.AddItem(Loc.GetString("humanoid-profile-editor-randomizer-uniform-any"), 0);
                    UniformStyleButton.AddItem(Loc.GetString("humanoid-profile-editor-randomizer-uniform-jumpsuit"), 1);
                    UniformStyleButton.AddItem(Loc.GetString("humanoid-profile-editor-randomizer-uniform-jumpskirt"), 2);
                    UniformStyleButton.SelectId(0);
    }
}
