using System.Collections.Generic;
using System.Linq;
using CommonBehaviors.Actions;
using Singular.Dynamics;
using Singular.Helpers;
using Singular.Managers;
using Singular.Settings;
using Styx;
using Styx.Pathing;
using Styx.TreeSharp;
using Styx.WoWInternals;
using Styx.WoWInternals.WoWObjects;
using Styx.CommonBot;
using System;
using System.Drawing;
using Action = Styx.TreeSharp.Action;
using Rest = Singular.Helpers.Rest;

namespace Singular.ClassSpecific.DeathKnight
{
    public class Unholy
    {
        #region Properties

        private static DeathKnightSettings DeathKnightSettings
        {
            get { return SingularSettings.Instance.DeathKnight(); }
        }

        private static LocalPlayer Me
        {
            get { return StyxWoW.Me; }
        }

        private static int active_enemies
        {
            get
            {
                return active_enemies_list.Count;
            }
        }

        private static List<WoWUnit> active_enemies_list
        {
            get { return Common.scenario.Mobs.Where(x=> x.Distance < (TalentManager.HasGlyph("Blood Boil") ? 15 : 10)).ToList(); }
        }

        private static int blood
        {
            get { return Common.BloodRuneSlotsActive; }
        }

        private static int death
        {
            get { return Common.DeathRuneSlotsActive; }
        }

        private static int frost
        {
            get { return Common.FrostRuneSlotsActive; }
        }

        private static uint runic_power
        {
            get { return StyxWoW.Me.CurrentRunicPower; }
        }

        private static int unholy
        {
            get { return Common.UnholyRuneSlotsActive; }
        }

        #endregion

        #region Public Methods

        [Behavior(BehaviorType.Pull, WoWClass.DeathKnight, WoWSpec.DeathKnightUnholy, WoWContext.All, 99)]
        [Behavior(BehaviorType.Heal, WoWClass.DeathKnight, WoWSpec.DeathKnightUnholy, WoWContext.All, 99)]
        public static Composite CreateDeathKnightUnholyDiagnostic()
        {
            return CreateDeathKnightUnholyInstanceSimCCombat();
        }

        [Behavior(BehaviorType.Combat, WoWClass.DeathKnight, WoWSpec.DeathKnightUnholy, WoWContext.Instances)]
        public static Composite CreateDeathKnightUnholyInstanceSimCCombat()
        {
            Generic.SuppressGenericRacialBehavior = true;

            return new PrioritySelector(
                Helpers.Common.EnsureReadyToAttackFromMelee(),
                Spell.WaitForCastOrChannel(),
                new Decorator(
                    ret => !Spell.IsGlobalCooldown(),
                    new PrioritySelector(
                        SingularRoutine.MoveBehaviorInlineToCombat(BehaviorType.Heal),
                        SingularRoutine.MoveBehaviorInlineToCombat(BehaviorType.CombatBuffs),
                        new Action(r =>
                        {
                            Common.scenario.Update(StyxWoW.Me.CurrentTarget);
                            return RunStatus.Failure;
                        }),
                        Helpers.Common.CreateInterruptBehavior(),
                        Movement.WaitForFacing(),
                        Movement.WaitForLineOfSpellSight(),

                        // # Executed every time the actor is available.
                        //actions+=/run_action_list,name=aoe,if=(!talent.necrotic_plague.enabled&active_enemies>=2)|active_enemies>=4
                        new Decorator(
                            req => (!talent.necrotic_plague_enabled && active_enemies >= 2) || active_enemies >= 4,
                            CreateAoeBehavior()),
                        //actions+=/run_action_list,name=single_target,if=(!talent.necrotic_plague.enabled&active_enemies<2)|active_enemies<4
                        new Decorator(
                            req => (!talent.necrotic_plague_enabled && active_enemies < 2) || active_enemies < 4,
                            CreateSingleTargetBehavior()),
                        new ActionAlwaysFail()
                        )
                    )
                );
        }

        [Behavior(BehaviorType.Combat, WoWClass.DeathKnight, WoWSpec.DeathKnightUnholy, WoWContext.Normal)]
        public static Composite CreateDeathKnightUnholyNormalCombat()
        {
            return CreateDeathKnightUnholyInstanceSimCCombat();
        }

        [Behavior(BehaviorType.Combat, WoWClass.DeathKnight, WoWSpec.DeathKnightUnholy, WoWContext.Battlegrounds)]
        public static Composite CreateDeathKnightUnholyPvPCombat()
        {
            return CreateDeathKnightUnholyInstanceSimCCombat();
        }

        #endregion

        #region Private Methods

        private static Composite CreateAoeBehavior()
        {
            return new PrioritySelector(
                //actions.aoe=unholy_blight
                Spell.BuffSelfAndWait("Unholy Blight"),
                //actions.aoe+=/call_action_list,name=spread,if=!dot.blood_plague.ticking|!dot.frost_fever.ticking|(!dot.necrotic_plague.ticking&talent.necrotic_plague.enabled)
                new Decorator(
                    req =>
                        (!dot.blood_plague_ticking || !dot.frost_fever_ticking ||
                         (!dot.necrotic_plague_ticking && talent.necrotic_plague_enabled)),
                    CreateSpreadDiseaseBehavior()),
                //actions.aoe+=/defile
                Spell.CastOnGround("Defile", on => Me.CurrentTarget, req => Spell.UseAOE),
                //actions.aoe+=/breath_of_sindragosa,if=runic_power>75
                Spell.Buff("Breath of Sindragosa", req => runic_power > 75 && !Me.HasAura("Breath of Sindragosa")),
                //actions.aoe+=/run_action_list,name=bos_aoe,if=dot.breath_of_sindragosa.ticking
                //actions.aoe+=/blood_boil,if=blood=2|(frost=2&death=2)
                Spell.Cast("Blood Boil", req => Spell.UseAOE && (blood == 2 || (frost == 2 && death == 2))),
                //actions.aoe+=/summon_gargoyle
                Spell.Cast("Summon Gargoyle"),
                //actions.aoe+=/dark_transformation
                Spell.Buff("Dark Transformation", on => Me.Pet),
                //actions.aoe+=/blood_tap,if=level<=90&buff.shadow_infusion.stack=5
                Spell.Cast("Blood Tap", req => Me.Level <= 90 && buff.shadow_infusion_stack == 5),
                //actions.aoe+=/defile
                Spell.CastOnGround("Defile", on => Me.CurrentTarget, req => Spell.UseAOE),
                //actions.aoe+=/death_and_decay,if=unholy=1
                Spell.CastOnGround("Death and Decay", on => Me.CurrentTarget, req => Spell.UseAOE && unholy == 1),
                //actions.aoe+=/soul_reaper,if=target.health.pct-3*(target.health.pct%target.time_to_die)<=45
                Spell.Cast("Soul Reaper",
                    req => target.health_pct /*- 3 * (target.health_pct % target.time_to_die) <= 45*/<= 46),
                //actions.aoe+=/scourge_strike,if=unholy=2
                Spell.Cast("Scourge Strike", req => unholy == 2),
                //actions.aoe+=/blood_tap,if=buff.blood_charge.stack>10
                Spell.Cast("Blood Tap", req => buff.blood_charge_stack > 10),
                //actions.aoe+=/death_coil,if=runic_power>90|buff.sudden_doom.react|(buff.dark_transformation.down&unholy<=1)
                Spell.Cast("Death Coil",
                    req => runic_power > 90 || buff.sudden_doom_react || (buff.dark_transformation_down && unholy <= 1)),
                //actions.aoe+=/blood_boil
                Spell.Cast("Blood Boil", req => Spell.UseAOE),
                //actions.aoe+=/icy_touch
                Spell.Buff("Icy Touch"),
                //actions.aoe+=/scourge_strike,if=unholy=1
                Spell.Cast("Scourge Strike", req => unholy == 1),
                //actions.aoe+=/death_coil
                Spell.Cast("Death Coil"),
                //actions.aoe+=/blood_tap
                Spell.Cast("Blood Tap"),
                //actions.aoe+=/plague_leech
                Spell.Cast("Plague Leech"),
                //actions.aoe+=/empower_rune_weapon
                Spell.Cast("Empower Rune Weapon"),
                new ActionAlwaysFail()
                );
        }

        private static Composite CreateSingleTargetBehavior()
        {
            return new PrioritySelector(
                //actions.single_target=plague_leech,if=(cooldown.outbreak.remains<1)&((blood<1&frost<1)|(blood<1&unholy<1)|(frost<1&unholy<1))
                Spell.Cast("Plague Leech",
                    req =>
                        (cooldown.outbreak_remains < 1) &&
                        ((blood < 1 && frost < 1) || (blood < 1 && unholy < 1) || (frost < 1 && unholy < 1))),
                //actions.single_target+=/plague_leech,if=((blood<1&frost<1)|(blood<1&unholy<1)|(frost<1&unholy<1))&disease.min_remains<3
                Spell.Cast("Plague Leech",
                    req =>
                        ((blood < 1 && frost < 1) || (blood < 1 && unholy < 1) || (frost < 1 && unholy < 1)) &&
                        disease.min_remains < 3),
                //actions.single_target+=/plague_leech,if=disease.min_remains<1
                Spell.Cast("Plague Leech", req => disease.min_remains < 1),
                //actions.single_target+=/outbreak,if=!disease.min_ticking
                Spell.Cast("Outbreak", req => !disease.min_ticking),
                //actions.single_target+=/unholy_blight,if=!talent.necrotic_plague.enabled&disease.min_remains<3
                Spell.BuffSelfAndWait("Unholy Blight", req => !talent.necrotic_plague_enabled && disease.min_remains < 3),
                //actions.single_target+=/unholy_blight,if=talent.necrotic_plague.enabled&dot.necrotic_plague.remains<1
                Spell.BuffSelfAndWait("Unholy Blight",
                    req => talent.necrotic_plague_enabled && dot.necrotic_plague_remains < 1),
                //actions.single_target+=/death_coil,if=runic_power>90
                Spell.Cast("Death Coil", req => runic_power > 90),
                //actions.single_target+=/soul_reaper,if=(target.health.pct-3*(target.health.pct%target.time_to_die))<=45
                Spell.Cast("Soul Reaper",
                    req => target.health_pct /*- 3 * (target.health_pct % target.time_to_die) <= 45*/<= 46),
                //actions.single_target+=/breath_of_sindragosa,if=runic_power>75
                Spell.Buff("Breath of Sindragosa", req => runic_power > 75 && !Me.HasAura("Breath of Sindragosa")),
                //actions.single_target+=/run_action_list,name=bos_st,if=dot.breath_of_sindragosa.ticking
                //actions.single_target+=/death_and_decay,if=cooldown.breath_of_sindragosa.remains<7&runic_power<88&talent.breath_of_sindragosa.enabled
                Spell.CastOnGround("Death and Decay", on => Me.CurrentTarget,
                    req =>
                        Spell.UseAOE && cooldown.breath_of_sindragosa_remains < 7 && runic_power < 88 &&
                        talent.breath_of_sindragosa_enabled),
                //actions.single_target+=/scourge_strike,if=cooldown.breath_of_sindragosa.remains<7&runic_power<88&talent.breath_of_sindragosa.enabled
                Spell.Cast("Scourge Strike",
                    req =>
                        cooldown.breath_of_sindragosa_remains < 7 && runic_power < 88 &&
                        talent.breath_of_sindragosa_enabled),
                //actions.single_target+=/festering_strike,if=cooldown.breath_of_sindragosa.remains<7&runic_power<76&talent.breath_of_sindragosa.enabled
                Spell.Cast("Festering Strike",
                    req =>
                        cooldown.breath_of_sindragosa_remains < 7 && runic_power < 76 &&
                        talent.breath_of_sindragosa_enabled),
                //actions.single_target+=/blood_tap,if=((target.health.pct-3*(target.health.pct%target.time_to_die))<=45)&cooldown.soul_reaper.remains=0
                Spell.Cast("Blood Tap",
                    req =>
                        (target.health_pct /*- 3 * (target.health_pct % target.time_to_die) <= 45*/<= 46) &&
                        cooldown.soul_reaper_remains == 0),
                //actions.single_target+=/death_and_decay,if=unholy=2
                Spell.CastOnGround("Death and Decay", on => Me.CurrentTarget, req => Spell.UseAOE && unholy == 2),
                //actions.single_target+=/defile,if=unholy=2
                Spell.CastOnGround("Defile", on => Me.CurrentTarget, req => Spell.UseAOE && unholy == 2),
                //actions.single_target+=/plague_strike,if=!disease.min_ticking&unholy=2
                Spell.Buff("Plague Strike", req => !disease.min_ticking && unholy == 2),
                //actions.single_target+=/scourge_strike,if=unholy=2
                Spell.Cast("Scourge Strike", req => unholy == 2),
                //actions.single_target+=/death_coil,if=runic_power>80
                Spell.Cast("Death Coil", req => runic_power > 80),
                //actions.single_target+=/festering_strike,if=talent.necrotic_plague.enabled&talent.unholy_blight.enabled&dot.necrotic_plague.remains<cooldown.unholy_blight.remains%2
                Spell.Cast("Festering Strike",
                    req =>
                        talent.necrotic_plague_enabled && talent.unholy_blight_enabled &&
                        dot.necrotic_plague_remains < cooldown.unholy_blight_remains % 2),
                //actions.single_target+=/festering_strike,if=blood=2&frost=2&(((Frost-death)>0)|((Blood-death)>0))
                Spell.Cast("Festering Strike",
                    req => blood == 2 && frost == 2 && (((frost - death) > 0) || ((blood - death) > 0))),
                //actions.single_target+=/festering_strike,if=(blood=2|frost=2)&(((Frost-death)>0)&((Blood-death)>0))
                Spell.Cast("Festering Strike",
                    req => (blood == 2 || frost == 2) && (((frost - death) > 0) && ((blood - death) > 0))),
                //actions.single_target+=/defile,if=blood=2|frost=2
                Spell.CastOnGround("Defile", on => Me.CurrentTarget, req => Spell.UseAOE && (blood == 2 || frost == 2)),
                //actions.single_target+=/plague_strike,if=!disease.min_ticking&(blood=2|frost=2)
                Spell.Buff("Plague Strike", req => !disease.min_ticking && (blood == 2 || frost == 2)),
                //actions.single_target+=/scourge_strike,if=blood=2|frost=2
                Spell.Cast("Scourge Strike", req => blood == 2 || frost == 2),
                //actions.single_target+=/festering_strike,if=((Blood-death)>1)
                Spell.Cast("Festering Strike", req => ((blood - death) > 1)),
                //actions.single_target+=/blood_boil,if=((Blood-death)>1)
                Spell.Cast("Blood Boil", req => Spell.UseAOE && ((blood - death) > 1)),
                //actions.single_target+=/festering_strike,if=((Frost-death)>1)
                Spell.Cast("Festering Strike", req => ((frost - death) > 1)),
                //actions.single_target+=/blood_tap,if=((target.health.pct-3*(target.health.pct%target.time_to_die))<=45)&cooldown.soul_reaper.remains=0
                Spell.Cast("Blood Tap",
                    req =>
                        (target.health_pct /*- 3 * (target.health_pct % target.time_to_die)) <= 45*/<= 46) &&
                        cooldown.soul_reaper_remains == 0),
                //actions.single_target+=/summon_gargoyle
                Spell.Cast("Summon Gargoyle"),
                //actions.single_target+=/death_and_decay
                Spell.CastOnGround("Death and Decay", on => Me.CurrentTarget, req => Spell.UseAOE),
                //actions.single_target+=/defile
                Spell.CastOnGround("Defile", on => Me.CurrentTarget, req => Spell.UseAOE),
                //actions.single_target+=/blood_tap,if=cooldown.defile.remains=0
                Spell.Cast("Blood Tap", req => cooldown.defile_remains == 0),
                //actions.single_target+=/plague_strike,if=!disease.min_ticking
                Spell.Buff("Plague Strike", req => !disease.ticking),
                //actions.single_target+=/dark_transformation
                Spell.Buff("Dark Transformation", on => Me.Pet),
                //actions.single_target+=/blood_tap,if=buff.blood_charge.stack>10&(buff.sudden_doom.react|(buff.dark_transformation.down&unholy<=1))
                Spell.Cast("Blood Tap",
                    req =>
                        buff.blood_charge_stack > 10 &&
                        (buff.sudden_doom_react || (buff.dark_transformation_down && unholy <= 1))),
                //actions.single_target+=/death_coil,if=buff.sudden_doom.react|(buff.dark_transformation.down&unholy<=1)
                Spell.Cast("Death Coil", req => buff.sudden_doom_react || (buff.dark_transformation_down && unholy <= 1)),
                //actions.single_target+=/scourge_strike,if=!((target.health.pct-3*(target.health.pct%target.time_to_die))<=45)|(Unholy>=2)
                Spell.Cast("Scourge Strike",
                    req =>
                        !(target.health_pct /*- 3 * (target.health_pct % target.time_to_die)) <= 45*/<= 46) ||
                        (unholy >= 2)),
                //actions.single_target+=/blood_tap
                Spell.Cast("Blood Tap"),
                //actions.single_target+=/festering_strike,if=!((target.health.pct-3*(target.health.pct%target.time_to_die))<=45)|(((Frost-death)>0)&((Blood-death)>0))
                Spell.Cast("Festering Strike",
                    req =>
                        !(target.health_pct /*- 3 * (target.health_pct % target.time_to_die)) <= 45*/<= 46) ||
                        (((frost - death) > 0) && ((blood - death) > 0))),
                //actions.single_target+=/death_coil
                Spell.Cast("Death Coil"),
                //actions.single_target+=/plague_leech
                Spell.Cast("Plague Leech"),
                //actions.single_target+=/scourge_strike,if=cooldown.empower_rune_weapon.remains=0
                Spell.Cast("Scourge Strike", req => cooldown.empower_rune_weapon_remains == 0),
                //actions.single_target+=/festering_strike,if=cooldown.empower_rune_weapon.remains=0
                Spell.Cast("Festering Strike", req => cooldown.empower_rune_weapon_remains == 0),
                //actions.single_target+=/blood_boil,if=cooldown.empower_rune_weapon.remains=0
                Spell.Cast("Blood Boil", req => Spell.UseAOE && cooldown.empower_rune_weapon_remains == 0),
                //actions.single_target+=/icy_touch,if=cooldown.empower_rune_weapon.remains=0
                Spell.Buff("Icy Touch", req => cooldown.empower_rune_weapon_remains == 0),
                //actions.single_target+=/empower_rune_weapon,if=blood<1&unholy<1&frost<1
                Spell.Cast("Empower Rune Weapon", req => blood < 1 && unholy < 1 && frost < 1),
                new ActionAlwaysFail()
                );
        }

        private static Composite CreateSpreadDiseaseBehavior()
        {
            return new PrioritySelector(
                //actions.spread=blood_boil,cycle_targets=1,if=!disease.min_ticking
                Spell.Cast("Blood Boil", req => Spell.UseAOE && disease.min_ticking && active_enemies_list.Count(u => !disease.ticking_on(u)) > 0),
                //actions.spread+=/outbreak,if=!disease.min_ticking
                Spell.Buff("Outbreak", req => !disease.min_ticking),
                //actions.spread+=/plague_strike,if=!disease.min_ticking
                Spell.Buff("Plague Strike", req => !disease.min_ticking),
                new ActionAlwaysFail()
                );
        }

        #endregion
    }
}