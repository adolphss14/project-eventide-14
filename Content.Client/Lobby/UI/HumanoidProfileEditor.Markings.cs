using Content.Shared.Humanoid;
using Content.Shared.Humanoid.Prototypes;

namespace Content.Client.Lobby.UI;

public sealed partial class HumanoidProfileEditor
{
    private void UpdateMarkingSkinColorMode()
    {
        if (Profile is null)
            return;

        var species = _prototypeManager.Index<SpeciesPrototype>(Profile.Species);
        var skinColoration = _prototypeManager.Index(species.SkinColoration);
        _markingsModel.SetFullColorSkinInput(
            skinColoration.Strategy.InputType == SkinColorationStrategyInput.Color);
    }

    private void UpdateMarkings()
    {
        if (Profile == null)
        {
            return;
        }

        UpdateMarkingSkinColorMode();
        _markingsModel.OrganData = _markingManager.GetMarkingData(Profile.Species);
        _markingsModel.OrganProfileData = _markingManager.GetProfileData(Profile.Species, Profile.Sex, Profile.Appearance.SkinColor, Profile.Appearance.EyeColor);
        _markingsModel.Markings = Profile.Appearance.Markings;
        UpdateRandomizerOptions();
    }

    private void OnMarkingChange()
    {
        if (Profile is null)
            return;

        Profile = Profile.WithCharacterAppearance(Profile.Appearance.WithMarkings(_markingsModel.Markings));
        ReloadProfilePreview();
        SetDirty();
    }
}
