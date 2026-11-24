using System.Linq;
using Content.Shared.Corvax.CCCVars;
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


    private void BindNamePartRandomizationFlags()
    {
        RandomizeFirstNameCheckBox.Pressed =
            (RandomizeLockButton.RandomizeCfg & HumanoidCharacterProfile.RandomizeCfg.FirstName) != 0;
        RandomizeLastNameCheckBox.Pressed =
            (RandomizeLockButton.RandomizeCfg & HumanoidCharacterProfile.RandomizeCfg.LastName) != 0;

        RandomizeFirstNameCheckBox.OnToggled += args =>
        {
            if (args.Pressed)
                RandomizeLockButton.RandomizeCfg |= HumanoidCharacterProfile.RandomizeCfg.FirstName;
            else
                RandomizeLockButton.RandomizeCfg &= ~HumanoidCharacterProfile.RandomizeCfg.FirstName;

            SyncNameRandomizationFlag();
        };

        RandomizeLastNameCheckBox.OnToggled += args =>
        {
            if (args.Pressed)
                RandomizeLockButton.RandomizeCfg |= HumanoidCharacterProfile.RandomizeCfg.LastName;
            else
                RandomizeLockButton.RandomizeCfg &= ~HumanoidCharacterProfile.RandomizeCfg.LastName;

            SyncNameRandomizationFlag();
        };

        SyncNameRandomizationFlag();
    }

    private static void SyncNameRandomizationFlag()
    {
        if ((RandomizeLockButton.RandomizeCfg &
             (HumanoidCharacterProfile.RandomizeCfg.FirstName | HumanoidCharacterProfile.RandomizeCfg.LastName)) != 0)
        {
            RandomizeLockButton.RandomizeCfg |= HumanoidCharacterProfile.RandomizeCfg.Name;
        }
        else
        {
            RandomizeLockButton.RandomizeCfg &= ~HumanoidCharacterProfile.RandomizeCfg.Name;
        }
    }

    private void InitializeRandomizationControls()
    {
        RandomizeUnlockedButton.OnPressed += _ => RandomizeProfile();
        RandomizeNameButton.OnPressed += _ => RandomizeNameOnly();
        RandomizeSettingsButton.OnPressed += _ => ToggleRandomizationSettings();

        BindNamePartRandomizationFlags();
        BindRandomizationFlag(RandomizeSpeciesCheckBox, HumanoidCharacterProfile.RandomizeCfg.Species);
        BindRandomizationFlag(RandomizeAgeCheckBox, HumanoidCharacterProfile.RandomizeCfg.Age);
        BindRandomizationFlag(RandomizeSexCheckBox, HumanoidCharacterProfile.RandomizeCfg.Sex);
        BindRandomizationFlag(RandomizeGenderCheckBox, HumanoidCharacterProfile.RandomizeCfg.Gender);
        BindRandomizationFlag(RandomizeSkinCheckBox, HumanoidCharacterProfile.RandomizeCfg.Skin);
        BindRandomizationFlag(RandomizeEyesCheckBox, HumanoidCharacterProfile.RandomizeCfg.Eyes);
        BindRandomizationFlag(RandomizeVoiceCheckBox, HumanoidCharacterProfile.RandomizeCfg.Voice);

        RandomizeTTSVoiceCheckBox.Visible = _cfgManager.GetCVar(CCCVars.TTSEnabled);
        if (!RandomizeTTSVoiceCheckBox.Visible)
            RandomizeLockButton.RandomizeCfg &= ~HumanoidCharacterProfile.RandomizeCfg.TTSVoice;
        BindRandomizationFlag(RandomizeTTSVoiceCheckBox, HumanoidCharacterProfile.RandomizeCfg.TTSVoice);
        BindRandomizationFlag(RandomizeJobsCheckBox, HumanoidCharacterProfile.RandomizeCfg.Jobs);
        BindRandomizationFlag(RandomizeAntagsCheckBox, HumanoidCharacterProfile.RandomizeCfg.Antags);
        BindRandomizationFlag(RandomizeTraitsCheckBox, HumanoidCharacterProfile.RandomizeCfg.Traits);
        BindRandomizationFlag(RandomizeLoadoutsCheckBox, HumanoidCharacterProfile.RandomizeCfg.Loadouts);
        BindRandomizationFlag(RandomizeSpawnPriorityCheckBox, HumanoidCharacterProfile.RandomizeCfg.SpawnPriority);
        BindRandomizationFlag(RandomizePreferenceUnavailableCheckBox, HumanoidCharacterProfile.RandomizeCfg.PreferenceUnavailable);

        ConsiderClothingCheckBox.OnToggled += args =>
        {
            ClothingStyleLabel.Visible = args.Pressed;
            ClothingStyleButton.Visible = args.Pressed;
            UniformStyleLabel.Visible = args.Pressed;
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
    private void UpdateMarkingsRandomizationFlag()
    {
        var availableLayers = _markingsModel.OrganData
            .SelectMany(organ => organ.Value.Layers.Select(layer => new MarkingLayerKey(organ.Key, layer)))
            .ToHashSet();

        var enabled = availableLayers.Any(key =>
            !_lockedMarkingStyles.Contains(key) || !_lockedMarkingColors.Contains(key));

        if (enabled)
            RandomizeLockButton.RandomizeCfg |= HumanoidCharacterProfile.RandomizeCfg.Markings;
        else
            RandomizeLockButton.RandomizeCfg &= ~HumanoidCharacterProfile.RandomizeCfg.Markings;
    }

}
