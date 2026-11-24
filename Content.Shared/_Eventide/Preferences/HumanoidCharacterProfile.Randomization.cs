using System;
using System.Collections.Generic;
using System.Linq;
using Content.Shared.Chat.Prototypes;
using Content.Shared.Corvax.TTS;
using Content.Shared.Humanoid;
using Content.Shared.Humanoid.Prototypes;
using Robust.Shared;
using Robust.Shared.Enums;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;

namespace Content.Shared.Preferences;

public sealed partial class HumanoidCharacterProfile
{
        /// <summary>
        /// An enum defining randomizable values in character editor.
        /// </summary>
        [Flags]
        public enum RandomizeCfg
        {
            // profile
            None = 0,
            Name = 1 << 0,
            Species = 1 << 1,
            Age = 1 << 2,
            Sex = 1 << 3,
            Gender = 1 << 4,
            // appearance
            Eyes = 1 << 5,
            Skin = 1 << 6,
            Markings = 1 << 7,
            Hair = 1 << 8,
            FacialHair = 1 << 9,
            HeadTop = 1 << 10,
            Tail = 1 << 11,
            BodyMarkings = 1 << 12,
            FirstName = 1 << 13,
            LastName = 1 << 14,
            HairColor = 1 << 15,
            FacialHairColor = 1 << 16,
            HeadTopColor = 1 << 17,
            TailColor = 1 << 18,
            BodyMarkingsColor = 1 << 19,
            // preferences
            Jobs = 1 << 20,
            Antags = 1 << 21,
            Traits = 1 << 22,
            Loadouts = 1 << 23,
            // Distinguishes explicit first/last-name toggles from legacy callers that only set Name.
            NameParts = 1 << 24,
            Voice = 1 << 25,
            TTSVoice = 1 << 26,
        }

        /// <summary>
        /// A randomize config that covers all possible values (including appearance).
        /// </summary>
        public const RandomizeCfg RandomizeConfigAll =
            RandomizeCfg.Name
            | RandomizeCfg.Species
            | RandomizeCfg.Age
            | RandomizeCfg.Sex
            | RandomizeCfg.Gender
            | RandomizeCfg.Eyes
            | RandomizeCfg.Skin
            | RandomizeCfg.Markings
            | RandomizeCfg.Hair
            | RandomizeCfg.FacialHair
            | RandomizeCfg.HeadTop
            | RandomizeCfg.Tail
            | RandomizeCfg.BodyMarkings
            | RandomizeCfg.FirstName
            | RandomizeCfg.LastName
            | RandomizeCfg.HairColor
            | RandomizeCfg.FacialHairColor
            | RandomizeCfg.HeadTopColor
            | RandomizeCfg.TailColor
            | RandomizeCfg.BodyMarkingsColor
            | RandomizeCfg.Jobs
            | RandomizeCfg.Antags
            | RandomizeCfg.Traits
            | RandomizeCfg.Loadouts
            | RandomizeCfg.NameParts
            | RandomizeCfg.Voice
            | RandomizeCfg.TTSVoice;

        /// <summary>
        /// Generates a randomized character profile with selective randomizing.
        /// </summary>
        /// <param name="randomizeCfg">Which values to randomize.</param>
        /// <param name="baseProfile">Profile to base the new profile on. Values that are not randomized will be taken from this profile.</param>
        /// <returns>A new character profile with selected values randomized</returns>
        public static HumanoidCharacterProfile Random(RandomizeCfg randomizeCfg, HumanoidCharacterProfile baseProfile, MarkingRandomizationConfig? markingRandomizationConfig = null)
        {
            var prototypeManager = IoCManager.Resolve<IPrototypeManager>();
            var random = IoCManager.Resolve<IRobustRandom>();

            // Start from the existing profile so job, antagonist, trait and loadout preferences survive appearance randomization.
            var profile = new HumanoidCharacterProfile(baseProfile);
            if ((randomizeCfg & RandomizeCfg.Species) != 0)
            {
                profile.Species = RandomSpecies();
            }
            else
            {
                profile.Species = DefaultSpecies;
                if (prototypeManager.HasIndex(baseProfile.Species))
                {
                    profile.Species = baseProfile.Species;
                }
            }
            var speciesProto = prototypeManager.Index(profile.Species);

            profile.Sex = (randomizeCfg & RandomizeCfg.Sex) != 0 ? RandomSex(speciesProto) : baseProfile.Sex;
            var defaultVoice = speciesProto.DefaultSoundsBySex[(int)profile.Sex];
            profile.Voice = (randomizeCfg & RandomizeCfg.Voice) != 0 && speciesProto.Voices.Count > 0
                ? random.Pick(speciesProto.Voices)
                : speciesProto.Voices.Contains(baseProfile.Voice) ? baseProfile.Voice : defaultVoice;
            profile.Gender = (randomizeCfg & RandomizeCfg.Gender) != 0 ? RandomGender(profile.Sex) : baseProfile.Gender;
            if ((randomizeCfg & RandomizeCfg.Name) != 0)
            {
                var hasNamePartConfig = (randomizeCfg & RandomizeCfg.NameParts) != 0;
                var randomizeFirst = !hasNamePartConfig || (randomizeCfg & RandomizeCfg.FirstName) != 0;
                var randomizeLast = !hasNamePartConfig || (randomizeCfg & RandomizeCfg.LastName) != 0;

                if (randomizeFirst || randomizeLast)
                {
                    var generatedName = SplitName(RandomName(speciesProto, profile.Gender));
                    var existingName = SplitName(baseProfile.Name);
                    var first = randomizeFirst ? generatedName.First : existingName.First;
                    var last = randomizeLast ? generatedName.Last : existingName.Last;
                    profile.Name = string.Join(" ", new[] { first, last }.Where(part => !string.IsNullOrWhiteSpace(part)));
                }
                else
                {
                    // Both name parts are individually locked.
                    profile.Name = baseProfile.Name;
                }
            }
            else
            {
                profile.Name = baseProfile.Name;
            }
            profile.Age = (randomizeCfg & RandomizeCfg.Age) != 0 ? RandomAge(speciesProto) : baseProfile.Age;
            if ((randomizeCfg & RandomizeCfg.TTSVoice) != 0)
            {
                profile.TTSVoice = RandomTTS(profile.Voice, profile.Sex);
            }
            else if (prototypeManager.TryIndex<TTSVoicePrototype>(baseProfile.TTSVoice, out var existingTtsVoice) &&
                     CanHaveVoice(existingTtsVoice, profile.Voice))
            {
                profile.TTSVoice = baseProfile.TTSVoice;
            }
            else
            {
                profile.TTSVoice = HumanoidProfileSystem.DefaultSexVoice[profile.Sex];
            }

            profile.Appearance = HumanoidCharacterAppearance.Random(speciesProto, profile.Sex, randomizeCfg, baseProfile.Appearance, markingRandomizationConfig);

            return profile;
        }

        private static (string First, string Last) SplitName(string name)
        {
            var separator = name.IndexOf(' ');
            return separator < 0
                ? (name, string.Empty)
                : (name[..separator], name[(separator + 1)..].Trim());
        }

        /// <summary>
        /// Randomizes only the selected part(s) of the character's name. The full randomizer
        /// continues to use RandomizeCfg.Name and therefore still generates both parts.
        /// </summary>
        public HumanoidCharacterProfile RandomizeName(bool firstName = true, bool lastName = true)
        {
            if (!firstName && !lastName)
                return new HumanoidCharacterProfile(this);

            var prototypeManager = IoCManager.Resolve<IPrototypeManager>();
            var species = prototypeManager.Index(Species);
            var generatedName = SplitName(RandomName(species, Gender));
            var existingName = SplitName(Name);
            var first = firstName ? generatedName.First : existingName.First;
            var last = lastName ? generatedName.Last : existingName.Last;

            return WithName(string.Join(" ", new[] { first, last }.Where(part => !string.IsNullOrWhiteSpace(part))));
        }

}
