using System;
using System.Linq;
using CommonBehaviors.Actions;
using Singular.Dynamics;
using Singular.Helpers;
using Singular.Managers;
using Styx;
using Styx.CommonBot;
using Styx.Helpers;
using Styx.WoWInternals;
using Styx.WoWInternals.WoWObjects;
using Styx.TreeSharp;
using Action = Styx.TreeSharp.Action;
using Singular.Settings;
using System.Drawing;

namespace Singular.ClassSpecific.Mage
{
    public class Arcane
    {
        #region Fields

        private static CombatScenario scenario;

        #endregion

        #region Properties

        private static double mana_pct
        {
            get { return Me.ManaPercent; }
        }

        private static double spell_haste
        {
            get { return 0; }
        }

        private static MageSettings MageSettings
        {
            get { return SingularSettings.Instance.Mage(); }
        }

        private static LocalPlayer Me
        {
            get { return StyxWoW.Me; }
        }

        private static int active_enemies
        {
            get { return scenario.Mobs.Count; }
        }

        private static double rune_of_power_cast_time
        {
            get { return Spell.GetSpellCastTime("Rune of Power").TotalSeconds; }
        }

        #endregion

        #region Public Methods

        [Behavior(BehaviorType.Pull, WoWClass.Mage, WoWSpec.MageArcane, WoWContext.All, 99)]
        [Behavior(BehaviorType.Heal, WoWClass.Mage, WoWSpec.MageArcane, WoWContext.All, 99)]
        public static Composite CreateMageArcaneDiagnostic(string state = null)
        {
            return CreateMageArcaneInstanceSimCCombat();
        }

        [Behavior(BehaviorType.Initialize, WoWClass.Mage, WoWSpec.MageArcane, priority: 9999)]
        public static Composite CreateMageArcaneInitialize()
        {
            scenario = new CombatScenario(40, 1.5f);

            return null;
        }

        [Behavior(BehaviorType.Combat, WoWClass.Mage, WoWSpec.MageArcane, WoWContext.Instances)]
        public static Composite CreateMageArcaneInstanceSimCCombat()
        {
            return new PrioritySelector(
                Helpers.Common.EnsureReadyToAttackFromLongRange(),
                Spell.WaitForCastOrChannel(FaceDuring.Yes),
                new Decorator(
                    ret => !Spell.IsGlobalCooldown(),
                    new PrioritySelector(
                        SingularRoutine.MoveBehaviorInlineToCombat(BehaviorType.Heal),
                        SingularRoutine.MoveBehaviorInlineToCombat(BehaviorType.CombatBuffs),
                        Helpers.Common.CreateInterruptBehavior(),
                        Movement.WaitForFacing(),
                        Movement.WaitForLineOfSpellSight(),

                        //# Executed every time the actor is available.
                        //actions=counterspell,if=target.debuff.casting.react
                        //actions+=/blink,if=movement.distance>10
                        //actions+=/blazing_speed,if=movement.remains>0
                        //actions+=/cold_snap,if=health.pct<30
                        //actions+=/time_warp,if=target.health.pct<25|time>5
                        //Spell.BuffSelf("Time Warp", req => target.health_pct < 25),
                        //actions+=/ice_floes,if=buff.ice_floes.down&(raid_event.movement.distance>0|raid_event.movement.in<action.arcane_missiles.cast_time)
                        //actions+=/rune_of_power,if=buff.rune_of_power.remains<cast_time
                        Spell.CastOnGround("Rune of Power", on => Me,
                            req => buff.rune_of_power_remains < rune_of_power_cast_time, false),
                        //actions+=/mirror_image
                        Spell.BuffSelf("Mirror Image"),
                        //actions+=/cold_snap,if=buff.presence_of_mind.down&cooldown.presence_of_mind.remains>75
                        //actions+=/call_action_list,name=aoe,if=active_enemies>=5
                        new Decorator(req => active_enemies >= 5, CreateAoeActionList()),
                        //actions+=/call_action_list,name=init_crystal,if=talent.prismatic_crystal.enabled&cooldown.prismatic_crystal.up
                        //actions+=/call_action_list,name=crystal_sequence,if=talent.prismatic_crystal.enabled&pet.prismatic_crystal.active
                        //actions+=/call_action_list,name=burn,if=time_to_die<mana.pct*0.35*spell_haste|cooldown.evocation.remains<=(mana.pct-30)*0.3*spell_haste|(buff.arcane_power.up&cooldown.evocation.remains<=(mana.pct-30)*0.4*spell_haste)
                        new Decorator(
                            req =>
                                target.time_to_die < mana_pct*0.35*spell_haste ||
                                cooldown.evocation_remains <= (mana_pct - 30)*0.3*spell_haste |
                                (buff.arcane_power_up & cooldown.evocation_remains <= (mana_pct - 30)*0.4*spell_haste),
                            CreateBurnActionList()),
                        //actions+=/call_action_list,name=conserve
                        new Decorator(CreateConserveActionList()),
                        new ActionAlwaysFail()
                        )
                    )
                );
        }

        [Behavior(BehaviorType.Combat, WoWClass.Mage, WoWSpec.MageArcane, WoWContext.Normal)]
        public static Composite CreateMageArcaneNormalCombat()
        {
            return CreateMageArcaneInstanceSimCCombat();
        }

        [Behavior(BehaviorType.Combat, WoWClass.Mage, WoWSpec.MageArcane, WoWContext.Battlegrounds)]
        public static Composite CreateMageArcanePvPCombat()
        {
            return CreateMageArcaneInstanceSimCCombat();
        }

        #endregion

        #region Private Methods

        private static Composite CreateAoeActionList()
        {
            return new PrioritySelector(
                //actions.aoe=call_action_list,name=cooldowns
                //actions.aoe+=/nether_tempest,cycle_targets=1,if=buff.arcane_charge.stack=4&(active_dot.nether_tempest=0|(ticking&remains<3.6))
                //actions.aoe+=/supernova
                //actions.aoe+=/arcane_orb,if=buff.arcane_charge.stack<4
                //actions.aoe+=/arcane_explosion,if=prev_gcd.evocation
                //actions.aoe+=/evocation,interrupt_if=mana.pct>96,if=mana.pct<85-2.5*buff.arcane_charge.stack
                //actions.aoe+=/arcane_missiles,if=set_bonus.tier17_4pc&active_enemies<10&buff.arcane_charge.stack=4&buff.arcane_instability.react
                //actions.aoe+=/nether_tempest,cycle_targets=1,if=talent.arcane_orb.enabled&buff.arcane_charge.stack=4&ticking&remains<cooldown.arcane_orb.remains
                //actions.aoe+=/arcane_barrage,if=buff.arcane_charge.stack=4
                //actions.aoe+=/cone_of_cold,if=glyph.cone_of_cold.enabled
                //actions.aoe+=/arcane_explosion
                new ActionAlwaysFail()
                );
        }

        private static Composite CreateBurnActionList()
        {
            return new PrioritySelector(
                //actions.burn=call_action_list,name=cooldowns
                //actions.burn+=/arcane_missiles,if=buff.arcane_missiles.react=3
                //actions.burn+=/arcane_missiles,if=set_bonus.tier17_4pc&buff.arcane_instability.react&buff.arcane_instability.remains<action.arcane_blast.execute_time
                //actions.burn+=/supernova,if=time_to_die<8|charges=2
                //actions.burn+=/nether_tempest,cycle_targets=1,if=target!=prismatic_crystal&buff.arcane_charge.stack=4&(active_dot.nether_tempest=0|(ticking&remains<3.6))
                //actions.burn+=/arcane_orb,if=buff.arcane_charge.stack<4
                //actions.burn+=/arcane_barrage,if=talent.arcane_orb.enabled&active_enemies>=3&buff.arcane_charge.stack=4&(cooldown.arcane_orb.remains<gcd|prev_gcd.arcane_orb)
                //actions.burn+=/presence_of_mind,if=mana.pct>96&(!talent.prismatic_crystal.enabled|!cooldown.prismatic_crystal.up)
                //actions.burn+=/arcane_blast,if=buff.arcane_charge.stack=4&mana.pct>93
                //actions.burn+=/arcane_missiles,if=buff.arcane_charge.stack=4&(mana.pct>70|!cooldown.evocation.up)
                //actions.burn+=/supernova,if=mana.pct>70&mana.pct<96
                //actions.burn+=/call_action_list,name=conserve,if=prev_gcd.evocation
                //actions.burn+=/evocation,interrupt_if=mana.pct>92,if=time_to_die>10&mana.pct<30+2.5*active_enemies*(9-active_enemies)
                //actions.burn+=/presence_of_mind,if=!talent.prismatic_crystal.enabled|!cooldown.prismatic_crystal.up
                //actions.burn+=/arcane_blast
                new ActionAlwaysFail()
                );
        }

        private static Composite CreateConserveActionList()
        {
            return new PrioritySelector(
                //actions.conserve=call_action_list,name=cooldowns,if=time_to_die<30|(buff.arcane_charge.stack=4&(!talent.prismatic_crystal.enabled|cooldown.prismatic_crystal.remains>15))
                //actions.conserve+=/arcane_missiles,if=buff.arcane_missiles.react=3|(talent.overpowered.enabled&buff.arcane_power.up&buff.arcane_power.remains<action.arcane_blast.execute_time)
                //actions.conserve+=/arcane_missiles,if=set_bonus.tier17_4pc&buff.arcane_instability.react&buff.arcane_instability.remains<action.arcane_blast.execute_time
                //actions.conserve+=/nether_tempest,cycle_targets=1,if=target!=prismatic_crystal&buff.arcane_charge.stack=4&(active_dot.nether_tempest=0|(ticking&remains<3.6))
                //actions.conserve+=/supernova,if=time_to_die<8|(charges=2&(buff.arcane_power.up|!cooldown.arcane_power.up)&(!talent.prismatic_crystal.enabled|cooldown.prismatic_crystal.remains>8))
                //actions.conserve+=/arcane_orb,if=buff.arcane_charge.stack<2
                //actions.conserve+=/presence_of_mind,if=mana.pct>96&(!talent.prismatic_crystal.enabled|!cooldown.prismatic_crystal.up)
                //actions.conserve+=/arcane_blast,if=buff.arcane_charge.stack=4&mana.pct>93
                //actions.conserve+=/arcane_barrage,if=talent.arcane_orb.enabled&active_enemies>=3&buff.arcane_charge.stack=4&(cooldown.arcane_orb.remains<gcd|prev_gcd.arcane_orb)
                //actions.conserve+=/arcane_missiles,if=buff.arcane_charge.stack=4&(!talent.overpowered.enabled|cooldown.arcane_power.remains>10*spell_haste)
                //actions.conserve+=/supernova,if=mana.pct<96&(buff.arcane_missiles.stack<2|buff.arcane_charge.stack=4)&(buff.arcane_power.up|(charges=1&cooldown.arcane_power.remains>recharge_time))&(!talent.prismatic_crystal.enabled|current_target=prismatic_crystal|(charges=1&cooldown.prismatic_crystal.remains>recharge_time+8))
                //actions.conserve+=/nether_tempest,cycle_targets=1,if=target!=prismatic_crystal&buff.arcane_charge.stack=4&(active_dot.nether_tempest=0|(ticking&remains<(10-3*talent.arcane_orb.enabled)*spell_haste))
                //actions.conserve+=/arcane_barrage,if=buff.arcane_charge.stack=4
                //actions.conserve+=/presence_of_mind,if=buff.arcane_charge.stack<2&(!talent.prismatic_crystal.enabled|!cooldown.prismatic_crystal.up)
                //actions.conserve+=/arcane_blast
                //actions.conserve+=/arcane_barrage,moving=1
                new ActionAlwaysFail()
                );
        }

        #endregion
    }

    public static class cooldown
    {
        #region Properties

        public static double evocation_remains
        {
            get { return Spell.GetSpellCooldown("Evocation").TotalSeconds; }
        }

        #endregion
    }

    public static class buff
    {
        #region Properties

        public static bool arcane_power_up
        {
            get { return StyxWoW.Me.GetAuraTimeLeft("Arcane Power").TotalSeconds > 0; }
        }

        public static double rune_of_power_remains
        {
            get { return StyxWoW.Me.GetAuraTimeLeft("Rune of Power").TotalSeconds; }
        }

        #endregion
    }

    public static class target
    {
        #region Properties

        public static double health_pct
        {
            get { return StyxWoW.Me.CurrentTarget.HealthPercent; }
        }

        public static long time_to_die
        {
            get { return StyxWoW.Me.CurrentTarget.TimeToDeath(); }
        }

        #endregion
    }
}