using Content.Shared.Preferences;
using Robust.Client.UserInterface.Controls;
using Robust.Shared.Utility;

namespace Content.Client.Lobby.UI;

public sealed partial class HumanoidProfileEditor
{
    private static void BindRandomizationFlag(CheckBox checkBox, HumanoidCharacterProfile.RandomizeCfg flag)
    {
        checkBox.Pressed = (RandomizeLockButton.RandomizeCfg & flag) != 0;
        checkBox.OnToggled += args =>
        {
            if (args.Pressed)
                RandomizeLockButton.RandomizeCfg |= flag;
            else
                RandomizeLockButton.RandomizeCfg &= ~flag;
        };
    }

    private void InitializeRandomizationControls()
    {
        RandomizeUnlockedButton.OnPressed += _ => RandomizeProfile();
        RandomizeNameButton.OnPressed += _ => RandomizeNameOnly();
        RandomizeSettingsButton.OnPressed += _ => ToggleRandomizationSettings();

        BindRandomizationFlag(RandomizeNameCheckBox, HumanoidCharacterProfile.RandomizeCfg.Name);
        BindRandomizationFlag(RandomizeFirstNameCheckBox, HumanoidCharacterProfile.RandomizeCfg.FirstName);
        BindRandomizationFlag(RandomizeLastNameCheckBox, HumanoidCharacterProfile.RandomizeCfg.LastName);
        BindRandomizationFlag(RandomizeSpeciesCheckBox, HumanoidCharacterProfile.RandomizeCfg.Species);
        BindRandomizationFlag(RandomizeAgeCheckBox, HumanoidCharacterProfile.RandomizeCfg.Age);
        BindRandomizationFlag(RandomizeSexCheckBox, HumanoidCharacterProfile.RandomizeCfg.Sex);
        BindRandomizationFlag(RandomizeGenderCheckBox, HumanoidCharacterProfile.RandomizeCfg.Gender);
        BindRandomizationFlag(RandomizeSkinCheckBox, HumanoidCharacterProfile.RandomizeCfg.Skin);
        BindRandomizationFlag(RandomizeEyesCheckBox, HumanoidCharacterProfile.RandomizeCfg.Eyes);
        BindRandomizationFlag(RandomizeMarkingsCheckBox, HumanoidCharacterProfile.RandomizeCfg.Markings);
        BindRandomizationFlag(RandomizeJobsCheckBox, HumanoidCharacterProfile.RandomizeCfg.Jobs);
        BindRandomizationFlag(RandomizeAntagsCheckBox, HumanoidCharacterProfile.RandomizeCfg.Antags);
        BindRandomizationFlag(RandomizeTraitsCheckBox, HumanoidCharacterProfile.RandomizeCfg.Traits);
        BindRandomizationFlag(RandomizeLoadoutsCheckBox, HumanoidCharacterProfile.RandomizeCfg.Loadouts);

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
