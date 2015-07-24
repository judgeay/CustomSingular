using System;
using System.Linq;
using CommonBehaviors.Actions;
using Singular.ClassSpecific.Common;
using Singular.Dynamics;
using Singular.Helpers;
using Styx;
using Styx.TreeSharp;
using Styx.WoWInternals.WoWObjects;
using Action = Styx.TreeSharp.Action;

namespace Singular.ClassSpecific
{
    // ReSharper disable InconsistentNaming
    // ReSharper disable ClassNeverInstantiated.Global
    public class Mage : ClassSpecificBase
    {
        #region Fields

        private const byte ARCANE_EXPLOSION_DISTANCE = 10;
        private const byte ARCANE_EXPLOSION_GLYPH_DISTANCE = 15;
        private const byte NETHER_TEMPEST_DISTANCE = 10;
        private const byte SUPERNOVA_DISTANCE = 8;

        private static readonly Func<Func<bool>, Composite> arcane_barrage = cond => Spell.Cast(MageSpells.arcane_barrage, req => Spell.UseAoe && cond());
        private static readonly Func<Func<bool>, Composite> arcane_blast = cond => Spell.Cast(MageSpells.arcane_blast, req => cond());
        private static readonly Func<Func<bool>, Composite> arcane_brilliance = cond => Spell.BuffSelf(MageSpells.arcane_brilliance, req => !Me.HasPartyBuff(PartyBuffType.SpellPower | PartyBuffType.Crit) && cond());

        private static readonly Func<Func<bool>, Composite> arcane_explosion =
            cond => Spell.Cast(MageSpells.arcane_explosion, req => Spell.UseAoe && EnemiesCountNearTarget(Me, glyph.arcane_explosion.enabled ? ARCANE_EXPLOSION_GLYPH_DISTANCE : ARCANE_EXPLOSION_DISTANCE) >= 2 && cond());

        private static readonly Func<Func<bool>, Composite> arcane_missiles = cond => Spell.Cast(MageSpells.arcane_missiles, req => cond());
        private static readonly Func<Func<bool>, Composite> arcane_orb = cond => Spell.Cast(MageSpells.arcane_orb, req => Spell.UseAoe && talent.arcane_orb.enabled && cond());
        private static readonly Func<Func<bool>, Composite> arcane_power = cond => Spell.BuffSelf(MageSpells.arcane_power, req => Spell.UseCooldown && cond());
        private static readonly Func<Func<bool>, Composite> cone_of_cold = cond => Spell.Cast(MageSpells.cone_of_cold, req => Spell.UseAoe && cond());
        private static readonly Func<Func<bool>, Composite> dalaran_brilliance = cond => Spell.BuffSelf(MageSpells.dalaran_brilliance, req => !Me.HasPartyBuff(PartyBuffType.SpellPower | PartyBuffType.Crit) && cond());
        private static readonly Func<Func<bool>, Composite> evocation = cond => Spell.Cast(MageSpells.evocation, req => cond());
        private static readonly Func<Func<bool>, Composite> mirror_image = cond => Spell.BuffSelf(MageSpells.mirror_image, req => Spell.UseCooldown && talent.mirror_image.enabled && cond());

        private static readonly Func<Func<bool>, Composite> nether_tempest =
            cond => Spell.Buff(MageSpells.nether_tempest, 1, on => NetherTempestTarget, req => Spell.UseAoe && talent.nether_tempest.enabled && EnemiesCountNearTarget(NetherTempestTarget, NETHER_TEMPEST_DISTANCE) >= 2 && cond());

        private static readonly Func<Func<bool>, Composite> presence_of_mind = cond => Spell.BuffSelf(MageSpells.presence_of_mind, req => Spell.UseCooldown && cond());
        private static readonly Func<Func<bool>, Composite> rune_of_power = cond => Spell.CastOnGround(MageSpells.rune_of_power, on => Me, req => talent.rune_of_power.enabled && cond());
        private static readonly Func<Func<bool>, Composite> supernova = cond => Spell.Buff(MageSpells.supernova, on => NetherTempestTarget, req => talent.supernova.enabled && EnemiesCountNearTarget(SupernovaTarget, SUPERNOVA_DISTANCE) >= 2 && cond());

        private static bool burn_phase;

        #endregion

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

        #region Properties

        public static WoWUnit NetherTempestTarget
        {
            get { return active_enemies_list.OrderByDescending(x => EnemiesCountNearTarget(x, NETHER_TEMPEST_DISTANCE)).FirstOrDefault(); }
        }

        public static WoWUnit SupernovaTarget
        {
            get
            {
                var units = active_enemies_list.ToList();
                if (Me.GroupInfo.IsInParty) units.AddRange(Me.GroupInfo.RaidMembers.Where(x => x != null).Select(x => x.ToPlayer()).Where(x => x != null));
                if (!units.Contains(Me)) units.Add(Me);

                return units.OrderByDescending(x => EnemiesCountNearTarget(x, SUPERNOVA_DISTANCE)).FirstOrDefault();
            }
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
                        use_trinket(),
                        //actions=counterspell,if=target.debuff.casting.react
                        //actions+=/stop_burn_phase,if=prev_gcd.evocation&burn_phase_duration>gcd.max
                        new Decorator(arcane_stop_burn_phase(), req => prev_gcd == MageSpells.evocation),
                        //actions+=/cold_snap,if=health.pct<30
                        //actions+=/time_warp,if=target.health.pct<25|time>5
                        //actions+=/call_action_list,name=movement,if=raid_event.movement.exists
                        //actions+=/rune_of_power,if=buff.rune_of_power.remains<2*spell_haste
                        rune_of_power(() => buff.rune_of_power.remains < 2 * spell_haste),
                        //actions+=/mirror_image
                        mirror_image(() => true),
                        //actions+=/cold_snap,if=buff.presence_of_mind.down&cooldown.presence_of_mind.remains>75
                        //actions+=/call_action_list,name=aoe,if=active_enemies>=5
                        new Decorator(arcane_aoe(), req => active_enemies >= 5),
                        //actions+=/call_action_list,name=init_burn,if=!burn_phase
                        new Decorator(arcane_init_burn(), req => true),
                        //actions+=/call_action_list,name=burn,if=burn_phase
                        new Decorator(arcane_burn(), req => burn_phase),
                        //actions+=/call_action_list,name=conserve
                        new Decorator(arcane_conserve()),
                        new ActionAlwaysFail()
                        )))
                ;
        }

        [Behavior(BehaviorType.PreCombatBuffs | BehaviorType.PullBuffs, WoWClass.Mage)]
        public static Composite Buffs()
        {
            return new PrioritySelector(
                arcane_brilliance(() => true),
                dalaran_brilliance(() => true),
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
                nether_tempest(() => buff.arcane_charge.stack == 4 && (!active_dot.nether_tempest.Ticking(NetherTempestTarget) || (active_dot.nether_tempest.Ticking(NetherTempestTarget) && active_dot.nether_tempest.Remains(NetherTempestTarget) < 3.6))),
                //actions.aoe+=/supernova
                supernova(() => true),
                //actions.aoe+=/arcane_orb,if=buff.arcane_charge.stack<4
                arcane_orb(() => buff.arcane_charge.stack < 4),
                //# APL hack for evocation interrupt
                //actions.aoe+=/arcane_explosion,if=prev_gcd.evocation
                arcane_explosion(() => prev_gcd == MageSpells.evocation),
                //actions.aoe+=/evocation,interrupt_if=mana.pct>96,if=mana.pct<85-2.5*buff.arcane_charge.stack
                evocation(() => mana.pct < 85 - 2.5 * buff.arcane_charge.stack),
                //actions.aoe+=/arcane_missiles,if=set_bonus.tier17_4pc&active_enemies<10&buff.arcane_charge.stack=4&buff.arcane_instability.react
                arcane_missiles(() => set_bonus.tier17_4pc && active_enemies < 10 && buff.arcane_charge.stack == 4 && buff.arcane_instability.react),
                //actions.aoe+=/arcane_missiles,target_if=debuff.mark_of_doom.remains>2*spell_haste+(target.distance%20),if=buff.arcane_missiles.react
                arcane_missiles(() => debuff.mark_of_doom.remains > 2 * spell_haste + (target.distance % 20) && buff.arcane_missiles.react),
                //actions.aoe+=/nether_tempest,cycle_targets=1,if=talent.arcane_orb.enabled&buff.arcane_charge.stack=4&ticking&remains<cooldown.arcane_orb.remains
                nether_tempest(() => talent.arcane_orb.enabled && buff.arcane_charge.stack == 4 && active_dot.nether_tempest.Ticking(NetherTempestTarget) && active_dot.nether_tempest.Remains(NetherTempestTarget) < cooldown.arcane_orb.remains),
                //actions.aoe+=/arcane_barrage,if=buff.arcane_charge.stack=4
                arcane_barrage(() => buff.arcane_charge.stack == 4),
                //actions.aoe+=/cone_of_cold,if=glyph.cone_of_cold.enabled
                cone_of_cold(() => glyph.cone_of_cold.enabled),
                //actions.aoe+=/arcane_explosion
                arcane_explosion(() => true),
                new ActionAlwaysFail()
                );
        }

        private static Composite arcane_burn()
        {
            return new PrioritySelector(
                //# High mana usage, "Burn" sequence
                //actions.burn=call_action_list,name=init_crystal,if=talent.prismatic_crystal.enabled&cooldown.prismatic_crystal.up
                //actions.burn+=/call_action_list,name=crystal_sequence,if=talent.prismatic_crystal.enabled&pet.prismatic_crystal.active
                //actions.burn+=/call_action_list,name=cooldowns
                new Decorator(arcane_cooldowns()),
                //actions.burn+=/arcane_missiles,if=buff.arcane_missiles.react=3
                arcane_missiles(() => buff.arcane_missiles.stack == 3),
                //actions.burn+=/arcane_missiles,if=set_bonus.tier17_4pc&buff.arcane_instability.react&buff.arcane_instability.remains<action.arcane_blast.execute_time
                arcane_missiles(() => set_bonus.tier17_4pc && buff.arcane_instability.react && buff.arcane_instability.remains < action.arcane_blast.execute_time),
                //actions.burn+=/supernova,if=target.time_to_die<8|charges=2
                supernova(() => target.time_to_die < 8 || action.supernova.charges == 2),
                //actions.burn+=/nether_tempest,cycle_targets=1,if=target!=pet.prismatic_crystal&buff.arcane_charge.stack=4&(active_dot.nether_tempest=0|(ticking&remains<3.6))
                nether_tempest(
                    () => /*target != pet.prismatic_crystal && */
                        buff.arcane_charge.stack == 4 && (!active_dot.nether_tempest.Ticking(NetherTempestTarget) || (active_dot.nether_tempest.Ticking(NetherTempestTarget) && active_dot.nether_tempest.Remains(NetherTempestTarget) < 3.6))),
                //actions.burn+=/arcane_orb,if=buff.arcane_charge.stack<4
                arcane_orb(() => buff.arcane_charge.stack < 4),
                //actions.burn+=/arcane_barrage,if=talent.arcane_orb.enabled&active_enemies>=3&buff.arcane_charge.stack=4&(cooldown.arcane_orb.remains<gcd.max|prev_gcd.arcane_orb)
                arcane_barrage(() => talent.arcane_orb.enabled && active_enemies >= 3 && buff.arcane_charge.stack == 4 && (cooldown.arcane_orb.remains < gcd_max || prev_gcd == MageSpells.arcane_orb)),
                //actions.burn+=/presence_of_mind,if=mana.pct>96&(!talent.prismatic_crystal.enabled|!cooldown.prismatic_crystal.up)
                presence_of_mind(() => mana.pct > 96 && (!talent.prismatic_crystal.enabled || !cooldown.prismatic_crystal.up)),
                //actions.burn+=/arcane_blast,if=buff.arcane_charge.stack=4&mana.pct>93
                arcane_blast(() => buff.arcane_charge.stack == 4 && mana.pct > 93),
                //actions.burn+=/arcane_missiles,if=buff.arcane_charge.stack=4&(mana.pct>70|!cooldown.evocation.up|target.time_to_die<15)
                arcane_missiles(() => buff.arcane_charge.stack == 4 && (mana.pct > 70 || !cooldown.evocation.up || target.time_to_die < 15)),
                //actions.burn+=/supernova,if=mana.pct>70&mana.pct<96
                supernova(() => mana.pct > 70 && mana.pct < 96),
                //actions.burn+=/evocation,interrupt_if=mana.pct>100-10%spell_haste,if=target.time_to_die>10&mana.pct<30+2.5*active_enemies*(9-active_enemies)-(40*(t18_class_trinket&buff.arcane_power.up))
                evocation(() => target.time_to_die > 10 && mana.pct < 30 + 2.5 * active_enemies * (9 - active_enemies) - (40 * (t18_class_trinket && buff.arcane_power.up).ToInt())),
                //actions.burn+=/presence_of_mind,if=!talent.prismatic_crystal.enabled|!cooldown.prismatic_crystal.up
                presence_of_mind(() => !talent.prismatic_crystal.enabled || !cooldown.prismatic_crystal.up),
                //actions.burn+=/arcane_blast
                arcane_blast(() => true),
                //actions.burn+=/evocation
                evocation(() => true),
                new ActionAlwaysFail()
                );
        }

        private static Composite arcane_conserve()
        {
            return new PrioritySelector(
                //# Low mana usage, "Conserve" sequence
                //actions.conserve=call_action_list,name=cooldowns,if=target.time_to_die<15
                new Decorator(arcane_cooldowns(), req => target.time_to_die < 15),
                //actions.conserve+=/arcane_missiles,if=buff.arcane_missiles.react=3|(talent.overpowered.enabled&buff.arcane_power.up&buff.arcane_power.remains<action.arcane_blast.execute_time)
                arcane_missiles(() => buff.arcane_missiles.stack == 3 || (talent.overpowered.enabled && buff.arcane_power.up && buff.arcane_power.remains < action.arcane_blast.execute_time)),
                //actions.conserve+=/arcane_missiles,if=set_bonus.tier17_4pc&buff.arcane_instability.react&buff.arcane_instability.remains<action.arcane_blast.execute_time
                arcane_missiles(() => set_bonus.tier17_4pc && buff.arcane_instability.react && buff.arcane_instability.remains < action.arcane_blast.execute_time),
                //actions.conserve+=/nether_tempest,cycle_targets=1,if=target!=pet.prismatic_crystal&buff.arcane_charge.stack=4&(active_dot.nether_tempest=0|(ticking&remains<3.6))
                nether_tempest(
                    () => /*target!=pet.prismatic_crystal && */
                        buff.arcane_charge.stack == 4 && (!active_dot.nether_tempest.Ticking(NetherTempestTarget) || (active_dot.nether_tempest.Ticking(NetherTempestTarget) && active_dot.nether_tempest.Remains(NetherTempestTarget) < 3.6))),
                //actions.conserve+=/supernova,if=target.time_to_die<8|(charges=2&(buff.arcane_power.up|!cooldown.arcane_power.up)&(!talent.prismatic_crystal.enabled|cooldown.prismatic_crystal.remains>8))
                supernova(() => target.time_to_die < 8 || (action.supernova.charges == 2 && (buff.arcane_power.up || !cooldown.arcane_power.up) && (!talent.prismatic_crystal.enabled || cooldown.prismatic_crystal.remains > 8))),
                //actions.conserve+=/arcane_orb,if=buff.arcane_charge.stack<2
                arcane_orb(() => buff.arcane_charge.stack < 2),
                //actions.conserve+=/presence_of_mind,if=mana.pct>96&(!talent.prismatic_crystal.enabled|!cooldown.prismatic_crystal.up)
                presence_of_mind(() => mana.pct > 96 && (!talent.prismatic_crystal.enabled || !cooldown.prismatic_crystal.up)),
                //actions.conserve+=/arcane_missiles,if=buff.arcane_missiles.react&debuff.mark_of_doom.remains>2*spell_haste+(target.distance%20)
                arcane_missiles(() => buff.arcane_missiles.react && debuff.mark_of_doom.remains > 2 * spell_haste + (target.distance % 20)),
                //actions.conserve+=/arcane_blast,if=buff.arcane_charge.stack=4&mana.pct>93
                arcane_blast(() => buff.arcane_charge.stack == 4 && mana.pct > 93),
                //actions.conserve+=/arcane_barrage,if=talent.arcane_orb.enabled&active_enemies>=3&buff.arcane_charge.stack=4&(cooldown.arcane_orb.remains<gcd.max|prev_gcd.arcane_orb)
                arcane_barrage(() => talent.arcane_orb.enabled && active_enemies >= 3 && buff.arcane_charge.stack == 4 && (cooldown.arcane_orb.remains < gcd_max || prev_gcd == MageSpells.arcane_orb)),
                //actions.conserve+=/arcane_missiles,if=buff.arcane_charge.stack=4&(!talent.overpowered.enabled|cooldown.arcane_power.remains>10*spell_haste)
                arcane_missiles(() => buff.arcane_charge.stack == 4 && (!talent.overpowered.enabled || cooldown.arcane_power.remains > 10 * spell_haste)),
                //actions.conserve+=/supernova,if=mana.pct<96&(buff.arcane_missiles.stack<2|buff.arcane_charge.stack=4)&(buff.arcane_power.up|(charges=1&cooldown.arcane_power.remains>recharge_time))&(!talent.prismatic_crystal.enabled|current_target=pet.prismatic_crystal|(charges=1&cooldown.prismatic_crystal.remains>recharge_time+8))
                supernova(
                    () =>
                        mana.pct < 96 && (buff.arcane_missiles.stack < 2 || buff.arcane_charge.stack == 4) && (buff.arcane_power.up || (action.supernova.charges == 1 && cooldown.arcane_power.remains > action.supernova.recharge_time)) &&
                        (!talent.prismatic_crystal.enabled || /*current_target = pet.prismatic_crystal || */ (action.supernova.charges == 1 && cooldown.prismatic_crystal.remains > action.supernova.recharge_time + 8))),
                //actions.conserve+=/nether_tempest,cycle_targets=1,if=target!=pet.prismatic_crystal&buff.arcane_charge.stack=4&(active_dot.nether_tempest=0|(ticking&remains<(10-3*talent.arcane_orb.enabled)*spell_haste))
                nether_tempest(
                    () => /*target!=pet.prismatic_crystal && */
                        buff.arcane_charge.stack == 4 &&
                        (!active_dot.nether_tempest.Ticking(NetherTempestTarget) ||
                         (active_dot.nether_tempest.Ticking(NetherTempestTarget) && active_dot.nether_tempest.Remains(NetherTempestTarget) < (10 - 3 * talent.arcane_orb.enabled.ToInt()) * spell_haste))),
                //actions.conserve+=/arcane_barrage,if=buff.arcane_charge.stack=4
                arcane_barrage(() => buff.arcane_charge.stack == 4),
                //actions.conserve+=/presence_of_mind,if=buff.arcane_charge.stack<2&mana.pct>93
                presence_of_mind(() => buff.arcane_charge.stack < 2 && mana.pct > 93),
                //actions.conserve+=/arcane_blast
                arcane_blast(() => true),
                //actions.conserve+=/arcane_barrage
                arcane_barrage(() => true),
                new ActionAlwaysFail()
                );
        }

        private static Composite arcane_cooldowns()
        {
            return new PrioritySelector(
                //actions.cooldowns=arcane_power
                arcane_power(() => true),
                //actions.cooldowns+=/blood_fury
                blood_fury(() => true),
                //actions.cooldowns+=/berserking
                berserking(() => true),
                //actions.cooldowns+=/arcane_torrent
                arcane_torrent(() => mana.pct < 20),
                //actions.cooldowns+=/potion,name=draenic_intellect,if=buff.arcane_power.up&(!talent.prismatic_crystal.enabled|pet.prismatic_crystal.active)
                //actions.cooldowns+=/use_item,slot=finger2
                new ActionAlwaysFail()
                );
        }

        private static Composite arcane_init_burn()
        {
            return new PrioritySelector(
                //# Regular burn with evocation
                //actions.init_burn=start_burn_phase,if=buff.arcane_charge.stack>=4&(cooldown.prismatic_crystal.up|!talent.prismatic_crystal.enabled)&(cooldown.arcane_power.up|(glyph.arcane_power.enabled&cooldown.arcane_power.remains>60))&(cooldown.evocation.remains-2*buff.arcane_missiles.stack*spell_haste-gcd.max*talent.prismatic_crystal.enabled)*0.75*(1-0.1*(cooldown.arcane_power.remains<5))*(1-0.1*(talent.nether_tempest.enabled|talent.supernova.enabled))*(10%action.arcane_blast.execute_time)<mana.pct-20-2.5*active_enemies*(9-active_enemies)+(cooldown.evocation.remains*1.8%spell_haste)
                new Decorator(arcane_start_burn_phase(),
                    req =>
                        buff.arcane_charge.stack >= 4 && (cooldown.prismatic_crystal.up || !talent.prismatic_crystal.enabled) && (cooldown.arcane_power.up || (glyph.arcane_power.enabled && cooldown.arcane_power.remains > 60)) &&
                        (cooldown.evocation.remains - 2 * buff.arcane_missiles.stack * spell_haste - gcd_max * talent.prismatic_crystal.enabled.ToInt()) * 0.75 * (1 - 0.1 * (cooldown.arcane_power.remains < 5).ToInt()) *
                        (1 - 0.1 * (talent.nether_tempest.enabled || talent.supernova.enabled).ToInt()) * (10 % action.arcane_blast.execute_time) <
                        mana.pct - 20 - 2.5 * active_enemies * (9 - active_enemies) + (cooldown.evocation.remains * 1.8 % spell_haste)),
                //# End of fight burn
                //actions.init_burn+=/start_burn_phase,if=target.time_to_die*0.75*(1-0.1*(talent.nether_tempest.enabled|talent.supernova.enabled))*(10%action.arcane_blast.execute_time)*1.1<mana.pct-10+(target.time_to_die*1.8%spell_haste)
                new Decorator(arcane_start_burn_phase(),
                    req => target.time_to_die * 0.75 * (1 - 0.1 * (talent.nether_tempest.enabled || talent.supernova.enabled).ToInt()) * (10 % action.arcane_blast.execute_time) * 1.1 < mana.pct - 10 + (target.time_to_die * 1.8 % spell_haste)),
                new ActionAlwaysFail()
                );
        }

        private static Composite arcane_start_burn_phase()
        {
            return new Action(ActionDelegate => burn_phase = true);
        }

        private static Composite arcane_stop_burn_phase()
        {
            return new Action(ActionDelegate => burn_phase = false);
        }

        #endregion

        // ReSharper disable MemberHidesStaticFromOuterClass
        // ReSharper disable UnusedMember.Local

        #region Types

        private static class MageSpells
        {
            #region Fields

            public const string arcane_barrage = "Arcane Barrage";
            public const string arcane_blast = "Arcane Blast";
            public const string arcane_brilliance = "Arcane Brilliance";
            public const string arcane_charge = "Arcane Charge";
            public const string arcane_explosion = "Arcane Explosion";
            public const int arcane_instability = 166872;
            public const string arcane_missiles = "Arcane Missiles";
            public const string arcane_missiles_proc = "Arcane Missiles!";
            public const string arcane_orb = "Arcane Orb";
            public const string arcane_power = "Arcane Power";
            public const string cone_of_cold = "Cone of Cold";
            public const string dalaran_brilliance = "Dalaran Brilliance";
            public const string evocation = "Evocation";
            public const string mark_of_doom = "Mark of Doom";
            public const string mirror_image = "Mirror Image";
            public const string nether_tempest = "Nether Tempest";
            public const string presence_of_mind = "Presence of Mind";
            public const string prismatic_crystal = "Prismatic Crystal";
            public const string rune_of_power = "Rune of Power";
            public const string supernova = "Supernova";

            #endregion
        }

        private class action : ActionBase
        {
            #region Fields

            public static readonly action arcane_blast = new action(MageSpells.supernova);
            public static readonly action supernova = new action(MageSpells.supernova);

            #endregion

            #region Constructors

            private action(string spellName)
                : base(spellName)
            {
            }

            #endregion
        }

        private class active_dot : DotBase
        {
            #region Fields

            public static readonly active_dot nether_tempest = new active_dot(MageSpells.nether_tempest);

            #endregion

            #region Constructors

            private active_dot(string spell)
                : base(spell)
            {
            }

            #endregion
        }

        private class buff : BuffBase
        {
            #region Fields

            public static readonly buff arcane_charge = new buff(MageSpells.arcane_charge);
            public static readonly buff arcane_instability = new buff(MageSpells.arcane_charge);
            public static readonly buff arcane_missiles = new buff(MageSpells.arcane_charge);
            public static readonly buff arcane_power = new buff(MageSpells.arcane_charge);
            public static readonly buff rune_of_power = new buff(MageSpells.arcane_charge);

            #endregion

            #region Constructors

            private buff(string spell)
                : base(spell)
            {
            }

            #endregion
        }

        private class cooldown : CooldownBase
        {
            #region Fields

            public static readonly cooldown arcane_orb = new cooldown(MageSpells.arcane_orb);
            public static readonly cooldown arcane_power = new cooldown(MageSpells.arcane_power);
            public static readonly cooldown evocation = new cooldown(MageSpells.evocation);
            public static readonly cooldown prismatic_crystal = new cooldown(MageSpells.prismatic_crystal);

            #endregion

            #region Constructors

            private cooldown(string spell)
                : base(spell)
            {
            }

            #endregion
        }

        private class debuff : DebuffBase
        {
            #region Fields

            public static readonly debuff mark_of_doom = new debuff(MageSpells.mark_of_doom);

            #endregion

            #region Constructors

            private debuff(string spell)
                : base(spell)
            {
            }

            #endregion
        }

        private class glyph : GlyphBase
        {
            #region Fields

            public static readonly glyph arcane_explosion = new glyph(MageSpells.arcane_explosion);
            public static readonly glyph arcane_power = new glyph(MageSpells.arcane_power);
            public static readonly glyph cone_of_cold = new glyph(MageSpells.cone_of_cold);

            #endregion

            #region Constructors

            private glyph(string spellName)
                : base(spellName)
            {
            }

            #endregion
        }

        private class talent : TalentBase
        {
            #region Fields

            public static readonly talent arcane_orb = new talent(MageTalentsEnum.ArcaneOrb);
            public static readonly talent mirror_image = new talent(MageTalentsEnum.MirrorImage);
            public static readonly talent nether_tempest = new talent(MageTalentsEnum.NetherTempest);
            public static readonly talent overpowered = new talent(MageTalentsEnum.Overpowered);
            public static readonly talent prismatic_crystal = new talent(MageTalentsEnum.PrismaticCrystal);
            public static readonly talent rune_of_power = new talent(MageTalentsEnum.RuneOfPower);
            public static readonly talent supernova = new talent(MageTalentsEnum.Supernova);

            #endregion

            #region Constructors

            private talent(MageTalentsEnum talent)
                : base((int) talent)
            {
            }

            #endregion
        }

        #endregion
    }
}