using System;
using System.Collections.Generic;
using System.Linq;
using Content.Shared.Antag;
using Content.Shared.Preferences;
using Content.Shared.Roles;
using Content.Shared.Traits;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;

namespace Content.Client.Lobby.UI;

public sealed partial class HumanoidProfileEditor
{
    /// <summary>
    /// Randomizes job priorities while keeping the single-high-priority rule enforced by the profile.
    /// </summary>
    private void RandomizeJobPreferences(IRobustRandom random)
    {
        if (Profile is null)
            return;

        var jobs = _prototypeManager.EnumeratePrototypes<JobPrototype>()
            .Where(job => job.SetPreference && _requirements.IsAllowed(job, Profile, out _))
            .ToArray();

        if (jobs.Length == 0)
            return;

        var highPriorityJob = random.Pick(jobs);
        var otherPriorities = new[] { JobPriority.Never, JobPriority.Low, JobPriority.Medium };
        var priorities = new Dictionary<ProtoId<JobPrototype>, JobPriority>();

        foreach (var job in jobs)
        {
            if (job.ID == highPriorityJob.ID)
            {
                priorities[job.ID] = JobPriority.High;
                continue;
            }

            var priority = random.Pick(otherPriorities);
            if (priority != JobPriority.Never)
                priorities[job.ID] = priority;
        }

        Profile = Profile.WithJobPriorities(priorities);
    }

    /// <summary>
    /// Randomizes antagonist opt-ins, excluding roles the current account cannot select.
    /// </summary>
    private void RandomizeAntagPreferences(IRobustRandom random)
    {
        if (Profile is null)
            return;

        foreach (var antag in Profile.AntagPreferences.ToArray())
            Profile = Profile.WithAntagPreference(antag, false);

        var antags = _prototypeManager.EnumeratePrototypes<AntagPrototype>()
            .Where(antag => antag.SetPreference && _requirements.IsAllowed(antag, Profile, out _))
            .ToArray();

        foreach (var antag in antags)
        {
            if (random.NextFloat(0f, 1f) < 0.35f)
                Profile = Profile.WithAntagPreference(antag.ID, true);
        }
    }

    /// <summary>
    /// Randomizes traits within their existing categories. WithTraitPreference enforces each category's point limit.
    /// </summary>
    private void RandomizeTraitPreferences(IRobustRandom random)
    {
        if (Profile is null)
            return;

        foreach (var trait in Profile.TraitPreferences.ToArray())
            Profile = Profile.WithoutTraitPreference(trait, _prototypeManager);

        var categories = _prototypeManager.EnumeratePrototypes<TraitPrototype>()
            .GroupBy(trait => trait.Category ?? TraitCategoryPrototype.Default);

        foreach (var category in categories)
        {
            foreach (var trait in category.OrderBy(_ => random.NextFloat(0f, 1f)))
            {
                if (random.NextFloat(0f, 1f) < 0.4f)
                    Profile = Profile.WithTraitPreference(trait.ID, _prototypeManager);
            }
        }
    }}
