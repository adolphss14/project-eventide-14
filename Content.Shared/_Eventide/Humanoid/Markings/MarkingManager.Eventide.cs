using Content.Shared.Humanoid.Prototypes;

namespace Content.Shared.Humanoid.Markings;

public sealed partial class MarkingManager
{
    /// <summary>
    /// Checks the active markings group and its parent groups. This lets specialized species
    /// inherit compatible customization options without copying every parent marking prototype.
    /// </summary>
    private bool IsGroupWhitelisted(MarkingPrototype marking, MarkingsGroupPrototype group)
    {
        if (marking.GroupWhitelist!.Contains(group))
            return true;

        foreach (var parentId in group.Parents ?? [])
        {
            var parent = _prototype.Index<MarkingsGroupPrototype>(parentId);
            if (IsGroupWhitelisted(marking, parent))
                return true;
        }

        return false;
    }

}
