using System;
using Content.Shared.Preferences;
using Content.Shared.Preferences.Loadouts;
using Robust.Shared;
using Robust.Shared.Random;

namespace Content.Client.Lobby.UI;

public sealed partial class HumanoidProfileEditor
{
    private void RandomizeNameOnly()
    {
        if (Profile is null)
            return;

        var config = RandomizeLockButton.RandomizeCfg;
        var randomizeFirstName = (config & HumanoidCharacterProfile.RandomizeCfg.FirstName) != 0;
        var randomizeLastName = (config & HumanoidCharacterProfile.RandomizeCfg.LastName) != 0;

        if (!randomizeFirstName && !randomizeLastName)
            return;

        Profile = Profile.RandomizeName(randomizeFirstName, randomizeLastName);
        UpdateNameEdit();
        ReloadPreview();
        SetDirty();
    }

    private void ToggleRandomizationSettings()
    {
        RandomizationSettingsPanel.Visible = !RandomizationSettingsPanel.Visible;
    }

    /// <summary>
    /// Randomize values selectively while respecting locked values.
    /// </summary>
    private void RandomizeProfile()
    {
        var config = RandomizeLockButton.RandomizeCfg;
        var markingConfig = GetMarkingRandomizationConfig();
        Profile = Profile == null
            ? HumanoidCharacterProfile.Random(config, new HumanoidCharacterProfile(), markingConfig)
            : HumanoidCharacterProfile.Random(config, Profile, markingConfig);

        if (Profile == null)
            return;

        var random = IoCManager.Resolve<IRobustRandom>();

        if ((config & HumanoidCharacterProfile.RandomizeCfg.Jobs) != 0)
            RandomizeJobPreferences(random);

        if ((config & HumanoidCharacterProfile.RandomizeCfg.Antags) != 0)
            RandomizeAntagPreferences(random);

        if ((config & HumanoidCharacterProfile.RandomizeCfg.Traits) != 0)
            RandomizeTraitPreferences(random);

        if ((config & HumanoidCharacterProfile.RandomizeCfg.SpawnPriority) != 0)
            Profile = Profile.WithSpawnPriorityPreference(random.Pick(Enum.GetValues<SpawnPriorityPreference>()));

        if ((config & HumanoidCharacterProfile.RandomizeCfg.PreferenceUnavailable) != 0)
            Profile = Profile.WithPreferenceUnavailable(random.Pick(Enum.GetValues<PreferenceUnavailableMode>()));

        if ((config & HumanoidCharacterProfile.RandomizeCfg.Loadouts) != 0)
        {
            RandomizeRoleLoadouts(
                ConsiderClothingCheckBox.Pressed,
                (LoadoutClothingStyle) ClothingStyleButton.SelectedId,
                (UniformRandomizationStyle) UniformStyleButton.SelectedId,
                random);
        }

        SetProfile(Profile, CharacterSlot);
        SetDirty();
    }
}

