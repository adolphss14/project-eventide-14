using Content.Server.Body.Systems;
using Content.Server.Chemistry.EntitySystems;
using Content.Server.Popups;
using Content.Shared.Body.Components;
using Content.Shared.Body.Systems;
using Content.Shared.DoAfter;
using Content.Shared.IdentityManagement;
using Content.Shared.Actions;
using Content.Shared.Mobs.Components;
using Content.Shared.Mobs;
using Robust.Shared.Player;
using System.Linq;
using WoundLickingActionEvent = Content.Shared._Genesis.WoundLicking.WoundLickingActionEvent;
using WoundLickingDoAfterEvent = Content.Shared._Genesis.WoundLicking.WoundLickingDoAfterEvent;

namespace Content.Server._Genesis.Felinid
{
    /// <summary>
    /// Allows felinids to lick wounds to reduce bleeding.
    /// </summary>
    public sealed partial class WoundLickingSystem : EntitySystem
    {
        [Dependency] private SharedDoAfterSystem _doAfterSystem = default!;
        [Dependency] private PopupSystem _popupSystem = default!;
        [Dependency] private SharedActionsSystem _actionsSystem = default!;
        [Dependency] private BloodstreamSystem _bloodstreamSystem = default!;
        [Dependency] private SolutionContainerSystem _solutionContainer = default!;

        public override void Initialize()
        {
            base.Initialize();
            SubscribeLocalEvent<WoundLickingComponent, ComponentInit>(OnInit);
            SubscribeLocalEvent<WoundLickingComponent, ComponentRemove>(OnRemove);
            SubscribeLocalEvent<WoundLickingComponent, WoundLickingDoAfterEvent>(OnDoAfter);
            SubscribeLocalEvent<WoundLickingActionEvent>(OnActionPerform);
        }

        private void OnInit(EntityUid uid, WoundLickingComponent comp, ComponentInit args)
        {
            _actionsSystem.AddAction(uid, ref comp.WoundLickingActionEntity, comp.WoundLickingAction);
        }

        private void OnRemove(EntityUid uid, WoundLickingComponent comp, ComponentRemove args)
        {
            _actionsSystem.RemoveAction(uid, comp.WoundLickingActionEntity);
        }

        private void OnActionPerform(WoundLickingActionEvent args)
        {
            if (args.Handled)
                return;

            args.Handled = true;
            var performer = args.Performer;
            var target = args.Target;

            // Ensure components
            if (
                !TryComp<WoundLickingComponent>(performer, out var woundLicking) ||
                !TryComp<BloodstreamComponent>(target, out var bloodstream) ||
                !TryComp<MobStateComponent>(target, out var mobState)
            )
                return;

            // Check target
            if (mobState.CurrentState == MobState.Dead)
                return;

            // Check "CanApplyOnSelf" field
            if (performer == target && !woundLicking.CanApplyOnSelf)
            {
                _popupSystem.PopupEntity(Loc.GetString("lick-wounds-yourself-impossible"),
                    performer, Filter.Entities(performer), true);
                return;
            }

            // Check "CanApplyOnOther" field
            if (performer != target && !woundLicking.CanApplyOnOther)
            {
                _popupSystem.PopupEntity(Loc.GetString("lick-wounds-other-impossible"),
                    performer, Filter.Entities(performer), true);
                return;
            }

            if (woundLicking.ReagentWhitelist.Any() &&
                (!_solutionContainer.ResolveSolution(target, bloodstream.BloodSolutionName, ref bloodstream.BloodSolution, out var bloodSolution)
                    || woundLicking.ReagentWhitelist.All(reagent => !bloodSolution.ContainsPrototype(reagent)))
            )
                return;

            // Check bloodstream
            if (bloodstream.BleedAmount == 0)
            {
                if (performer == target)
                {
                    _popupSystem.PopupEntity(Loc.GetString("lick-wounds-yourself-no-wounds"),
                        performer, performer);
                    return;
                }
                _popupSystem.PopupEntity(Loc.GetString("lick-wounds-performer-no-wounds", ("target", target)),
                    performer, performer);
                return;
            }

            // Popup


            if (target == performer)
            {
                // Applied on yourself
                var performerIdentity = Identity.Entity(performer, EntityManager);
                var otherFilter = Filter.Pvs(performer, entityManager: EntityManager)
                    .RemoveWhereAttachedEntity(e => e == performer);

                _popupSystem.PopupEntity(Loc.GetString("lick-wounds-yourself-begin"),
                performer, performer);
                _popupSystem.PopupEntity(Loc.GetString("lick-wounds-yourself-other-begin", ("performer", performerIdentity)),
                    performer, otherFilter, true);
            }
            else
            {
                // Applied on someone else
                var targetIdentity = Identity.Entity(target, EntityManager);
                var performerIdentity = Identity.Entity(performer, EntityManager);
                var otherFilter = Filter.Pvs(performer, entityManager: EntityManager)
                    .RemoveWhereAttachedEntity(e => e == performer || e == target);

                _popupSystem.PopupEntity(Loc.GetString("lick-wounds-performer-begin", ("target", targetIdentity)),
                performer, performer);
                _popupSystem.PopupEntity(Loc.GetString("lick-wounds-target-begin", ("performer", performerIdentity)),
                    target, target);
                _popupSystem.PopupEntity(Loc.GetString("lick-wounds-other-begin", ("performer", performerIdentity), ("target", targetIdentity)),
                    performer, otherFilter, true);
            }

            // DoAfter
            _doAfterSystem.TryStartDoAfter(new DoAfterArgs(EntityManager, performer, woundLicking.Delay, new WoundLickingDoAfterEvent(), performer, target: target)
            {
                BreakOnMove = true,
                BreakOnDamage = true
            });
        }

        private void OnDoAfter(EntityUid uid, WoundLickingComponent comp, WoundLickingDoAfterEvent args)
        {
            if (args.Cancelled || args.Handled)
            {
                return;
            }
            if (TryComp<BloodstreamComponent>(args.Args.Target, out var bloodstream))
                LickWound(uid, (args.Args.Target.Value, bloodstream), comp);
        }

        private void LickWound(EntityUid performer, Entity<BloodstreamComponent> target, WoundLickingComponent comp)
        {
            var bloodstream = target.Comp;
            var healed = Math.Min(bloodstream.BleedAmount, comp.MaxHeal);
            _bloodstreamSystem.TryModifyBleedAmount((target.Owner, bloodstream), -healed);

            if (performer == target.Owner)
            {
                // Applied on yourself
                var performerIdentity = Identity.Entity(performer, EntityManager);
                var otherFilter = Filter.Pvs(performer, entityManager: EntityManager)
                    .RemoveWhereAttachedEntity(e => e == performer);

                _popupSystem.PopupEntity(Loc.GetString("lick-wounds-yourself-success"),
                performer, performer);
                _popupSystem.PopupEntity(Loc.GetString("lick-wounds-yourself-other-success", ("performer", performerIdentity)),
                    performer, otherFilter, true);
            }
            else
            {
                // Applied on someone else
                var targetIdentity = Identity.Entity(target, EntityManager);
                var performerIdentity = Identity.Entity(performer, EntityManager);
                var otherFilter = Filter.Pvs(performer, entityManager: EntityManager)
                    .RemoveWhereAttachedEntity(e => e == performer || e == target.Owner);

                _popupSystem.PopupEntity(Loc.GetString("lick-wounds-performer-success", ("target", targetIdentity)),
                performer, performer);
                _popupSystem.PopupEntity(Loc.GetString("lick-wounds-target-success", ("performer", performerIdentity)),
                    target, target);
                _popupSystem.PopupEntity(Loc.GetString("lick-wounds-other-success", ("performer", performerIdentity), ("target", targetIdentity)),
                    performer, otherFilter, true);
            }
        }
    }
}
