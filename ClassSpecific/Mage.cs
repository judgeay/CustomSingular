using System.Linq;
using CommonBehaviors.Actions;
using Singular.Dynamics;
using Singular.Helpers;
using Singular.Managers;
using Styx;
using Styx.TreeSharp;

namespace Singular.ClassSpecific
{
    // ReSharper disable InconsistentNaming
    // ReSharper disable ClassNeverInstantiated.Global
    public class Mage : Common
    {
        #region Enums

        private enum MageTalentsEnum
        {
            // ReSharper disable UnusedMember.Local
            Evanesce = 1,
            BlazingSpeed,
            IceFloes,

            AlterTime,
            Flameglow,
            IceBarrier,

            RingOfFrost,
            IceWard,
            Frostjaw,

            GreaterInvisibility,
            Cauterize,
            ColdSnap,

            NetherTempest,
            LivingBomb = NetherTempest,
            FrostBomb = NetherTempest,
            UnstableMagic,
            Supernova,
            BlastWave = Supernova,
            IceNova = Supernova,

            MirrorImage,
            RuneOfPower,
            IncantersFlow,

            Overpowered,
            Kindling = Overpowered,
            ThermalVoid = Overpowered,
            PrismaticCrystal,
            ArcaneOrb,
            Meteor = ArcaneOrb,
            CometStorm = ArcaneOrb
            // ReSharper restore UnusedMember.Local
        }

        #endregion

        #region Constant

        private const string arcane_barrage = "Arcane Barrage";
        private const string arcane_blast = "Arcane Blast";
        private const string arcane_brilliance = "Arcane Brilliance";
        private const string arcane_charge = "Arcane Charge";
        private const string arcane_explosion = "Arcane Explosion";
        private const int arcane_instability = 166872;
        private const string arcane_missiles = "Arcane Missiles";
        private const string arcane_missiles_proc = "Arcane Missiles!";
        //private const int arcane_missiles_proc = 79683;
        private const string arcane_orb = "Arcane Orb";
        private const string arcane_power = "Arcane Power";
        private const string cone_of_cold = "Cone of Cold";
        private const string dalaran_brilliance = "Dalaran Brilliance";
        private const string evocation = "Evocation";
        private const string mirror_image = "Mirror Image";
        private const string nether_tempest = "Nether Tempest";
        private const string presence_of_mind = "Presence of Mind";
        private const string prismatic_crystal = "Prismatic Crystal";
        private const string rune_of_power = "Rune of Power";
        private const string supernova = "Supernova";

        #endregion

        #region Properties

        private static bool UseArcaneExplosion
        {
            get { return active_enemies_list.Count(x => x.Distance < (TalentManager.HasGlyph(arcane_explosion) ? 15 : 10)) > 1; }
        }

        private static bool UseSupernova
        {
            get { return active_enemies_list.Count(x => x.Distance < 8) > 1; }
        }

        #endregion

        #region Public Methods

        [Behavior(BehaviorType.Combat | BehaviorType.Pull, WoWClass.Mage, WoWSpec.MageArcane)]
        public static Composite ArcaneActionList()
        {
            return new PrioritySelector(
                Helpers.Common.EnsureReadyToAttackFromLongRange(),
                Spell.WaitForCastOrChannel(FaceDuring.Yes),
                new Decorator(
                    ret => !Spell.IsGlobalCooldown(),
                    new PrioritySelector(
                        Helpers.Common.CreateInterruptBehavior(),
                        Movement.WaitForFacing(),
                        Movement.WaitForLineOfSpellSight(),

                        //# Executed every time the actor is available.
                        //actions=counterspell,if=target.debuff.casting.react
                        //actions+=/blink,if=movement.distance>10
                        //actions+=/blazing_speed,if=movement.remains>0
                        //actions+=/cold_snap,if=health.pct<30
                        //actions+=/time_warp,if=target.health.pct<25|time>5
                        //actions+=/ice_floes,if=buff.ice_floes.down&(raid_event.movement.distance>0|raid_event.movement.in<action.arcane_missiles.cast_time)
                        //actions+=/rune_of_power,if=buff.rune_of_power.remains<cast_time
                        Spell.CastOnGround(rune_of_power, on => Me, req => !buff.rune_of_power_react),
                        //actions+=/mirror_image
                        Spell.BuffSelf(mirror_image),
                        //actions+=/cold_snap,if=buff.presence_of_mind.down&cooldown.presence_of_mind.remains>75
                        //actions+=/call_action_list,name=aoe,if=active_enemies>=5
                        new Decorator(req => Spell.UseAOE && active_enemies >= 5, arcane_aoe()),
                        //actions+=/call_action_list,name=init_crystal,if=talent.prismatic_crystal.enabled&cooldown.prismatic_crystal.up
                        //new ActionList(arcane_init_crystal, req => talent.prismatic_crystal_enabled && cooldown.prismatic_crystal_up),
                        //actions+=/call_action_list,name=crystal_sequence,if=talent.prismatic_crystal.enabled&pet.prismatic_crystal.active
                        //new ActionList(arcane_crystal_sequence, req => talent.prismatic_crystal_enabled && pet.prismatic_crystal_active),
                        //actions+=/call_action_list,name=burn,if=time_to_die<mana.pct*0.35*spell_haste|cooldown.evocation.remains<=(mana.pct-30)*0.3*spell_haste|(buff.arcane_power.up&cooldown.evocation.remains<=(mana.pct-30)*0.4*spell_haste)
                        new Decorator(req => cooldown.evocation_remains <= (mana_pct - 30)*0.3*spell_haste || (buff.arcane_power_up && cooldown.evocation_remains <= (mana_pct - 30)*0.4*spell_haste), arcane_burn()),
                        //actions+=/call_action_list,name=conserve
                        new Decorator(arcane_conserve()),
                        new ActionAlwaysFail()
                        )));
        }

        [Behavior(BehaviorType.PreCombatBuffs | BehaviorType.PullBuffs, WoWClass.Mage)]
        public static Composite Buffs()
        {
            return new PrioritySelector(
                Spell.BuffSelf(arcane_brilliance, req => !Me.HasPartyBuff(PartyBuffType.SpellPower | PartyBuffType.Crit)),
                Spell.BuffSelf(dalaran_brilliance, req => !Me.HasPartyBuff(PartyBuffType.SpellPower | PartyBuffType.Crit)),
                new ActionAlwaysFail()
                );
        }

        #endregion

        #region Private Methods

        private static Composite arcane_aoe()
        {
            return new PrioritySelector(
                //actions.aoe=call_action_list,name=cooldowns
                new Decorator(arcane_cooldowns()),
                //actions.aoe+=/nether_tempest,cycle_targets=1,if=buff.arcane_charge.stack=4&(active_dot.nether_tempest=0|(ticking&remains<3.6))
                Spell.Buff(nether_tempest, 1, on => Me.CurrentTarget, req => buff.arcane_charge_stack == 4 && (!active_dot.nether_tempest_ticking || (active_dot.nether_tempest_ticking && active_dot.nether_tempest_remains < 3.6))),
                //actions.aoe+=/supernova
                Spell.BuffSelf(supernova, req => UseSupernova),
                //actions.aoe+=/arcane_orb,if=buff.arcane_charge.stack<4
                Spell.Cast(arcane_orb, req => buff.arcane_charge_stack < 4),
                //actions.aoe+=/arcane_explosion,if=prev_gcd.evocation
                Spell.Cast(arcane_explosion, req => UseArcaneExplosion && prev_gcd == evocation),
                //actions.aoe+=/evocation,interrupt_if=mana.pct>96,if=mana.pct<85-2.5*buff.arcane_charge.stack
                Spell.Cast(evocation, req => mana_pct < 85 - 2.5*buff.arcane_charge_stack),
                //actions.aoe+=/arcane_missiles,if=set_bonus.tier17_4pc&active_enemies<10&buff.arcane_charge.stack=4&buff.arcane_instability.react
                Spell.Cast(arcane_missiles, req => active_enemies < 10 && buff.arcane_charge_stack == 4 && buff.arcane_instability_react),
                //actions.aoe+=/nether_tempest,cycle_targets=1,if=talent.arcane_orb.enabled&buff.arcane_charge.stack=4&ticking&remains<cooldown.arcane_orb.remains
                Spell.Buff(nether_tempest, 1, on => Me.CurrentTarget, req => talent.arcane_orb_enabled && buff.arcane_charge_stack == 4 && active_dot.nether_tempest_ticking && active_dot.nether_tempest_remains < cooldown.arcane_orb_remains),
                //actions.aoe+=/arcane_barrage,if=buff.arcane_charge.stack=4
                Spell.Cast(arcane_barrage, req => buff.arcane_charge_stack == 4),
                //actions.aoe+=/cone_of_cold,if=glyph.cone_of_cold.enabled
                Spell.Cast(cone_of_cold, req => glyph.cone_of_cold_enabled),
                //actions.aoe+=/arcane_explosion
                Spell.Cast(arcane_explosion, req => UseArcaneExplosion),
                new ActionAlwaysFail()
                );
        }

        private static Composite arcane_burn()
        {
            return new PrioritySelector(
                //actions.burn=call_action_list,name=cooldowns
                new Decorator(arcane_cooldowns()),
                //actions.burn+=/arcane_missiles,if=buff.arcane_missiles.react=3
                Spell.Cast(arcane_missiles, req => buff.arcane_missiles_stack == 3),
                //actions.burn+=/arcane_missiles,if=set_bonus.tier17_4pc&buff.arcane_instability.react&buff.arcane_instability.remains<action.arcane_blast.execute_time
                Spell.Cast(arcane_missiles, req => buff.arcane_instability_react && buff.arcane_instability_remains < action.arcane_blast_execute_time),
                //actions.burn+=/supernova,if=time_to_die<8|charges=2
                Spell.BuffSelf(supernova, req => UseSupernova && /*target.time_to_die < 8 ||*/ action.supernova_charges == 2),
                //actions.burn+=/nether_tempest,cycle_targets=1,if=target!=prismatic_crystal&buff.arcane_charge.stack=4&(active_dot.nether_tempest=0|(ticking&remains<3.6))
                Spell.Buff(nether_tempest, 1, on => Me.CurrentTarget, req => buff.arcane_charge_stack == 4 && (!active_dot.nether_tempest_ticking || (active_dot.nether_tempest_ticking && active_dot.nether_tempest_remains < 3.6))),
                //actions.burn+=/arcane_orb,if=buff.arcane_charge.stack<4
                Spell.Cast(arcane_orb, req => buff.arcane_charge_stack < 4),
                //actions.burn+=/arcane_barrage,if=talent.arcane_orb.enabled&active_enemies>=3&buff.arcane_charge.stack=4&(cooldown.arcane_orb.remains<gcd|prev_gcd.arcane_orb)
                Spell.Cast(arcane_barrage, req => talent.arcane_orb_enabled && active_enemies >= 3 && buff.arcane_charge_stack == 4 && (cooldown.arcane_orb_remains < gcd || prev_gcd == arcane_orb)),
                //actions.burn+=/presence_of_mind,if=mana.pct>96&(!talent.prismatic_crystal.enabled|!cooldown.prismatic_crystal.up)
                Spell.BuffSelf(presence_of_mind, req => mana_pct > 96 && (!talent.prismatic_crystal_enabled || !cooldown.prismatic_crystal_up)),
                //actions.burn+=/arcane_blast,if=buff.arcane_charge.stack=4&mana.pct>93
                Spell.Cast(arcane_blast, req => buff.arcane_charge_stack == 4 && mana_pct > 93),
                //actions.burn+=/arcane_missiles,if=buff.arcane_charge.stack=4&(mana.pct>70|!cooldown.evocation.up)
                Spell.Cast(arcane_missiles, req => buff.arcane_charge_stack == 4 && (mana_pct > 70 || !cooldown.evocation_up)),
                //actions.burn+=/supernova,if=mana.pct>70&mana.pct<96
                Spell.BuffSelf(supernova, req => UseSupernova && mana_pct > 70 && mana_pct < 96),
                //actions.burn+=/call_action_list,name=conserve,if=prev_gcd.evocation
                new Decorator(req => prev_gcd == evocation, arcane_conserve()),
                //actions.burn+=/evocation,interrupt_if=mana.pct>92,if=time_to_die>10&mana.pct<30+2.5*active_enemies*(9-active_enemies)
                Spell.Cast(evocation, req => /*target.time_to_die > 10 &&*/ mana_pct < 30 + 2.5*active_enemies*(9 - active_enemies)),
                //actions.burn+=/presence_of_mind,if=!talent.prismatic_crystal.enabled|!cooldown.prismatic_crystal.up
                Spell.BuffSelf(presence_of_mind, req => !talent.prismatic_crystal_enabled || !cooldown.prismatic_crystal_up),
                //actions.burn+=/arcane_blast
                Spell.Cast(arcane_blast),
                new ActionAlwaysFail()
                );
        }

        private static Composite arcane_conserve()
        {
            return new PrioritySelector(
                //actions.conserve=call_action_list,name=cooldowns,if=time_to_die<30|(buff.arcane_charge.stack=4&(!talent.prismatic_crystal.enabled|cooldown.prismatic_crystal.remains>15))
                new Decorator(req => /*target.time_to_die < 30 ||*/ (buff.arcane_charge_stack == 4 && (!talent.prismatic_crystal_enabled || cooldown.prismatic_crystal_remains > 15)), arcane_cooldowns()),
                //actions.conserve+=/arcane_missiles,if=buff.arcane_missiles.react=3|(talent.overpowered.enabled&buff.arcane_power.up&buff.arcane_power.remains<action.arcane_blast.execute_time)
                Spell.Cast(arcane_missiles, req => buff.arcane_missiles_stack == 3 || (talent.overpowered_enabled && buff.arcane_power_up && buff.arcane_power_remains < action.arcane_blast_execute_time)),
                //actions.conserve+=/arcane_missiles,if=set_bonus.tier17_4pc&buff.arcane_instability.react&buff.arcane_instability.remains<action.arcane_blast.execute_time
                Spell.Cast(arcane_missiles, req => buff.arcane_instability_react && buff.arcane_instability_remains < action.arcane_blast_execute_time),
                //actions.conserve+=/nether_tempest,cycle_targets=1,if=target!=prismatic_crystal&buff.arcane_charge.stack=4&(active_dot.nether_tempest=0|(ticking&remains<3.6))
                Spell.Buff(nether_tempest, 1, on => Me.CurrentTarget, req => buff.arcane_charge_stack == 4 && (!active_dot.nether_tempest_ticking || (active_dot.nether_tempest_ticking && active_dot.nether_tempest_remains < 3.6))),
                //actions.conserve+=/supernova,if=time_to_die<8|(charges=2&(buff.arcane_power.up|!cooldown.arcane_power.up)&(!talent.prismatic_crystal.enabled|cooldown.prismatic_crystal.remains>8))
                Spell.BuffSelf(supernova,
                    req => UseSupernova && /*target.time_to_die < 8 ||*/ (action.supernova_charges == 2 && (buff.arcane_power_up || !cooldown.arcane_power_up) && (!talent.prismatic_crystal_enabled || cooldown.prismatic_crystal_remains > 8))),
                //actions.conserve+=/arcane_orb,if=buff.arcane_charge.stack<2
                Spell.Cast(arcane_orb, req => buff.arcane_charge_stack < 2),
                //actions.conserve+=/presence_of_mind,if=mana.pct>96&(!talent.prismatic_crystal.enabled|!cooldown.prismatic_crystal.up)
                Spell.BuffSelf(presence_of_mind, req => mana_pct > 96 && (!talent.prismatic_crystal_enabled || !cooldown.prismatic_crystal_up)),
                //actions.conserve+=/arcane_blast,if=buff.arcane_charge.stack=4&mana.pct>93
                Spell.Cast(arcane_blast, req => buff.arcane_charge_stack == 4 && mana_pct > 93),
                //actions.conserve+=/arcane_barrage,if=talent.arcane_orb.enabled&active_enemies>=3&buff.arcane_charge.stack=4&(cooldown.arcane_orb.remains<gcd|prev_gcd.arcane_orb)
                Spell.Cast(arcane_barrage, req => talent.arcane_orb_enabled && active_enemies >= 3 && buff.arcane_charge_stack == 4 && (cooldown.arcane_orb_remains < gcd || prev_gcd == arcane_orb)),
                //actions.conserve+=/arcane_missiles,if=buff.arcane_charge.stack=4&(!talent.overpowered.enabled|cooldown.arcane_power.remains>10*spell_haste)
                Spell.Cast(arcane_missiles, req => buff.arcane_charge_stack == 4 && (!talent.overpowered_enabled || cooldown.arcane_power_remains > 10*spell_haste)),
                //actions.conserve+=/supernova,if=mana.pct<96&(buff.arcane_missiles.stack<2|buff.arcane_charge.stack=4)&(buff.arcane_power.up|(charges=1&cooldown.arcane_power.remains>recharge_time))&(!talent.prismatic_crystal.enabled|current_target=prismatic_crystal|(charges=1&cooldown.prismatic_crystal.remains>recharge_time+8))
                Spell.BuffSelf(supernova,
                    req =>
                        UseSupernova && mana_pct < 96 && (buff.arcane_missiles_stack < 2 || buff.arcane_charge_stack == 4) && (buff.arcane_power_up || (action.supernova_charges == 1 && cooldown.arcane_power_remains > action.supernova_recharge_time)) &&
                        (!talent.prismatic_crystal_enabled || (action.supernova_charges == 1 && cooldown.prismatic_crystal_remains > action.supernova_recharge_time + 8))),
                //actions.conserve+=/nether_tempest,cycle_targets=1,if=target!=prismatic_crystal&buff.arcane_charge.stack=4&(active_dot.nether_tempest=0|(ticking&remains<(10-3*talent.arcane_orb.enabled)*spell_haste))
                Spell.Buff(nether_tempest, 1, on => Me.CurrentTarget,
                    req => buff.arcane_charge_stack == 4 && (!active_dot.nether_tempest_ticking || (active_dot.nether_tempest_ticking && active_dot.nether_tempest_remains < (10 - 3*(talent.arcane_orb_enabled ? 1 : 0))*spell_haste))),
                //actions.conserve+=/arcane_barrage,if=buff.arcane_charge.stack=4
                Spell.Cast(arcane_barrage, req => buff.arcane_charge_stack == 4),
                //actions.conserve+=/presence_of_mind,if=buff.arcane_charge.stack<2&(!talent.prismatic_crystal.enabled|!cooldown.prismatic_crystal.up)
                Spell.BuffSelf(presence_of_mind, req => buff.arcane_charge_stack < 2 && (!talent.prismatic_crystal_enabled || !cooldown.prismatic_crystal_up)),
                //actions.conserve+=/arcane_blast
                Spell.Cast(arcane_blast),
                //actions.conserve+=/arcane_barrage,moving=1
                Spell.Cast(arcane_barrage),
                new ActionAlwaysFail()
                );
        }

        private static Composite arcane_cooldowns()
        {
            return new PrioritySelector(
                //actions.cooldowns=arcane_power
                Spell.BuffSelf(arcane_power),
                //actions.cooldowns+=/blood_fury
                //actions.cooldowns+=/berserking
                //actions.cooldowns+=/arcane_torrent
                //actions.cooldowns+=/potion,name=draenic_intellect,if=buff.arcane_power.up&(!talent.prismatic_crystal.enabled|pet.prismatic_crystal.active)

                new ActionAlwaysFail()
                );
        }

        #endregion

        #region Types

        private static class action
        {
            #region Properties

            public static double arcane_blast_execute_time
            {
                get { return Spell.GetSpellCastTime(arcane_blast).TotalSeconds; }
            }

            public static int supernova_charges
            {
                get { return Spell.GetCharges(supernova); }
            }

            public static double supernova_recharge_time
            {
                get { return Spell.GetSpellCastTime(supernova).TotalSeconds; }
            }

            #endregion
        }

        private static class active_dot
        {
            #region Properties

            public static double nether_tempest_remains
            {
                get { return Remains(nether_tempest); }
            }

            public static bool nether_tempest_ticking
            {
                get { return Ticking(nether_tempest); }
            }

            #endregion

            #region Private Methods

            private static double Remains(string dot)
            {
                return StyxWoW.Me.CurrentTarget.GetAuraTimeLeft(dot).TotalSeconds;
            }

            private static bool Ticking(string dot)
            {
                return Remains(dot) > 0;
            }

            #endregion
        }

        private static class buff
        {
            #region Properties

            public static uint arcane_charge_stack
            {
                get { return Stack(arcane_charge); }
            }

            public static bool arcane_instability_react
            {
                get { return React(arcane_instability); }
            }

            public static double arcane_instability_remains
            {
                get { return Remain(arcane_instability); }
            }

            public static decimal arcane_missiles_stack
            {
                get { return Stack(arcane_missiles_proc); }
            }

            public static double arcane_power_remains
            {
                get { return Remain(arcane_power); }
            }

            public static bool arcane_power_up
            {
                get { return Up(arcane_power); }
            }

            public static bool rune_of_power_react
            {
                get { return React(rune_of_power); }
            }

            #endregion

            #region Private Methods

            private static bool React(string aura)
            {
                return StyxWoW.Me.HasAura(aura);
            }

            private static bool React(int aura)
            {
                return StyxWoW.Me.HasAura(aura);
            }

            private static double Remain(string aura)
            {
                return StyxWoW.Me.GetAuraTimeLeft(aura).TotalSeconds;
            }

            private static double Remain(int aura)
            {
                return StyxWoW.Me.GetAuraTimeLeft(aura).TotalSeconds;
            }

            private static uint Stack(string aura)
            {
                return StyxWoW.Me.GetAuraStacks(aura);
            }

            private static bool Up(string aura)
            {
                return Remain(aura) > 0;
            }

            #endregion
        }

        private static class cooldown
        {
            #region Properties

            public static double arcane_orb_remains
            {
                get { return Remains(arcane_orb); }
            }

            public static double arcane_power_remains
            {
                get { return Remains(arcane_power); }
            }

            public static bool arcane_power_up
            {
                get { return Up(arcane_power); }
            }

            public static double evocation_remains
            {
                get { return Remains(evocation); }
            }

            public static bool evocation_up
            {
                get { return Up(evocation); }
            }

            public static double prismatic_crystal_remains
            {
                get { return Remains(prismatic_crystal); }
            }

            public static bool prismatic_crystal_up
            {
                get { return Up(prismatic_crystal); }
            }

            #endregion

            #region Private Methods

            private static double Remains(string spell)
            {
                return Spell.GetSpellCooldown(spell).TotalSeconds;
            }

            private static bool Up(string spell)
            {
                return Remains(spell) <= 0;
            }

            #endregion
        }

        private static class glyph
        {
            #region Properties

            public static bool cone_of_cold_enabled
            {
                get { return TalentManager.HasGlyph(cone_of_cold); }
            }

            #endregion
        }

        //private static class pet
        //{
        //    #region Properties

        //    public static bool prismatic_crystal_active
        //    {
        //        get { throw new NotImplementedException(); }
        //    }

        //    #endregion
        //}

        private static class talent
        {
            #region Properties

            public static bool arcane_orb_enabled
            {
                get { return HasTalent(MageTalentsEnum.ArcaneOrb); }
            }

            public static bool overpowered_enabled
            {
                get { return HasTalent(MageTalentsEnum.Overpowered); }
            }

            public static bool prismatic_crystal_enabled
            {
                get { return HasTalent(MageTalentsEnum.PrismaticCrystal); }
            }

            #endregion

            #region Private Methods

            private static bool HasTalent(MageTalentsEnum tal)
            {
                return TalentManager.IsSelected((int) tal);
            }

            #endregion
        }

        #endregion
    }
}