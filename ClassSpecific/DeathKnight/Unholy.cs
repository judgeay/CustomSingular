using System.Collections.Generic;
using System.Linq;
using CommonBehaviors.Actions;
using Singular.Dynamics;
using Singular.Helpers;
using Singular.Managers;
using Singular.Settings;
using Styx;
using Styx.TreeSharp;
using Styx.WoWInternals.WoWObjects;
using Action = Styx.TreeSharp.Action;

namespace Singular.ClassSpecific.DeathKnight
{
    public class Unholy
    {
        #region Fields

        private const string blood_boil = "Blood Boil";
        //private const string blood_charge = "Blood Charge";
        //private const string blood_plague = "Blood Plague";
        private const string blood_tap = "Blood Tap";
        private const string breath_of_sindragosa = "Breath of Sindragosa";
        private const string dark_transformation = "Dark Transformation";
        private const string death_and_decay = "Death and Decay";
        private const string death_coil = "Death Coil";
        private const string defile = "Defile";
        private const string empower_rune_weapon = "Empower Rune Weapon";
        private const string festering_strike = "Festering Strike";
        //private const string frost_fever = "Frost Fever";
        private const string icy_touch = "Icy Touch";
        //private const string necrotic_plague = "Necrotic Plague";
        private const string outbreak = "Outbreak";
        private const string plague_leech = "Plague Leech";
        private const string plague_strike = "Plague Strike";
        private const string raise_dead = "Raise Dead";
        private const string scourge_strike = "Scourge Strike";
        //private const string shadow_infusion = "Shadow Infusion";
        private const string soul_reaper = "Soul Reaper";
        //private const int sudden_doom = 81340;
        private const string summon_gargoyle = "Summon Gargoyle";
        private const string unholy_blight = "Unholy Blight";

        #endregion

        #region Properties

        private static DeathKnightSettings DeathKnightSettings
        {
            get { return SingularSettings.Instance.DeathKnight(); }
        }

        private static LocalPlayer Me
        {
            get { return StyxWoW.Me; }
        }

        private static decimal active_enemies
        {
            get { return Common.scenario.Mobs.Count(x => x.Distance < (TalentManager.HasGlyph(blood_boil) ? 15 : 10)); }
        }

        private static IEnumerable<WoWUnit> active_enemies_list
        {
            get { return Common.scenario.Mobs; }
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

        [Behavior(BehaviorType.Combat | BehaviorType.Pull, WoWClass.DeathKnight, WoWSpec.DeathKnightUnholy, WoWContext.Normal)]
        [Behavior(BehaviorType.Combat | BehaviorType.Pull, WoWClass.DeathKnight, WoWSpec.DeathKnightUnholy, WoWContext.Battlegrounds)]
        [Behavior(BehaviorType.Combat | BehaviorType.Pull, WoWClass.DeathKnight, WoWSpec.DeathKnightUnholy, WoWContext.Instances)]
        public static Composite UnholyActionList()
        {
            Generic.SuppressGenericRacialBehavior = true;

            return new PrioritySelector(
                Spell.Buff(raise_dead, re => !StyxWoW.Me.GotAlivePet),
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
                        // 
                        new PrioritySelector(
                            // actions=auto_attack
                            // ... handled by Ensure
                            // actions+=/deaths_advance,if=movement.remains>2
                            // actions+=/antimagic_shell,damage=100000
                            // actions+=/blood_fury
                            //Spell.BuffSelf("Blood Fury"),
                            // actions+=/berserking
                            //Spell.BuffSelf("Berserking"),
                            // actions+=/arcane_torrent
                            //Spell.BuffSelf("Arcane Torrent"),
                            // actions+=/potion,name=draenic_strength,if=buff.dark_transformation.up&target.time_to_die<=60
                            // actions+=/run_action_list,name=aoe,if=active_enemies>=2
                            new Decorator(req => active_enemies >= 2, aoe()),
                            // actions+=/run_action_list,name=single_target,if=active_enemies<2
                            new Decorator(req => active_enemies < 2, single_target()),
                            new ActionAlwaysFail()
                            ),
                        new ActionAlwaysFail()
                        )
                    )
                );
        }

        #endregion

        #region Private Methods

        private static Composite aoe()
        {
            return new PrioritySelector(
                // actions.aoe=unholy_blight
                Spell.BuffSelfAndWait(unholy_blight),
                //actions.aoe+=/call_action_list,name=spread,if=!dot.blood_plague.ticking|!dot.frost_fever.ticking|(!dot.necrotic_plague.ticking&talent.necrotic_plague.enabled)
                new Decorator(req => !dot.blood_plague_ticking || !dot.frost_fever_ticking || (!dot.necrotic_plague_ticking && talent.necrotic_plague_enabled), spread()),
                // actions.aoe+=/defile
                Spell.CastOnGround(defile, on => Me.CurrentTarget, req => Spell.UseAOE),
                // actions.aoe+=/breath_of_sindragosa,if=runic_power>75
                Spell.Buff(breath_of_sindragosa, req => runic_power > 75 && !Me.HasAura(breath_of_sindragosa)),
                // actions.aoe+=/run_action_list,name=bos_aoe,if=dot.breath_of_sindragosa.ticking
                new Decorator(req => dot.breath_of_sindragosa_ticking, bos_aoe()),
                //actions.aoe+=/blood_boil,if=blood=2|(frost=2&death=2)
                Spell.Cast(blood_boil, req => Spell.UseAOE && (blood == 2 || (frost == 2 && death == 2))),
                // actions.aoe+=/summon_gargoyle
                Spell.Cast(summon_gargoyle),
                // actions.aoe+=/dark_transformation
                Spell.Buff(dark_transformation, on => Me.Pet),
                // actions.aoe+=/blood_tap,if=level<=90&&buff.shadow_infusion.stack==5
                Spell.Cast(blood_tap, req => Me.Level <= 90 && buff.shadow_infusion_stack == 5),
                // actions.aoe+=/defile
                Spell.CastOnGround(defile, on => Me.CurrentTarget, req => Spell.UseAOE),
                // actions.aoe+=/death_and_decay,if=unholy==1
                Spell.CastOnGround(death_and_decay, on => Me.CurrentTarget, req => Spell.UseAOE && unholy == 1),
                // actions.aoe+=/soul_reaper,if=target.health.pct-3*(target.health.pct%target.time_to_die)<=45
                Spell.Cast(soul_reaper, req => target.health_pct <= 46),
                // actions.aoe+=/scourge_strike,if=unholy==2
                Spell.Cast(scourge_strike, req => unholy == 2),
                // actions.aoe+=/blood_tap,if=buff.blood_charge.stack>10
                Spell.Cast(blood_tap, req => buff.blood_charge_stack > 10),
                // actions.aoe+=/death_coil,if=runic_power>90||buff.sudden_doom_react||(buff.dark_transformation_down&&unholy<=1)
                Spell.Cast(death_coil, req => runic_power > 90 || buff.sudden_doom_react || (buff.dark_transformation_down && unholy <= 1)),
                // actions.aoe+=/blood_boil
                Spell.Cast(blood_boil, req => Spell.UseAOE),
                // actions.aoe+=/icy_touch
                Spell.Cast(icy_touch),
                // actions.aoe+=/scourge_strike,if=unholy==1
                Spell.Cast(scourge_strike, req => unholy == 1),
                // actions.aoe+=/death_coil
                Spell.Cast(death_coil),
                // actions.aoe+=/blood_tap
                Spell.Cast(blood_tap),
                // actions.aoe+=/plague_leech
                Spell.Cast(plague_leech /*, req => disease.min_ticking*/),
                // actions.aoe+=/empower_rune_weapon
                Spell.Cast(empower_rune_weapon),
                new ActionAlwaysFail()
                );
        }

        private static Composite bos_aoe()
        {
            return new PrioritySelector(
                // actions.bos_aoe=death_and_decay,if=runic_power<88
                Spell.CastOnGround(death_and_decay, on => Me.CurrentTarget, req => Spell.UseAOE && runic_power < 88),
                // actions.bos_aoe+=/blood_boil,if=runic_power<88
                Spell.Cast(blood_boil, req => Spell.UseAOE && runic_power < 88),
                // actions.bos_aoe+=/scourge_strike,if=runic_power<88&&unholy==1
                Spell.Cast(scourge_strike, req => runic_power < 88 && unholy == 1),
                // actions.bos_aoe+=/icy_touch,if=runic_power<88
                Spell.Cast(icy_touch, req => runic_power < 88),
                // actions.bos_aoe+=/blood_tap,if=buff.blood_charge.stack>=5
                Spell.Cast(blood_tap, req => buff.blood_charge_stack >= 5),
                // actions.bos_aoe+=/plague_leech
                Spell.Cast(plague_leech /*, req => disease.min_ticking*/),
                // actions.bos_aoe+=/empower_rune_weapon
                Spell.Cast(empower_rune_weapon),
                // actions.bos_aoe+=/death_coil,if=buff.sudden_doom_react
                Spell.Cast(death_coil, req => buff.sudden_doom_react),
                new ActionAlwaysFail()
                );
        }

        private static Composite bos_st()
        {
            return new PrioritySelector(
                // 
                // actions.bos_st=death_and_decay,if=runic_power<88
                Spell.CastOnGround(death_and_decay, on => Me.CurrentTarget, req => Spell.UseAOE && runic_power < 88),
                // actions.bos_st+=/festering_strike,if=runic_power<77
                Spell.Cast(festering_strike, req => Spell.UseAOE && runic_power < 77),
                // actions.bos_st+=/scourge_strike,if=runic_power<88
                Spell.Cast(scourge_strike, req => runic_power < 88),
                // actions.bos_st+=/blood_tap,if=buff.blood_charge.stack>=5
                Spell.Cast(blood_tap, req => buff.blood_charge_stack >= 5),
                // actions.bos_st+=/plague_leech
                Spell.Cast(plague_leech /*, req => disease.min_ticking*/),
                // actions.bos_aoe+=/empower_rune_weapon
                Spell.Cast(empower_rune_weapon),
                // actions.bos_aoe+=/death_coil,if=buff.sudden_doom_react
                Spell.Cast(death_coil, req => buff.sudden_doom_react),
                new ActionAlwaysFail()
                );
        }

        private static Composite single_target()
        {
            return new PrioritySelector(
                // actions.single_target=plague_leech,if=(cooldown.outbreak.remains<1)&((blood<1&frost<1)|(blood<1&unholy<1)|(frost<1&unholy<1))
                Spell.Cast(plague_leech, req => /*disease.min_ticking &&*/ (cooldown.outbreak_remains < 1) && ((blood < 1 && frost < 1) || (blood < 1 && unholy < 1) || (frost < 1 && unholy < 1))),
                // actions.single_target+=/plague_leech,if=((blood<1&frost<1)|(blood<1&unholy<1)|(frost<1&unholy<1))&disease.min_remains<3
                Spell.Cast(plague_leech, req => /*disease.min_ticking &&*/ ((blood < 1 && frost < 1) || (blood < 1 && unholy < 1) || (frost < 1 && unholy < 1)) && disease.min_remains < 3),
                // actions.single_target+=/plague_leech,if=disease.min_remains<1
                Spell.Cast(plague_leech, req => /*disease.min_ticking &&*/ disease.min_remains < 1),
                //actions.single_target+=/outbreak,if=!disease.min_ticking
                Spell.Cast(outbreak, req => !disease.min_ticking),
                // actions.single_target+=/unholy_blight,if=!talent.necrotic_plague.enabled&disease.min_remains<3unholy_blight
                Spell.BuffSelfAndWait(unholy_blight, req => !talent.necrotic_plague_enabled && disease.min_remains < 3),
                // actions.single_target+=/unholy_blight,if=talent.necrotic_plague_enabled&&dot.necrotic_plague_remains<1
                Spell.BuffSelfAndWait(unholy_blight, req => talent.necrotic_plague_enabled && dot.necrotic_plague_remains < 1),
                // actions.single_target+=/death_coil,if=runic_power>90
                Spell.Cast(death_coil, req => runic_power > 90),
                // actions.single_target+=/soul_reaper,if=(target.health_pct-3*(target.health_pct%target.time_to_die))<=45
                Spell.Cast(soul_reaper, req => target.health_pct <= 46),
                // actions.single_target+=/breath_of_sindragosa,if=runic_power>75
                Spell.Buff(breath_of_sindragosa, req => runic_power > 75 && !Me.HasAura(breath_of_sindragosa)),
                // actions.single_target+=/run_action_list,name=bos_st,if=dot.breath_of_sindragosa.ticking
                new Decorator(req => dot.breath_of_sindragosa_ticking, bos_st()),
                // actions.single_target+=/death_and_decay,if=cooldown.breath_of_sindragosa_remains<7&&runic_power<88&&talent.breath_of_sindragosa_enabled
                Spell.CastOnGround(death_and_decay, on => Me.CurrentTarget, req => Spell.UseAOE && cooldown.breath_of_sindragosa_remains < 7 && runic_power < 88 && talent.breath_of_sindragosa_enabled),
                // actions.single_target+=/scourge_strike,if=cooldown.breath_of_sindragosa_remains<7&&runic_power<88&&talent.breath_of_sindragosa_enabled
                Spell.Cast(scourge_strike, req => cooldown.breath_of_sindragosa_remains < 7 && runic_power < 88 && talent.breath_of_sindragosa_enabled),
                // actions.single_target+=/festering_strike,if=cooldown.breath_of_sindragosa_remains<7&&runic_power<76&&talent.breath_of_sindragosa_enabled
                Spell.Cast(festering_strike, req => cooldown.breath_of_sindragosa_remains < 7 && runic_power < 76 && talent.breath_of_sindragosa_enabled),
                // actions.single_target+=/blood_tap,if=((target.health_pct-3*(target.health_pct%target.time_to_die))<=45)&&cooldown.soul_reaper_remains==0
                Spell.Cast(blood_tap, req => (target.health_pct <= 46) && cooldown.soul_reaper_remains == 0),
                // actions.single_target+=/death_and_decay,if=unholy==2
                Spell.CastOnGround(death_and_decay, on => Me.CurrentTarget, req => Spell.UseAOE && unholy == 2),
                // actions.single_target+=/defile,if=unholy==2
                Spell.CastOnGround(defile, on => Me.CurrentTarget, req => Spell.UseAOE && unholy == 2),
                // actions.single_target+=/plague_strike,if=!disease.min_ticking&&unholy==2
                Spell.Cast(plague_strike, req => !disease.min_ticking && unholy == 2),
                // actions.single_target+=/scourge_strike,if=unholy==2
                Spell.Cast(scourge_strike, req => unholy == 2),
                // actions.single_target+=/death_coil,if=runic_power>80
                Spell.Cast(death_coil, req => runic_power > 80),
                //actions.single_target+=/festering_strike,if=talent.necrotic_plague.enabled&talent.unholy_blight.enabled&dot.necrotic_plague.remains<cooldown.unholy_blight.remains%2
                Spell.Cast(festering_strike, req => talent.necrotic_plague_enabled && talent.unholy_blight_enabled && dot.necrotic_plague_remains < cooldown.unholy_blight_remains%2),
                //actions.single_target+=/festering_strike,if=blood=2&frost=2&(((Frost-death)>0)|((Blood-death)>0))
                Spell.Cast(festering_strike, req => blood == 2 && frost == 2 && (((frost - death) > 0) || ((blood - death) > 0))),
                //actions.single_target+=/festering_strike,if=(blood=2|frost=2)&(((Frost-death)>0)&((Blood-death)>0))
                Spell.Cast(festering_strike, req => (blood == 2 || frost == 2) && (((frost - death) > 0) && ((blood - death) > 0))),
                // actions.single_target+=/defile,if=blood==2||frost==2
                Spell.CastOnGround(defile, on => Me.CurrentTarget, req => Spell.UseAOE && (blood == 2 || frost == 2)),
                //actions.single_target+=/plague_strike,if=!disease.min_ticking&(blood=2|frost=2)
                Spell.Cast(plague_strike, req => !disease.min_ticking && (blood == 2 || frost == 2)),
                //actions.single_target+=/scourge_strike,if=blood=2|frost=2
                Spell.Cast(scourge_strike, req => blood == 2 || frost == 2),
                //actions.single_target+=/festering_strike,if=((Blood-death)>1)
                Spell.Cast(festering_strike, req => ((blood - death) > 1)),
                //actions.single_target+=/blood_boil,if=((Blood-death)>1)
                Spell.Cast(blood_boil, req => Spell.UseAOE && ((blood - death) > 1)),
                //actions.single_target+=/festering_strike,if=((Frost-death)>1)
                Spell.Cast(festering_strike, req => ((frost - death) > 1)),
                //actions.single_target+=/blood_tap,if=((target.health.pct-3*(target.health.pct%target.time_to_die))<=45)&cooldown.soul_reaper.remains=0
                Spell.Cast(blood_tap, req => target.health_pct <= 46 && cooldown.soul_reaper_remains == 0),
                //actions.single_target+=/summon_gargoyle
                Spell.Cast(summon_gargoyle),
                //actions.single_target+=/death_and_decay
                Spell.CastOnGround(death_and_decay, on => Me.CurrentTarget, req => Spell.UseAOE),
                //actions.single_target+=/defile
                Spell.CastOnGround(defile, on => Me.CurrentTarget, req => Spell.UseAOE),
                //actions.single_target+=/blood_tap,if=cooldown.defile.remains=0
                Spell.Cast(blood_tap, req => cooldown.defile_remains == 0),
                //actions.single_target+=/plague_strike,if=!disease.min_ticking
                Spell.Cast(plague_strike, req => !disease.min_ticking),
                //actions.single_target+=/dark_transformation
                Spell.Cast(dark_transformation, req => Me.Pet),
                //actions.single_target+=/blood_tap,if=buff.blood_charge.stack>10&(buff.sudden_doom.react|(buff.dark_transformation.down&unholy<=1))
                Spell.Cast(blood_tap, req => buff.blood_charge_stack > 10 && (buff.sudden_doom_react || (buff.dark_transformation_down && unholy <= 1))),
                //actions.single_target+=/death_coil,if=buff.sudden_doom.react|(buff.dark_transformation.down&unholy<=1)
                Spell.Cast(death_coil, req => buff.sudden_doom_react || (buff.dark_transformation_down && unholy <= 1)),
                //actions.single_target+=/scourge_strike,if=!((target.health.pct-3*(target.health.pct%target.time_to_die))<=45)|(Unholy>=2)
                Spell.Cast(scourge_strike, req => !(target.health_pct <= 46) || (unholy >= 2)),
                //actions.single_target+=/blood_tap
                Spell.Cast(blood_tap),
                //actions.single_target+=/festering_strike,if=!((target.health.pct-3*(target.health.pct%target.time_to_die))<=45)|(((Frost-death)>0)&((Blood-death)>0))
                Spell.Cast(festering_strike, req => !(target.health_pct <= 46) || (((frost - death) > 0) && ((blood - death) > 0))),
                //actions.single_target+=/death_coil
                Spell.Cast(death_coil),
                //actions.single_target+=/plague_leech
                Spell.Cast(plague_leech /*, req => disease.min_ticking*/),
                //actions.single_target+=/scourge_strike,if=cooldown.empower_rune_weapon.remains=0
                Spell.Cast(scourge_strike, req => cooldown.empower_rune_weapon_remains == 0),
                //actions.single_target+=/festering_strike,if=cooldown.empower_rune_weapon.remains=0
                Spell.Cast(festering_strike, req => cooldown.empower_rune_weapon_remains == 0),
                //actions.single_target+=/blood_boil,if=cooldown.empower_rune_weapon.remains=0
                Spell.Cast(blood_boil, req => cooldown.empower_rune_weapon_remains == 0),
                //actions.single_target+=/icy_touch,if=cooldown.empower_rune_weapon.remains=0
                Spell.Cast(icy_touch, req => cooldown.empower_rune_weapon_remains == 0),
                //actions.single_target+=/empower_rune_weapon,if=blood<1&unholy<1&frost<1
                Spell.Cast(empower_rune_weapon, req => blood < 1 && unholy < 1 && frost < 1),
                new ActionAlwaysFail()
                );
        }

        private static Composite spread()
        {
            return new PrioritySelector(
                //actions.spread=blood_boil,cycle_targets=1,if=!disease.min_ticking
                Spell.Cast(blood_boil, req => Spell.UseAOE && active_enemies_list.Count(u => !disease.ticking_on(u)) > 0 && active_enemies_list.Any(disease.ticking_on)),
                //actions.spread+=/outbreak,if=!disease.min_ticking
                Spell.Cast(outbreak, req => !disease.min_ticking),
                //actions.spread+=/plague_strike,if=!disease.min_ticking
                Spell.Cast(plague_strike, req => !disease.min_ticking),
                new ActionAlwaysFail()
                );
        }

        #endregion
    }
}