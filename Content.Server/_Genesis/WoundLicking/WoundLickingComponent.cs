using System.Threading;
using Content.Shared.Actions.Components;
using Robust.Shared.Prototypes;

namespace Content.Server._Genesis.Felinid
{
    [RegisterComponent]
    [Access(typeof(WoundLickingSystem))]
    public sealed partial class WoundLickingComponent : Component
    {
        [DataField("woundLickingAction")]
        public EntProtoId<EntityTargetActionComponent> WoundLickingAction = "ActionWoundLicking";

        [DataField("woundLickingActionEntity")]
        public EntityUid? WoundLickingActionEntity;

        /// <summary>
        /// Max possible bleeding reduce. Human max bleeding is 20f, many weapons deals near 15f bleeding
        /// </summary>
        [DataField("maxHeal")]
        [ViewVariables(VVAccess.ReadWrite)]
        public float MaxHeal { get; set; } = 15f;

        /// <summary>
        /// How long it requires to lick wounds
        /// </summary>
        [DataField("delay")]
        [ViewVariables(VVAccess.ReadWrite)]
        public float Delay { get; set; } = 3f;

        /// <summary>
        /// If true, then wound-licking can be applied only on yourself
        /// </summary>
        [DataField("canApplyOnSelf")]
        [ViewVariables(VVAccess.ReadWrite)]
        public bool CanApplyOnSelf { get; set; } = true;

        /// <summary>
        /// If true, then wound-licking can be applied only on other entities
        /// </summary>
        [DataField("canApplyOnOther")]
        [ViewVariables(VVAccess.ReadWrite)]
        public bool CanApplyOnOther { get; set; } = false;


        /// <summary>
        /// If Target's bloodstream don't use one of these reagents, then ability can't be performed on it.
        /// </summary>
        [DataField("reagentWhitelist")]
        public List<String> ReagentWhitelist { get; set; } = new()
        {
            "Blood",
            "Slime"
        };

        /// <summary>
        ///     Token for interrupting a do-after action. If not null, implies component is
        ///     currently "in use".
        /// </summary>
        public CancellationTokenSource? CancelToken;
    }
}
