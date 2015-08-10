using System;
using System.Linq;
using CommonBehaviors.Actions;
using Singular.ClassSpecific.Common;
using Singular.Dynamics;
using Singular.Helpers;
using Singular.Managers;
using Singular.Settings;
using Styx;
using Styx.TreeSharp;
using Styx.WoWInternals.WoWObjects;
using Action = Styx.TreeSharp.Action;

namespace Singular.ClassSpecific
{
    // ReSharper disable ClassNeverInstantiated.Global
    // ReSharper disable InconsistentNaming
    // ReSharper disable CompareOfFloatsByEqualityOperator
    public class DeathKnight : ClassSpecificBase
    {
        #region Fields

        private const byte BLOOD_BOIL_DISTANCE = 10;
        private const byte BLOOD_BOIL_GLYPH_DISTANCE = 15;
        private const byte DEATH_AND_DECAY_DISTANCE = 10;
        private const byte HOWLING_BLAST_DISTANCE = 10;

        private static readonly Func<Func<bool>, Composite> antimagic_shell = cond => 
            Spell.BuffSelf(DkSpells.antimagic_shell, req =>
                Spell.UseDefensiveCooldown &&
                SingularRoutine.Instance.ActiveEnemies.Any(u => u.IsCasting && u.CurrentTarget == Me && (!u.CanInterruptCurrentSpellCast || Spell.IsSpellOnCooldown(DkSpells.mind_freeze) || !Spell.CanCastHack(DkSpells.mind_freeze, u))) && cond());

        private static readonly Func<Func<bool>, Composite> blood_boil = cond => Spell.Cast(DkSpells.blood_boil, req => Spell.UseAoe && cond());
        private static readonly Func<Func<bool>, Composite> blood_tap = cond => Spell.Cast(DkSpells.blood_tap, req => talent.blood_tap.enabled && cond());
        private static readonly Func<Func<bool>, Composite> bone_shield = cond => Spell.BuffSelf(DkSpells.bone_shield, req => Spell.UseDefensiveCooldown && cond());
        private static readonly Func<Func<bool>, Composite> breath_of_sindragosa = cond => Spell.Buff(DkSpells.breath_of_sindragosa, req => talent.breath_of_sindragosa.enabled && !Me.HasAura(DkSpells.breath_of_sindragosa) && Spell.UseAoe && cond());
        private static readonly Func<Func<bool>, Composite> conversion = cond => Spell.BuffSelf(DkSpells.conversion, req => Spell.UseDefensiveCooldown && cond());
        private static readonly Func<Func<bool>, Composite> dancing_rune_weapon = cond => Spell.Cast(DkSpells.dancing_rune_weapon, req => Spell.UseCooldown && cond());
        private static readonly Func<Func<bool>, Composite> dark_transformation = cond => Spell.Cast(DkSpells.dark_transformation, on => Me.Pet, req => cond());
        private static readonly Func<Func<bool>, Composite> death_and_decay = cond => Spell.CastOnGround(DkSpells.death_and_decay, on => Me.CurrentTarget, req => Spell.UseAoe && cond());
        private static readonly Func<Func<bool>, Composite> death_coil = cond => Spell.Cast(DkSpells.death_coil, req => cond());
        private static readonly Func<Func<bool>, Composite> death_grip = cond => Spell.Cast(DkSpells.death_grip, req => cond());
        private static readonly Func<Func<bool>, Composite> death_pact = cond => Spell.BuffSelf(DkSpells.death_pact, req => talent.death_pact.enabled && Spell.UseDefensiveCooldown && cond());
        private static readonly Func<Func<bool>, Composite> death_strike = cond => Spell.Cast(DkSpells.death_strike, req => cond());
        private static readonly Func<Func<bool>, Composite> defile = cond => Spell.CastOnGround(DkSpells.defile, on => Me.CurrentTarget, req => talent.defile.enabled && Spell.UseAoe && cond());
        private static readonly Func<Func<bool>, Composite> empower_rune_weapon = cond => Spell.BuffSelf(DkSpells.empower_rune_weapon, req => Spell.UseCooldown && cond());
        private static readonly Func<Func<bool>, Composite> festering_strike = cond => Spell.Cast(DkSpells.festering_strike, req => cond());
        private static readonly Func<Func<bool>, Composite> frost_strike = cond => Spell.Cast(DkSpells.frost_strike, req => cond());
        private static readonly Func<Func<bool>, Composite> horn_of_winter = cond => Spell.BuffSelf(DkSpells.horn_of_winter, req => cond());
        private static readonly Func<Func<bool>, Composite> howling_blast = cond => Spell.Cast(DkSpells.howling_blast, req => cond());
        private static readonly Func<Func<bool>, Composite> icebound_fortitude = cond => Spell.BuffSelf(DkSpells.icebound_fortitude, req => Spell.UseDefensiveCooldown && cond());
        private static readonly Func<Func<bool>, Composite> icy_touch = cond => Spell.Cast(DkSpells.icy_touch, req => cond());
        private static readonly Func<Func<bool>, Composite> lichborne = cond => Spell.BuffSelf(DkSpells.lichborne, req => Spell.UseCooldown && talent.lichborne.enabled && cond());
        private static readonly Func<Func<bool>, Composite> obliterate = cond => Spell.Cast(DkSpells.obliterate, req => cond());
        private static readonly Func<Func<bool>, Composite> outbreak = cond => Spell.Cast(DkSpells.outbreak, req => cond());
        private static readonly Func<Func<bool>, Composite> pillar_of_frost = cond => Spell.BuffSelf(DkSpells.pillar_of_frost, req => Spell.UseCooldown && cond());
        private static readonly Func<Func<bool>, Composite> plague_leech = cond => Spell.Cast(DkSpells.plague_leech, req => talent.plague_leech.enabled && disease.ticking && cond());
        private static readonly Func<Func<bool>, Composite> plague_strike = cond => Spell.Cast(DkSpells.plague_strike, req => cond());
        private static readonly Func<Func<bool>, Composite> raise_dead = cond => Spell.Cast(DkSpells.raise_dead, req => StyxWoW.Me.GotAlivePet == false && SingularSettings.Instance.DisablePetUsage == false && cond());
        private static readonly Func<Func<bool>, Composite> rune_tap = cond => Spell.BuffSelf(DkSpells.rune_tap, req => Spell.UseDefensiveCooldown && cond());
        private static readonly Func<Func<bool>, Composite> scourge_strike = cond => Spell.Cast(DkSpells.scourge_strike, req => cond());
        private static readonly Func<Func<bool>, Composite> soul_reaper = cond => Spell.Cast(DkSpells.soul_reaper, req => cond());
        private static readonly Func<Func<bool>, Composite> summon_gargoyle = cond => Spell.Cast(DkSpells.summon_gargoyle, req => Spell.UseCooldown && cond());
        private static readonly Func<Func<bool>, Composite> unholy_blight = cond => Spell.Cast(DkSpells.unholy_blight, req => talent.unholy_blight.enabled && cond());
        private static readonly Func<Func<bool>, Composite> vampiric_blood = cond => Spell.BuffSelf(DkSpells.vampiric_blood, req => Spell.UseDefensiveCooldown && cond());

        #endregion

        #region Enums

        public enum DkTalentsEnum
        {
            // ReSharper disable UnusedMember.Local
            Plaguebearer = 1,
            PlagueLeech,
            UnholyBlight,

            Lichborne,
            AntiMagicZone,
            Purgatory,

            DeathsAdvance,
            Chilblains,
            Asphyxiate,

            BloodTap,
            RunicEmpowerment,
            RunicCorruption,

            DeathPact,
            DeathSiphon,
            Conversion,

            GorefiendsGrasp,
            RemorselessWinter,
            DesecratedGround,

            NecroticPlague,
            Defile,
            BreathOfSindragosa
            // ReSharper restore UnusedMember.Local
        }

        #endregion

        #region Properties

        public static int blood_death
        {
            get
            {
                var sum = 0;

                for (var i = 0; i < 2; i++)
                    if (Me.GetRuneCount(i) > 0 && Me.GetRuneType(i) == RuneType.Death)
                        ++sum;

                return sum;
            }
        }

        private static int Blood
        {
            get
            {
                var sum = 0;

                for (var i = 0; i < 6; i++)
                    if (Me.GetRuneCount(i) > 0 && (Me.GetRuneType(i) == RuneType.Blood || Me.GetRuneType(i) == RuneType.Death))
                        ++sum;

                return sum;
            }
        }

        private static byte BloodBoilDistance
        {
            get { return (TalentManager.HasGlyph(DkSpells.blood_boil) ? BLOOD_BOIL_GLYPH_DISTANCE : BLOOD_BOIL_DISTANCE); }
        }

        private static int Frost
        {
            get
            {
                var sum = 0;

                for (var i = 0; i < 6; i++)
                    if (Me.GetRuneCount(i) > 0 && (Me.GetRuneType(i) == RuneType.Frost || Me.GetRuneType(i) == RuneType.Death))
                        ++sum;

                return sum;
            }
        }

        private static int blood
        {
            get { return Me.GetRuneCount(0) + Me.GetRuneCount(1); }
        }

        private static int death
        {
            get { return Me.GetRuneCount(RuneType.Death); }
        }

        private static int frost
        {
            get { return Me.GetRuneCount(2) + Me.GetRuneCount(3); }
        }

        private static uint runic_power
        {
            get { return Me.CurrentRunicPower; }
        }

        private static int unholy
        {
            get { return Me.GetRuneCount(4) + Me.GetRuneCount(5); }
        }

        #endregion

        #region Public Methods

        [Behavior(BehaviorType.Combat, WoWClass.DeathKnight, WoWSpec.DeathKnightBlood)]
        public static Composite BloodActionList()
        {
            return new PrioritySelector(Helpers.Common.EnsureReadyToAttackFromMelee(), Spell.WaitForCastOrChannel(),
                new Decorator(ret => !Spell.IsGlobalCooldown(), new PrioritySelector(
                    SingularRoutine.MoveBehaviorInlineToCombat(BehaviorType.Heal),
                    SingularRoutine.MoveBehaviorInlineToCombat(BehaviorType.CombatBuffs),
                    Helpers.Common.CreateInterruptBehavior(),
                    Movement.WaitForFacing(),
                    Movement.WaitForLineOfSpellSight(),
                    use_trinket(),
                    //actions=auto_attack
                    //actions+=/potion,name=draenic_armor,if=buff.potion.down&buff.blood_shield.down&!unholy&!frost
                    // # if=time>10
                    //actions+=/blood_fury
                    blood_fury(() => true),
                    // # if=time>10
                    //actions+=/berserking
                    berserking(() => true),
                    // # if=time>10
                    //actions+=/arcane_torrent
                    arcane_torrent(() => runic_power < 20),
                    //actions+=/antimagic_shell
                    antimagic_shell(() => true),
                    //actions+=/conversion,if=!buff.conversion.up&runic_power>50&health.pct<90
                    conversion(() => !buff.conversion.up && runic_power > 50 && health.pct < 90),
                    //actions+=/lichborne,if=health.pct<90
                    lichborne(() => health.pct < 90),
                    //actions+=/death_strike,if=incoming_damage_5s>=health.max*0.65
                    death_strike(() => buff.blood_shield.remains < 2),
                    //actions+=/army_of_the_dead,if=buff.bone_shield.down&buff.dancing_rune_weapon.down&buff.icebound_fortitude.down&buff.vampiric_blood.down
                    //actions+=/bone_shield,if=buff.army_of_the_dead.down&buff.bone_shield.down&buff.dancing_rune_weapon.down&buff.icebound_fortitude.down&buff.vampiric_blood.down
                    bone_shield(() => buff.army_of_the_dead.down && buff.bone_shield.down && buff.dancing_rune_weapon.down && buff.icebound_fortitude.down && buff.vampiric_blood.down),
                    //actions+=/vampiric_blood,if=health.pct<50
                    vampiric_blood(() => health.pct < 50),
                    //actions+=/icebound_fortitude,if=health.pct<30&buff.army_of_the_dead.down&buff.dancing_rune_weapon.down&buff.bone_shield.down&buff.vampiric_blood.down
                    icebound_fortitude(() => health.pct < 30 && buff.army_of_the_dead.down && buff.dancing_rune_weapon.down && buff.bone_shield.down && buff.vampiric_blood.down),
                    //actions+=/rune_tap,if=health.pct<50&buff.army_of_the_dead.down&buff.dancing_rune_weapon.down&buff.bone_shield.down&buff.vampiric_blood.down&buff.icebound_fortitude.down
                    rune_tap(() => health.pct < 50 && buff.army_of_the_dead.down && buff.dancing_rune_weapon.down && buff.bone_shield.down && buff.vampiric_blood.down && buff.icebound_fortitude.down),
                    //actions+=/dancing_rune_weapon,if=health.pct<80&buff.army_of_the_dead.down&buff.icebound_fortitude.down&buff.bone_shield.down&buff.vampiric_blood.down
                    dancing_rune_weapon(() => true),
                    //actions+=/death_pact,if=health.pct<50
                    death_pact(() => health.pct < 50),
                    //actions+=/outbreak,if=(!talent.necrotic_plague.enabled&disease.min_remains<8)|!disease.ticking
                    outbreak(() => (!talent.necrotic_plague.enabled && disease.min_remains < 8) || !disease.ticking),
                    //actions+=/death_coil,if=runic_power>90
                    death_coil(() => runic_power > 90),
                    //actions+=/plague_strike,if=(!talent.necrotic_plague.enabled&!dot.blood_plague.ticking)|(talent.necrotic_plague.enabled&!dot.necrotic_plague.ticking)
                    plague_strike(() => (!talent.necrotic_plague.enabled && !dot.blood_plague.ticking) || (talent.necrotic_plague.enabled && !dot.necrotic_plague.ticking)),
                    //actions+=/icy_touch,if=(!talent.necrotic_plague.enabled&!dot.frost_fever.ticking)|(talent.necrotic_plague.enabled&!dot.necrotic_plague.ticking)
                    icy_touch(() => (!talent.necrotic_plague.enabled && !dot.frost_fever.ticking) || (talent.necrotic_plague.enabled && !dot.necrotic_plague.ticking)),
                    //actions+=/defile
                    defile(() => true),
                    //actions+=/plague_leech,if=((!blood&!unholy)|(!blood&!frost)|(!unholy&!frost))&cooldown.outbreak.remains<=gcd
                    plague_leech(() => ((!blood.ToBool() && !unholy.ToBool()) || (!blood.ToBool() && !frost.ToBool()) || (!unholy.ToBool() && !frost.ToBool())) && cooldown.outbreak.remains <= gcd),
                    //actions+=/call_action_list,name=bt,if=talent.blood_tap.enabled
                    new Decorator(req => talent.blood_tap.enabled, BloodBt()),
                    //actions+=/call_action_list,name=re,if=talent.runic_empowerment.enabled
                    new Decorator(req => talent.runic_empowerment.enabled, BloodRe()),
                    //actions+=/call_action_list,name=rc,if=talent.runic_corruption.enabled
                    new Decorator(req => talent.runic_corruption.enabled, BloodRc()),
                    //actions+=/call_action_list,name=nrt,if=!talent.blood_tap.enabled&!talent.runic_empowerment.enabled&!talent.runic_corruption.enabled
                    new Decorator(req => !talent.blood_tap.enabled && !talent.runic_empowerment.enabled && !talent.runic_corruption.enabled, BloodNrt()),
                    //actions+=/defile,if=buff.crimson_scourge.react
                    defile(() => buff.crimson_scourge.react),
                    //actions+=/death_and_decay,if=buff.crimson_scourge.react
                    death_and_decay(() => buff.crimson_scourge.react),
                    //actions+=/blood_boil,if=buff.crimson_scourge.react
                    blood_boil(() => buff.crimson_scourge.react),
                    //actions+=/death_coil
                    death_coil(() => true),
                    //actions+=/empower_rune_weapon,if=!blood&!unholy&!frost
                    empower_rune_weapon(() => !blood.ToBool() && !unholy.ToBool() && !frost.ToBool()),
                    new ActionAlwaysFail()
                    )));
        }

        [Behavior(BehaviorType.Pull, WoWClass.DeathKnight, WoWSpec.DeathKnightBlood, WoWContext.Instances)]
        public static Composite BloodInstancePull()
        {
            return BloodActionList();
        }

        [Behavior(BehaviorType.PreCombatBuffs | BehaviorType.PullBuffs, WoWClass.DeathKnight)]
        public static Composite Buffs()
        {
            return new PrioritySelector(
                bone_shield(() => Me.Specialization == WoWSpec.DeathKnightBlood),
                horn_of_winter(() => !Me.HasPartyBuff(PartyBuffType.AttackPower)),
                new ActionAlwaysFail()
                );
        }

        [Behavior(BehaviorType.Combat, WoWClass.DeathKnight, WoWSpec.DeathKnightFrost)]
        public static Composite FrostActionList()
        {
            return new PrioritySelector(Helpers.Common.EnsureReadyToAttackFromMelee(), Spell.WaitForCastOrChannel(),
                new Decorator(ret => !Spell.IsGlobalCooldown(), new PrioritySelector(
                    SingularRoutine.MoveBehaviorInlineToCombat(BehaviorType.Heal),
                    SingularRoutine.MoveBehaviorInlineToCombat(BehaviorType.CombatBuffs),
                    Helpers.Common.CreateInterruptBehavior(),
                    Movement.WaitForFacing(),
                    Movement.WaitForLineOfSpellSight(),
                    //actions=auto_attack
                    //actions+=/deaths_advance,if=movement.remains>2
                    //actions+=/antimagic_shell,damage=100000,if=((dot.breath_of_sindragosa.ticking&runic_power<25)|cooldown.breath_of_sindragosa.remains>40)|!talent.breath_of_sindragosa.enabled
                    antimagic_shell(() => true),
                    icebound_fortitude(() => health.pct < 50),
                    death_pact(() => health.pct < 30),
                    //actions+=/pillar_of_frost
                    pillar_of_frost(() => true),
                    //actions+=/potion,name=draenic_strength,if=target.time_to_die<=30|(target.time_to_die<=60&buff.pillar_of_frost.up)
                    //actions+=/empower_rune_weapon,if=target.time_to_die<=60&buff.potion.up
                    //actions+=/blood_fury
                    blood_fury(() => true),
                    //actions+=/berserking
                    berserking(() => true),
                    //actions+=/arcane_torrent
                    arcane_torrent(() => true),
                    //actions+=/use_item,slot=trinket2
                    use_trinket(),
                    //actions+=/plague_leech,if=disease.min_remains<1
                    plague_leech(() => disease.min_remains < 1),
                    //actions+=/soul_reaper,if=target.health.pct-3*(target.health.pct%target.time_to_die)<=35
                    soul_reaper(() => target.health.pct - 3 * (target.health.pct / target.time_to_die) <= 35),
                    //actions+=/blood_tap,if=(target.health.pct-3*(target.health.pct%target.time_to_die)<=35&cooldown.soul_reaper.remains=0)
                    blood_tap(() => (target.health.pct - 3 * (target.health.pct / target.time_to_die) <= 35 && cooldown.soul_reaper.remains == 0)),
                    //actions+=/run_action_list,name=single_target_2h,if=spell_targets.howling_blast<4&main_hand.2h
                    new Decorator(FrostSingleTarget2h(), req => spell_targets.howling_blast < 4 && main_hand._2h),
                    //actions+=/run_action_list,name=single_target_1h,if=spell_targets.howling_blast<3&main_hand.1h
                    new Decorator(FrostSingleTarget1h(), req => spell_targets.howling_blast < 3 && main_hand._1h),
                    //actions+=/run_action_list,name=multi_target,if=spell_targets.howling_blast>=3+main_hand.2h
                    new Decorator(FrostMultiTarget(), req => spell_targets.howling_blast >= (3 + main_hand._2h.ToInt())),
                    new ActionAlwaysFail()
                    )));
        }

        [Behavior(BehaviorType.Pull, WoWClass.DeathKnight, WoWSpec.DeathKnightFrost, WoWContext.Instances)]
        public static Composite FrostInstancePull()
        {
            return FrostActionList();
        }

        [Behavior(BehaviorType.Pull, WoWClass.DeathKnight, (WoWSpec) int.MaxValue, WoWContext.Normal | WoWContext.Battlegrounds)]
        public static Composite NormalAndPvPPull()
        {
            return new PrioritySelector(Helpers.Common.EnsureReadyToAttackFromMelee(), Spell.WaitForCastOrChannel(),
                new Decorator(req => !Spell.IsGlobalCooldown(), new PrioritySelector(
                    Helpers.Common.CreateInterruptBehavior(),
                    Movement.WaitForFacing(),
                    Movement.WaitForLineOfSpellSight(),
                    death_grip(
                        () =>
                            MovementManager.IsMovementDisabled == false && Me.CurrentTarget.IsBoss() == false && Me.CurrentTarget.DistanceSqr > 10 * 10 &&
                            (Me.CurrentTarget.IsPlayer || Me.CurrentTarget.TaggedByMe || (!Me.CurrentTarget.TaggedByOther && CompositeBuilder.CurrentBehaviorType == BehaviorType.Pull && SingularRoutine.CurrentWoWContext != WoWContext.Instances))),
                    new DecoratorContinue(req => Me.IsMoving, new Action(req => StopMoving.Now())),
                    new WaitContinue(1, until => !Me.GotTarget() || Me.CurrentTarget.IsWithinMeleeRange, new ActionAlwaysSucceed())
                    )
                    ),
                Movement.CreateMoveToMeleeBehavior(true)
                );
        }

        [Behavior(BehaviorType.Combat, WoWClass.DeathKnight, WoWSpec.DeathKnightUnholy)]
        public static Composite UnholyActionList()
        {
            return new PrioritySelector(Helpers.Common.EnsureReadyToAttackFromMelee(), Spell.WaitForCastOrChannel(),
                new Decorator(ret => !Spell.IsGlobalCooldown(), new PrioritySelector(
                    SingularRoutine.MoveBehaviorInlineToCombat(BehaviorType.Heal),
                    SingularRoutine.MoveBehaviorInlineToCombat(BehaviorType.CombatBuffs),
                    Helpers.Common.CreateInterruptBehavior(),
                    Movement.WaitForFacing(),
                    Movement.WaitForLineOfSpellSight(),
                    raise_dead(() => true),
                    use_trinket(),
                    //actions=auto_attack
                    //actions+=/deaths_advance,if=movement.remains>2
                    //actions+=/antimagic_shell,damage=100000,if=((dot.breath_of_sindragosa.ticking&runic_power<25)|cooldown.breath_of_sindragosa.remains>40)|!talent.breath_of_sindragosa.enabled
                    antimagic_shell(() => true),
                    icebound_fortitude(() => health.pct < 50),
                    death_pact(() => health.pct < 30),
                    //actions+=/blood_fury,if=!talent.breath_of_sindragosa.enabled
                    blood_fury(() => !talent.breath_of_sindragosa.enabled),
                    //actions+=/berserking,if=!talent.breath_of_sindragosa.enabled
                    berserking(() => !talent.breath_of_sindragosa.enabled),
                    //actions+=/arcane_torrent,if=!talent.breath_of_sindragosa.enabled
                    arcane_torrent(() => !talent.breath_of_sindragosa.enabled),
                    //actions+=/potion,name=draenic_strength,if=(buff.dark_transformation.up&target.time_to_die<=60)&!talent.breath_of_sindragosa.enabled
                    //actions+=/run_action_list,name=unholy
                    new Decorator(UnholyUnholy()),
                    new ActionAlwaysFail()
                    )
                    )
                );
        }

        [Behavior(BehaviorType.Pull, WoWClass.DeathKnight, WoWSpec.DeathKnightUnholy, WoWContext.Instances)]
        public static Composite UnholyInstancePull()
        {
            return UnholyActionList();
        }

        #endregion

        #region Private Methods

        private static Composite BloodBt()
        {
            return new PrioritySelector(
                //actions.bt=death_strike,if=unholy=2|frost=2
                death_strike(() => unholy == 2 || frost == 2),
                //actions.bt+=/blood_tap,if=buff.blood_charge.stack>=5&!blood
                blood_tap(() => buff.blood_charge.stack >= 5 && !blood.ToBool()),
                //actions.bt+=/death_strike,if=buff.blood_charge.stack>=10&unholy&frost
                death_strike(() => buff.blood_charge.stack >= 10 && unholy.ToBool() && frost.ToBool()),
                //actions.bt+=/blood_tap,if=buff.blood_charge.stack>=10&!unholy&!frost
                blood_tap(() => buff.blood_charge.stack >= 10 && !unholy.ToBool() && !frost.ToBool()),
                //actions.bt+=/blood_tap,if=buff.blood_charge.stack>=5&(!unholy|!frost)
                blood_tap(() => buff.blood_charge.stack >= 5 && (!unholy.ToBool() || !frost.ToBool())),
                //actions.bt+=/blood_tap,if=buff.blood_charge.stack>=5&blood.death&!unholy&!frost
                blood_tap(() => buff.blood_charge.stack >= 5 && blood_death.ToBool() && !unholy.ToBool() && !frost.ToBool()),
                //actions.bt+=/death_coil,if=runic_power>70
                death_coil(() => runic_power > 70),
                //actions.bt+=/soul_reaper,if=target.health.pct-3*(target.health.pct%target.time_to_die)<=35&(blood=2|(blood&!blood.death))
                soul_reaper(() => target.health.pct - 3 * (target.health.pct / target.time_to_die) <= 35 && (blood == 2 || (blood.ToBool() && !blood_death.ToBool()))),
                //actions.bt+=/blood_boil,if=blood=2|(blood&!blood.death)
                blood_boil(() => blood == 2 || (blood.ToBool() && !blood_death.ToBool())),
                new ActionAlwaysFail()
                );
        }

        private static Composite BloodNrt()
        {
            return new PrioritySelector(
                //actions.nrt=death_strike,if=unholy=2|frost=2
                death_strike(() => unholy == 2 || frost == 2),
                //actions.nrt+=/death_coil,if=runic_power>70
                death_coil(() => runic_power > 70),
                //actions.nrt+=/soul_reaper,if=target.health.pct-3*(target.health.pct%target.time_to_die)<=35&blood>=1
                soul_reaper(() => target.health.pct - 3 * (target.health.pct / target.time_to_die) <= 35 && blood >= 1),
                //actions.nrt+=/blood_boil,if=blood>=1
                blood_boil(() => blood >= 1),
                new ActionAlwaysFail()
                );
        }

        private static Composite BloodRc()
        {
            return new PrioritySelector(
                //actions.rc=death_strike,if=unholy=2|frost=2
                death_strike(() => unholy == 2 || frost == 2),
                //actions.rc+=/death_coil,if=runic_power>70
                death_coil(() => runic_power > 70),
                //actions.rc+=/soul_reaper,if=target.health.pct-3*(target.health.pct%target.time_to_die)<=35&blood>=1
                soul_reaper(() => target.health.pct - 3 * (target.health.pct / target.time_to_die) <= 35 && blood >= 1),
                //actions.rc+=/blood_boil,if=blood=2
                blood_boil(() => blood == 2),
                new ActionAlwaysFail()
                );
        }

        private static Composite BloodRe()
        {
            return new PrioritySelector(
                //actions.re=death_strike,if=unholy&frost
                death_strike(() => unholy > 0 && frost > 0),
                //actions.re+=/death_coil,if=runic_power>70
                death_coil(() => runic_power > 70),
                //actions.re+=/soul_reaper,if=target.health.pct-3*(target.health.pct%target.time_to_die)<=35&blood=2
                soul_reaper(() => target.health.pct - 3 * (target.health.pct / target.time_to_die) <= 35 && blood == 2),
                //actions.re+=/blood_boil,if=blood=2
                blood_boil(() => blood == 2),
                new ActionAlwaysFail()
                );
        }

        private static Composite FrostMultiTarget()
        {
            return new PrioritySelector(
                //actions.multi_target=unholy_blight
                unholy_blight(() => true),
                //actions.multi_target+=/frost_strike,if=buff.killing_machine.react&main_hand.1h
                frost_strike(() => buff.killing_machine.react && main_hand._1h),
                //actions.multi_target+=/obliterate,if=unholy>1
                obliterate(() => unholy > 1),
                //actions.multi_target+=/blood_boil,if=dot.blood_plague.ticking&(!talent.unholy_blight.enabled|cooldown.unholy_blight.remains<49),line_cd=28
                blood_boil(() => Enemies(BloodBoilDistance).Any(x => !dot.blood_plague.Ticking(x)) && dot.blood_plague.ticking && (!talent.unholy_blight.enabled || cooldown.unholy_blight.remains < 49)),
                //actions.multi_target+=/defile
                defile(() => true),
                //actions.multi_target+=/breath_of_sindragosa,if=runic_power>75
                breath_of_sindragosa(() => runic_power > 75),
                //actions.multi_target+=/run_action_list,name=multi_target_bos,if=dot.breath_of_sindragosa.ticking
                new Decorator(FrostMultiTargetBos(), req => dot.breath_of_sindragosa.ticking),
                //actions.multi_target+=/howling_blast
                howling_blast(() => true),
                //actions.multi_target+=/blood_tap,if=buff.blood_charge.stack>10
                blood_tap(() => buff.blood_charge.stack > 10),
                //actions.multi_target+=/frost_strike,if=runic_power>88
                frost_strike(() => runic_power > 88),
                //actions.multi_target+=/death_and_decay,if=unholy=1
                death_and_decay(() => unholy == 1),
                //actions.multi_target+=/plague_strike,if=unholy=2&!dot.blood_plague.ticking&!talent.necrotic_plague.enabled
                plague_strike(() => unholy == 2 && !dot.blood_plague.ticking && !talent.necrotic_plague.enabled),
                //actions.multi_target+=/blood_tap
                blood_tap(() => true),
                //actions.multi_target+=/frost_strike,if=!talent.breath_of_sindragosa.enabled|cooldown.breath_of_sindragosa.remains>=10
                frost_strike(() => !talent.breath_of_sindragosa.enabled || cooldown.breath_of_sindragosa.remains >= 10),
                //actions.multi_target+=/plague_leech
                plague_leech(() => true),
                //actions.multi_target+=/plague_strike,if=unholy=1
                plague_strike(() => unholy == 1),
                //actions.multi_target+=/empower_rune_weapon
                empower_rune_weapon(() => true),
                new ActionAlwaysFail()
                );
        }

        private static Composite FrostMultiTargetBos()
        {
            return new PrioritySelector(
                //actions.multi_target_bos=howling_blast
                howling_blast(() => true),
                //actions.multi_target_bos+=/blood_tap,if=buff.blood_charge.stack>10
                blood_tap(() => buff.blood_charge.stack > 10),
                //actions.multi_target_bos+=/death_and_decay,if=unholy=1
                death_and_decay(() => unholy == 1),
                //actions.multi_target_bos+=/plague_strike,if=unholy=2
                plague_strike(() => unholy == 2),
                //actions.multi_target_bos+=/blood_tap
                blood_tap(() => true),
                //actions.multi_target_bos+=/plague_leech
                plague_leech(() => true),
                //actions.multi_target_bos+=/plague_strike,if=unholy=1
                plague_strike(() => unholy == 1),
                //actions.multi_target_bos+=/empower_rune_weapon
                empower_rune_weapon(() => true),
                new ActionAlwaysFail()
                );
        }

        private static Composite FrostSingleTarget1h()
        {
            return new PrioritySelector(
                //actions.single_target_1h=breath_of_sindragosa,if=runic_power>75
                breath_of_sindragosa(() => runic_power > 75),
                //actions.single_target_1h+=/run_action_list,name=single_target_bos,if=dot.breath_of_sindragosa.ticking
                new Decorator(FrostSingleTargetBos(), req => dot.breath_of_sindragosa.ticking),
                //actions.single_target_1h+=/frost_strike,if=buff.killing_machine.react
                frost_strike(() => buff.killing_machine.react),
                //actions.single_target_1h+=/obliterate,if=unholy>1|buff.killing_machine.react
                obliterate(() => unholy > 1 || buff.killing_machine.react),
                //actions.single_target_1h+=/defile
                defile(() => true),
                //actions.single_target_1h+=/blood_tap,if=talent.defile.enabled&cooldown.defile.remains=0
                blood_tap(() => talent.defile.enabled && cooldown.defile.remains == 0),
                //actions.single_target_1h+=/frost_strike,if=runic_power>88
                frost_strike(() => runic_power > 88),
                //actions.single_target_1h+=/howling_blast,if=buff.rime.react|death>1|frost>1
                howling_blast(() => buff.rime.react || death > 1 || frost > 1),
                //actions.single_target_1h+=/blood_tap,if=buff.blood_charge.stack>10
                blood_tap(() => buff.blood_charge.stack > 10),
                //actions.single_target_1h+=/frost_strike,if=runic_power>76
                frost_strike(() => runic_power > 76),
                //actions.single_target_1h+=/unholy_blight,if=!disease.ticking
                unholy_blight(() => !disease.ticking),
                //actions.single_target_1h+=/outbreak,if=!dot.blood_plague.ticking
                outbreak(() => !dot.blood_plague.ticking),
                //actions.single_target_1h+=/plague_strike,if=!talent.necrotic_plague.enabled&!dot.blood_plague.ticking
                plague_strike(() => !talent.necrotic_plague.enabled && !dot.blood_plague.ticking),
                //actions.single_target_1h+=/howling_blast,if=!(target.health.pct-3*(target.health.pct%target.time_to_die)<=35&cooldown.soul_reaper.remains<3)|death+frost>=2
                howling_blast(() => !(target.health.pct - 3 * (target.health.pct / target.time_to_die) <= 35 && cooldown.soul_reaper.remains < 3) || death + frost >= 2),
                //actions.single_target_1h+=/outbreak,if=talent.necrotic_plague.enabled&debuff.necrotic_plague.stack<=14
                outbreak(() => talent.necrotic_plague.enabled && debuff.necrotic_plague.stack <= 14),
                //actions.single_target_1h+=/blood_tap
                blood_tap(() => true),
                //actions.single_target_1h+=/plague_leech
                plague_leech(() => true),
                //actions.single_target_1h+=/empower_rune_weapon
                empower_rune_weapon(() => true),
                new ActionAlwaysFail()
                );
        }

        private static Composite FrostSingleTarget2h()
        {
            return new PrioritySelector(
                //actions.single_target_2h=defile
                defile(() => true),
                //actions.single_target_2h+=/blood_tap,if=talent.defile.enabled&cooldown.defile.remains=0
                blood_tap(() => talent.defile.enabled && cooldown.defile.remains == 0),
                //actions.single_target_2h+=/howling_blast,if=buff.rime.react&disease.min_remains>5&buff.killing_machine.react
                howling_blast(() => buff.rime.react && disease.min_remains > 5 && buff.killing_machine.react),
                //actions.single_target_2h+=/obliterate,if=buff.killing_machine.react
                obliterate(() => buff.killing_machine.react),
                //actions.single_target_2h+=/blood_tap,if=buff.killing_machine.react
                blood_tap(() => buff.killing_machine.react),
                //actions.single_target_2h+=/howling_blast,if=!talent.necrotic_plague.enabled&!dot.frost_fever.ticking&buff.rime.react
                howling_blast(() => !talent.necrotic_plague.enabled && !dot.frost_fever.ticking && buff.rime.react),
                //actions.single_target_2h+=/outbreak,if=!disease.max_ticking
                outbreak(() => !disease.max_ticking),
                //actions.single_target_2h+=/unholy_blight,if=!disease.min_ticking
                unholy_blight(() => !disease.min_ticking),
                //actions.single_target_2h+=/breath_of_sindragosa,if=runic_power>75
                breath_of_sindragosa(() => runic_power > 75),
                //actions.single_target_2h+=/run_action_list,name=single_target_bos,if=dot.breath_of_sindragosa.ticking
                new Decorator(FrostSingleTargetBos(), req => dot.breath_of_sindragosa.ticking),
                //actions.single_target_2h+=/obliterate,if=talent.breath_of_sindragosa.enabled&cooldown.breath_of_sindragosa.remains<7&runic_power<76
                obliterate(() => talent.breath_of_sindragosa.enabled && cooldown.breath_of_sindragosa.remains < 7 && runic_power < 76),
                //actions.single_target_2h+=/howling_blast,if=talent.breath_of_sindragosa.enabled&cooldown.breath_of_sindragosa.remains<3&runic_power<88
                howling_blast(() => talent.breath_of_sindragosa.enabled && cooldown.breath_of_sindragosa.remains < 3 && runic_power < 88),
                //actions.single_target_2h+=/howling_blast,if=!talent.necrotic_plague.enabled&!dot.frost_fever.ticking
                howling_blast(() => !talent.necrotic_plague.enabled && !dot.frost_fever.ticking),
                //actions.single_target_2h+=/howling_blast,if=talent.necrotic_plague.enabled&!dot.necrotic_plague.ticking
                howling_blast(() => talent.necrotic_plague.enabled && !dot.necrotic_plague.ticking),
                //actions.single_target_2h+=/plague_strike,if=!talent.necrotic_plague.enabled&!dot.blood_plague.ticking
                plague_strike(() => !talent.necrotic_plague.enabled && !dot.blood_plague.ticking),
                //actions.single_target_2h+=/blood_tap,if=buff.blood_charge.stack>10&runic_power>76
                blood_tap(() => buff.blood_charge.stack > 10 && runic_power > 76),
                //actions.single_target_2h+=/frost_strike,if=runic_power>76
                frost_strike(() => runic_power > 76),
                //actions.single_target_2h+=/howling_blast,if=buff.rime.react&disease.min_remains>5&(blood.frac>=1.8|unholy.frac>=1.8|frost.frac>=1.8)
                howling_blast(() => buff.rime.react && disease.min_remains > 5 && (blood >= 1 || unholy >= 1 || frost >= 1)),
                //actions.single_target_2h+=/obliterate,if=blood.frac>=1.8|unholy.frac>=1.8|frost.frac>=1.8
                obliterate(() => blood >= 1 || unholy >= 1 || frost >= 1),
                //actions.single_target_2h+=/plague_leech,if=disease.min_remains<3&((blood.frac<=0.95&unholy.frac<=0.95)|(frost.frac<=0.95&unholy.frac<=0.95)|(frost.frac<=0.95&blood.frac<=0.95))
                plague_leech(() => disease.min_remains < 3 && ((blood <= 1 && unholy <= 1) || (frost <= 1 && unholy <= 1) || (frost <= 1 && blood <= 1))),
                //actions.single_target_2h+=/frost_strike,if=talent.runic_empowerment.enabled&(frost=0|unholy=0|blood=0)&(!buff.killing_machine.react|!obliterate.ready_in<=1)
                frost_strike(() => talent.runic_empowerment.enabled && (frost == 0 || unholy == 0 || blood == 0) && (!buff.killing_machine.react)),
                //actions.single_target_2h+=/frost_strike,if=talent.blood_tap.enabled&buff.blood_charge.stack<=10&(!buff.killing_machine.react|!obliterate.ready_in<=1)
                frost_strike(() => talent.blood_tap.enabled && buff.blood_charge.stack <= 10 && (!buff.killing_machine.react)),
                //actions.single_target_2h+=/howling_blast,if=buff.rime.react&disease.min_remains>5
                howling_blast(() => buff.rime.react && disease.min_remains > 5),
                //actions.single_target_2h+=/obliterate,if=blood.frac>=1.5|unholy.frac>=1.6|frost.frac>=1.6|buff.bloodlust.up|cooldown.plague_leech.remains<=4
                obliterate(() => blood >= 2 || unholy >= 2 || frost >= 2 || buff.bloodlust.up || cooldown.plague_leech.remains <= 4),
                //actions.single_target_2h+=/blood_tap,if=(buff.blood_charge.stack>10&runic_power>=20)|(blood.frac>=1.4|unholy.frac>=1.6|frost.frac>=1.6)
                blood_tap(() => (buff.blood_charge.stack > 10 && runic_power >= 20) || (blood >= 2 || unholy >= 2 || frost >= 2)),
                //actions.single_target_2h+=/frost_strike,if=!buff.killing_machine.react
                frost_strike(() => !buff.killing_machine.react),
                //actions.single_target_2h+=/plague_leech,if=(blood.frac<=0.95&unholy.frac<=0.95)|(frost.frac<=0.95&unholy.frac<=0.95)|(frost.frac<=0.95&blood.frac<=0.95)
                plague_leech(() => (blood <= 1 && unholy <= 1) || (frost <= 1 && unholy <= 1) || (frost <= 1 && blood <= 1)),
                //actions.single_target_2h+=/empower_rune_weapon
                empower_rune_weapon(() => true),
                new ActionAlwaysFail()
                );
        }

        private static Composite FrostSingleTargetBos()
        {
            return new PrioritySelector(
                //actions.single_target_bos=obliterate,if=buff.killing_machine.react
                obliterate(() => buff.killing_machine.react),
                //actions.single_target_bos+=/blood_tap,if=buff.killing_machine.react&buff.blood_charge.stack>=5
                blood_tap(() => buff.killing_machine.react && buff.blood_charge.stack >= 5),
                //actions.single_target_bos+=/plague_leech,if=buff.killing_machine.react
                plague_leech(() => buff.killing_machine.react),
                //actions.single_target_bos+=/blood_tap,if=buff.blood_charge.stack>=5
                blood_tap(() => buff.blood_charge.stack >= 5),
                //actions.single_target_bos+=/plague_leech
                plague_leech(() => true),
                //actions.single_target_bos+=/obliterate,if=runic_power<76
                obliterate(() => runic_power < 76),
                //actions.single_target_bos+=/howling_blast,if=((death=1&frost=0&unholy=0)|death=0&frost=1&unholy=0)&runic_power<88
                howling_blast(() => ((death == 1 && frost == 0 && unholy == 0) || death == 0 && frost == 1 && unholy == 0) && runic_power < 88),
                new ActionAlwaysFail()
                );
        }

        private static Composite UnholyBos()
        {
            return new PrioritySelector(
                //actions.bos=blood_fury,if=dot.breath_of_sindragosa.ticking
                blood_fury(() => dot.breath_of_sindragosa.ticking),
                //actions.bos+=/berserking,if=dot.breath_of_sindragosa.ticking
                berserking(() => dot.breath_of_sindragosa.ticking),
                //actions.bos+=/potion,name=draenic_strength,if=dot.breath_of_sindragosa.ticking
                //actions.bos+=/unholy_blight,if=!disease.ticking
                unholy_blight(() => !disease.ticking),
                //actions.bos+=/plague_strike,if=!disease.ticking
                plague_strike(() => !disease.ticking),
                //actions.bos+=/blood_boil,cycle_targets=1,if=(spell_targets.blood_boil>=2&!(dot.blood_plague.ticking|dot.frost_fever.ticking))|spell_targets.blood_boil>=4&(runic_power<88&runic_power>30)
                blood_boil(() => (spell_targets.blood_boil >= 2 && Enemies(BloodBoilDistance).Any(x => !(dot.blood_plague.Ticking(x) || dot.frost_fever.Ticking(x)))) || spell_targets.blood_boil >= 4 && (runic_power < 88 && runic_power > 30)),
                //actions.bos+=/death_and_decay,if=spell_targets.death_and_decay>=2&(runic_power<88&runic_power>30)
                death_and_decay(() => spell_targets.death_and_decay >= 2 && (runic_power < 88 && runic_power > 30)),
                //actions.bos+=/festering_strike,if=(blood=2&frost=2&(((Frost-death)>0)|((Blood-death)>0)))&runic_power<80
                festering_strike(() => (blood == 2 && frost == 2 && (((Frost - death) > 0) || ((Blood - death) > 0))) && runic_power < 80),
                //actions.bos+=/festering_strike,if=((blood=2|frost=2)&(((Frost-death)>0)&((Blood-death)>0)))&runic_power<80
                festering_strike(() => ((blood == 2 || frost == 2) && (((Frost - death) > 0) && ((Blood - death) > 0))) && runic_power < 80),
                //actions.bos+=/arcane_torrent,if=runic_power<70
                arcane_torrent(() => runic_power < 70),
                //actions.bos+=/scourge_strike,if=spell_targets.blood_boil<=3&(runic_power<88&runic_power>30)
                scourge_strike(() => spell_targets.blood_boil <= 3 && (runic_power < 88 && runic_power > 30)),
                //actions.bos+=/blood_boil,if=spell_targets.blood_boil>=4&(runic_power<88&runic_power>30)
                blood_boil(() => spell_targets.blood_boil >= 4 && (runic_power < 88 && runic_power > 30)),
                //actions.bos+=/festering_strike,if=runic_power<77
                festering_strike(() => runic_power < 77),
                //actions.bos+=/scourge_strike,if=(spell_targets.blood_boil>=4&(runic_power<88&runic_power>30))|spell_targets.blood_boil<=3
                scourge_strike(() => (spell_targets.blood_boil >= 4 && (runic_power < 88 && runic_power > 30)) || spell_targets.blood_boil <= 3),
                //actions.bos+=/dark_transformation
                dark_transformation(() => true),
                //actions.bos+=/blood_tap,if=buff.blood_charge.stack>=5
                blood_tap(() => buff.blood_charge.stack >= 5),
                //actions.bos+=/plague_leech
                plague_leech(() => true),
                //actions.bos+=/empower_rune_weapon,if=runic_power<60
                empower_rune_weapon(() => runic_power < 60),
                //actions.bos+=/death_coil,if=buff.sudden_doom.react
                death_coil(() => buff.sudden_doom.react),
                new ActionAlwaysFail()
                );
        }

        private static Composite UnholyUnholy()
        {
            return new PrioritySelector(
                //actions.unholy=plague_leech,if=((cooldown.outbreak.remains<1)|disease.min_remains<1)&((blood<1&frost<1)|(blood<1&unholy<1)|(frost<1&unholy<1))
                plague_leech(() => ((cooldown.outbreak.remains < 1) || disease.min_remains < 1) && ((blood < 1 && frost < 1) || (blood < 1 && unholy < 1) || (frost < 1 && unholy < 1))),
                //actions.unholy+=/soul_reaper,if=(target.health.pct-3*(target.health.pct%target.time_to_die))<=45
                soul_reaper(() => (target.health.pct - 3 * (target.health.pct / target.time_to_die)) <= 45),
                //actions.unholy+=/blood_tap,if=((target.health.pct-3*(target.health.pct%target.time_to_die))<=45)&cooldown.soul_reaper.remains=0
                blood_tap(() => ((target.health.pct - 3 * (target.health.pct / target.time_to_die)) <= 45) && cooldown.soul_reaper.remains == 0),
                //actions.unholy+=/summon_gargoyle
                summon_gargoyle(() => true),
                //actions.unholy+=/breath_of_sindragosa,if=runic_power>75
                breath_of_sindragosa(() => runic_power > 75),
                //actions.unholy+=/run_action_list,name=bos,if=dot.breath_of_sindragosa.ticking
                new Decorator(UnholyBos(), req => talent.breath_of_sindragosa.enabled && dot.breath_of_sindragosa.ticking),
                //actions.unholy+=/unholy_blight,if=!disease.min_ticking
                unholy_blight(() => !disease.min_ticking),
                //actions.unholy+=/outbreak,cycle_targets=1,if=!talent.necrotic_plague.enabled&(!(dot.blood_plague.ticking|dot.frost_fever.ticking))
                outbreak(() => !talent.necrotic_plague.enabled && (!(dot.blood_plague.ticking || dot.frost_fever.ticking))),
                //actions.unholy+=/plague_strike,if=(!talent.necrotic_plague.enabled&!(dot.blood_plague.ticking|dot.frost_fever.ticking))|(talent.necrotic_plague.enabled&!dot.necrotic_plague.ticking)
                plague_strike(() => (!talent.necrotic_plague.enabled && !(dot.blood_plague.ticking || dot.frost_fever.ticking)) || (talent.necrotic_plague.enabled && !dot.necrotic_plague.ticking)),
                //actions.unholy+=/blood_boil,cycle_targets=1,if=(spell_targets.blood_boil>1&!talent.necrotic_plague.enabled)&(!(dot.blood_plague.ticking|dot.frost_fever.ticking))
                blood_boil(() => (spell_targets.blood_boil > 1 && !talent.necrotic_plague.enabled) && Enemies(BloodBoilDistance).Any(x => (!(dot.blood_plague.Ticking(x) || dot.frost_fever.Ticking(x))))),
                //actions.unholy+=/death_and_decay,if=spell_targets.death_and_decay>1&unholy>1
                death_and_decay(() => spell_targets.death_and_decay > 1 && unholy > 1),
                //actions.unholy+=/defile,if=unholy=2
                defile(() => unholy == 2),
                //actions.unholy+=/blood_tap,if=talent.defile.enabled&cooldown.defile.remains=0
                blood_tap(() => talent.defile.enabled && cooldown.defile.remains == 0),
                //actions.unholy+=/scourge_strike,if=unholy=2
                scourge_strike(() => unholy == 2),
                //actions.unholy+=/festering_strike,if=talent.necrotic_plague.enabled&talent.unholy_blight.enabled&dot.necrotic_plague.remains<cooldown.unholy_blight.remains%2
                festering_strike(() => talent.necrotic_plague.enabled && talent.unholy_blight.enabled && dot.necrotic_plague.remains < cooldown.unholy_blight.remains / 2),
                //actions.unholy+=/dark_transformation
                dark_transformation(() => true),
                //actions.unholy+=/festering_strike,if=blood=2&frost=2&(((Frost-death)>0)|((Blood-death)>0))
                festering_strike(() => blood == 2 && frost == 2 && (((Frost - death) > 0) || ((Blood - death) > 0))),
                //actions.unholy+=/festering_strike,if=(blood=2|frost=2)&(((Frost-death)>0)&((Blood-death)>0))
                festering_strike(() => (blood == 2 || frost == 2) && (((Frost - death) > 0) && ((Blood - death) > 0))),
                //actions.unholy+=/blood_boil,cycle_targets=1,if=(talent.necrotic_plague.enabled&!dot.necrotic_plague.ticking)&spell_targets.blood_boil>1
                blood_boil(() => (talent.necrotic_plague.enabled && Enemies(BloodBoilDistance).Any(x => !dot.necrotic_plague.Ticking(x))) && spell_targets.blood_boil > 1),
                //actions.unholy+=/defile,if=blood=2|frost=2
                defile(() => blood == 2 || frost == 2),
                //actions.unholy+=/death_and_decay,if=spell_targets.death_and_decay>1
                death_and_decay(() => spell_targets.death_and_decay > 1),
                //actions.unholy+=/defile
                defile(() => true),
                //actions.unholy+=/blood_boil,if=talent.breath_of_sindragosa.enabled&((spell_targets.blood_boil>=4&(blood=2|(frost=2&death=2)))&(cooldown.breath_of_sindragosa.remains>6|runic_power<75))
                blood_boil(() => talent.breath_of_sindragosa.enabled && ((spell_targets.blood_boil >= 4 && (blood == 2 || (frost == 2 && death == 2))) && (cooldown.breath_of_sindragosa.remains > 6 || runic_power < 75))),
                //actions.unholy+=/blood_boil,if=!talent.breath_of_sindragosa.enabled&(spell_targets.blood_boil>=4&(blood=2|(frost=2&death=2)))
                blood_boil(() => !talent.breath_of_sindragosa.enabled && (spell_targets.blood_boil >= 4 && (blood == 2 || (frost == 2 && death == 2)))),
                //actions.unholy+=/blood_tap,if=buff.blood_charge.stack>10
                blood_tap(() => buff.blood_charge.stack > 10),
                //actions.unholy+=/outbreak,if=talent.necrotic_plague.enabled&debuff.necrotic_plague.stack<=14
                outbreak(() => talent.necrotic_plague.enabled && debuff.necrotic_plague.stack <= 14),
                //actions.unholy+=/death_coil,if=(buff.sudden_doom.react|runic_power>80)&(buff.blood_charge.stack<=10)
                death_coil(() => (buff.sudden_doom.react || runic_power > 80) && (buff.blood_charge.stack <= 10)),
                //actions.unholy+=/blood_boil,if=(spell_targets.blood_boil>=4&(cooldown.breath_of_sindragosa.remains>6|runic_power<75))|(!talent.breath_of_sindragosa.enabled&spell_targets.blood_boil>=4)
                blood_boil(() => (spell_targets.blood_boil >= 4 && (cooldown.breath_of_sindragosa.remains > 6 || runic_power < 75)) || (!talent.breath_of_sindragosa.enabled && spell_targets.blood_boil >= 4)),
                //actions.unholy+=/scourge_strike,if=(cooldown.breath_of_sindragosa.remains>6|runic_power<75|unholy=2)|!talent.breath_of_sindragosa.enabled
                scourge_strike(() => (cooldown.breath_of_sindragosa.remains > 6 || runic_power < 75 || unholy == 2) || !talent.breath_of_sindragosa.enabled),
                //actions.unholy+=/festering_strike,if=(cooldown.breath_of_sindragosa.remains>6|runic_power<75)|!talent.breath_of_sindragosa.enabled
                festering_strike(() => (cooldown.breath_of_sindragosa.remains > 6 || runic_power < 75) || !talent.breath_of_sindragosa.enabled),
                //actions.unholy+=/death_coil,if=(cooldown.breath_of_sindragosa.remains>20)|!talent.breath_of_sindragosa.enabled
                death_coil(() => (cooldown.breath_of_sindragosa.remains > 20) || !talent.breath_of_sindragosa.enabled),
                //actions.unholy+=/plague_leech
                plague_leech(() => true),
                //actions.unholy+=/empower_rune_weapon,if=!talent.breath_of_sindragosa.enabled
                empower_rune_weapon(() => !talent.breath_of_sindragosa.enabled),
                new ActionAlwaysFail()
                );
        }

        #endregion

        // ReSharper disable MemberHidesStaticFromOuterClass
        // ReSharper disable UnusedMember.Local

        #region Types

        public static class DkSpells
        {
            // ReSharper disable UnusedMember.Local

            #region Fields

            public const string antimagic_shell = "Anti-Magic Shell";
            public const string army_of_the_dead = "Army of the Dead";
            public const string blood_boil = "Blood Boil";
            public const string blood_charge = "Blood Charge";
            public const string blood_plague = "Blood Plague";
            public const string blood_shield = "Blood Shield";
            public const string blood_tap = "Blood Tap";
            public const string bone_shield = "Bone Shield";
            public const string breath_of_sindragosa = "Breath of Sindragosa";
            public const string conversion = "Conversion";
            public const int crimson_scourge = 81141;
            public const string dancing_rune_weapon = "Dancing Rune Weapon";
            public const string dark_transformation = "Dark Transformation";
            public const string death_and_decay = "Death and Decay";
            public const string death_coil = "Death Coil";
            public const string death_grip = "Death Grip";
            public const string death_pact = "Death Pact";
            public const string death_strike = "Death Strike";
            public const string defile = "Defile";
            public const string empower_rune_weapon = "Empower Rune Weapon";
            public const string festering_strike = "Festering Strike";
            public const int freezing_fog = 59052;
            public const string frost_fever = "Frost Fever";
            public const string frost_strike = "Frost Strike";
            public const string horn_of_winter = "Horn of Winter";
            public const string howling_blast = "Howling Blast";
            public const string icebound_fortitude = "Icebound Fortitude";
            public const string icy_touch = "Icy Touch";
            public const int killing_machine = 51124;
            public const string lichborne = "Lichborne";
            public const string mind_freeze = "Mind Freeze";
            public const string necrotic_plague = "Necrotic Plague";
            public const string obliterate = "Obliterate";
            public const string outbreak = "Outbreak";
            public const string pillar_of_frost = "Pillar of Frost";
            public const string plague_leech = "Plague Leech";
            public const string plague_strike = "Plague Strike";
            public const string raise_dead = "Raise Dead";
            public const string rune_tap = "Rune Tap";
            public const string runic_empowerment = "Runic Empowerment";
            public const string scourge_strike = "Scourge Strike";
            public const string shadow_infusion = "Shadow Infusion";
            public const string soul_reaper = "Soul Reaper";
            public const int sudden_doom = 81340;
            public const string summon_gargoyle = "Summon Gargoyle";
            public const string unholy_blight = "Unholy Blight";
            public const string vampiric_blood = "Vampiric Blood";

            #endregion

            // ReSharper restore UnusedMember.Local
        }

        private static class disease
        {
            #region Fields

            private static readonly string[] listBase = {DkSpells.blood_plague, DkSpells.frost_fever};
            private static readonly string[] listWithNecroticPlague = {DkSpells.necrotic_plague};

            #endregion

            #region Properties

            public static double max_remains
            {
                get { return max_remains_on(StyxWoW.Me.CurrentTarget); }
            }

            public static bool max_ticking
            {
                get { return max_ticking_on(StyxWoW.Me.CurrentTarget); }
            }

            public static double min_remains
            {
                get { return min_remains_on(StyxWoW.Me.CurrentTarget); }
            }

            public static bool min_ticking
            {
                get { return ticking; }
            }

            public static bool ticking
            {
                get { return ticking_on(StyxWoW.Me.CurrentTarget); }
            }

            private static string[] diseaseArray
            {
                get { return talent.necrotic_plague.enabled ? listWithNecroticPlague : listBase; }
            }

            #endregion

            #region Private Methods

            private static double max_remains_on(WoWUnit unit)
            {
                if (unit == null) return 0;

                var max = double.MinValue;

                // ReSharper disable once LoopCanBeConvertedToQuery
                foreach (var s in diseaseArray)
                {
                    var rmn = unit.GetAuraTimeLeft(s).TotalSeconds;
                    if (rmn > max)
                        max = rmn;
                }

                if (max <= double.MinValue)
                    max = 0;

                return max;
            }

            private static bool max_ticking_on(WoWUnit unit)
            {
                if (unit == null) return false;

                return unit.HasAnyOfMyAuras(diseaseArray);
            }

            private static double min_remains_on(WoWUnit unit)
            {
                if (unit == null) return 0;

                var min = double.MaxValue;

                // ReSharper disable once LoopCanBeConvertedToQuery
                foreach (var s in diseaseArray)
                {
                    var rmn = unit.GetAuraTimeLeft(s).TotalSeconds;
                    if (rmn < min)
                        min = rmn;
                }

                if (min >= double.MaxValue)
                    min = 0;

                return min;
            }

            private static bool ticking_on(WoWUnit unit)
            {
                if (unit == null) return false;

                return unit.HasAllMyAuras(diseaseArray);
            }

            #endregion
        }

        private static class spell_targets
        {
            #region Properties

            public static int blood_boil
            {
                get { return EnemiesCountNearTarget(Me, BloodBoilDistance); }
            }

            public static int death_and_decay
            {
                get { return EnemiesCountNearTarget(Me.CurrentTarget, DEATH_AND_DECAY_DISTANCE); }
            }

            public static int howling_blast
            {
                get { return EnemiesCountNearTarget(Me.CurrentTarget, HOWLING_BLAST_DISTANCE); }
            }

            #endregion
        }

        private class buff : BuffBase
        {
            #region Fields

            public static readonly buff army_of_the_dead = new buff(DkSpells.army_of_the_dead);
            public static readonly buff blood_charge = new buff(DkSpells.blood_charge);
            public static readonly buff blood_shield = new buff(DkSpells.blood_shield);
            public static readonly buff bloodlust = new buff(ClassSpecificBase.bloodlust);
            public static readonly buff bone_shield = new buff(DkSpells.bone_shield);
            public static readonly buff conversion = new buff(DkSpells.conversion);
            public static readonly buff crimson_scourge = new buff(DkSpells.crimson_scourge);
            public static readonly buff dancing_rune_weapon = new buff(DkSpells.dancing_rune_weapon);
            public static readonly buff icebound_fortitude = new buff(DkSpells.icebound_fortitude);
            public static readonly buff killing_machine = new buff(DkSpells.killing_machine);
            public static readonly buff rime = new buff(DkSpells.freezing_fog);
            public static readonly buff shadow_infusion = new buff(DkSpells.shadow_infusion);
            public static readonly buff sudden_doom = new buff(DkSpells.sudden_doom);
            public static readonly buff vampiric_blood = new buff(DkSpells.vampiric_blood);

            #endregion

            #region Constructors

            private buff(int spellId)
                : base(spellId)
            {
            }

            private buff(string spell)
                : base(spell)
            {
            }

            #endregion
        }

        private class cooldown : CooldownBase
        {
            #region Fields

            public static readonly cooldown antimagic_shell = new cooldown(DkSpells.antimagic_shell);
            public static readonly cooldown breath_of_sindragosa = new cooldown(DkSpells.breath_of_sindragosa);
            public static readonly cooldown defile = new cooldown(DkSpells.defile);
            public static readonly cooldown empower_rune_weapon = new cooldown(DkSpells.empower_rune_weapon);
            public static readonly cooldown outbreak = new cooldown(DkSpells.outbreak);
            public static readonly cooldown pillar_of_frost = new cooldown(DkSpells.pillar_of_frost);
            public static readonly cooldown plague_leech = new cooldown(DkSpells.plague_leech);
            public static readonly cooldown soul_reaper = new cooldown(DkSpells.soul_reaper);
            public static readonly cooldown unholy_blight = new cooldown(DkSpells.unholy_blight);

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

            public static readonly debuff necrotic_plague = new debuff(DkSpells.necrotic_plague);

            #endregion

            #region Constructors

            private debuff(string spell)
                : base(spell)
            {
            }

            #endregion
        }

        private class dot : DotBase
        {
            #region Fields

            public static readonly dot blood_plague = new dot(DkSpells.blood_plague);
            public static readonly dot breath_of_sindragosa = new dot(DkSpells.breath_of_sindragosa);
            public static readonly dot frost_fever = new dot(DkSpells.frost_fever);
            public static readonly dot necrotic_plague = new dot(DkSpells.necrotic_plague);

            #endregion

            #region Constructors

            private dot(string spell)
                : base(spell)
            {
            }

            #endregion
        }

        private class talent : TalentBase
        {
            #region Fields

            public static readonly talent blood_tap = new talent(DkTalentsEnum.BloodTap);
            public static readonly talent breath_of_sindragosa = new talent(DkTalentsEnum.BreathOfSindragosa);
            public static readonly talent defile = new talent(DkTalentsEnum.Defile);
            public static readonly talent lichborne = new talent(DkTalentsEnum.Lichborne);
            public static readonly talent necrotic_plague = new talent(DkTalentsEnum.NecroticPlague);
            public static readonly talent plague_leech = new talent(DkTalentsEnum.PlagueLeech);
            public static readonly talent runic_corruption = new talent(DkTalentsEnum.RunicCorruption);
            public static readonly talent runic_empowerment = new talent(DkTalentsEnum.RunicEmpowerment);
            public static readonly talent unholy_blight = new talent(DkTalentsEnum.UnholyBlight);
            public static readonly talent death_pact = new talent(DkTalentsEnum.DeathPact);

            #endregion

            #region Constructors

            private talent(DkTalentsEnum talent)
                : base((int) talent)
            {
            }

            #endregion
        }

        #endregion
    }
}