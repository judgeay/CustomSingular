using System;
using System.Collections.Generic;
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
    public class DeathKnight : Common.Common
    {
        #region Enums

        private enum DeathKnightTalentsEnum
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

        #region Constant

        // ReSharper disable UnusedMember.Local
        public const string blood_boil = "Blood Boil";

        private const string antimagic_shell = "Anti-Magic Shell";
        private const string army_of_the_dead = "Army of the Dead";
        private const string blood_charge = "Blood Charge";
        private const string blood_plague = "Blood Plague";
        private const string blood_tap = "Blood Tap";
        private const string bone_shield = "Bone Shield";
        private const string breath_of_sindragosa = "Breath of Sindragosa";
        private const string conversion = "Conversion";
        private const int crimson_scourge = 81141;
        private const string dancing_rune_weapon = "Dancing Rune Weapon";
        private const string dark_transformation = "Dark Transformation";
        private const string death_and_decay = "Death and Decay";
        private const string death_coil = "Death Coil";
        private const string death_grip = "Death Grip";
        private const string death_strike = "Death Grip";
        private const string defile = "Defile";
        private const string empower_rune_weapon = "Empower Rune Weapon";
        private const string festering_strike = "Festering Strike";
        private const int freezing_fog = 59052;
        private const string frost_fever = "Frost Fever";
        private const string horn_of_winter = "Horn of Winter";
        private const string icebound_fortitude = "Icebound Fortitude";
        private const string icy_touch = "Icy Touch";
        private const int killing_machine = 51124;
        private const string lichborne = "Lichborne";
        private const string necrotic_plague = "Necrotic Plague";
        private const string outbreak = "Outbreak";
        private const string pillar_of_frost = "Pillar of Frost";
        private const string plague_leech = "Plague Leech";
        private const string plague_strike = "Plague Strike";
        private const string raise_dead = "Raise Dead";
        private const string rune_tap = "Rune Tap";
        private const string runic_empowerment = "Runic Empowerment";
        private const string scourge_strike = "Scourge Strike";
        private const string shadow_infusion = "Shadow Infusion";
        private const string soul_reaper = "Soul Reaper";
        private const int sudden_doom = 81340;
        private const string summon_gargoyle = "Summon Gargoyle";
        private const string unholy_blight = "Unholy Blight";
        private const string vampiric_blood = "Vampiric Blood";
        // ReSharper restore UnusedMember.Local

        private static readonly Dictionary<object, Func<Func<bool>, Composite>> Actions = new Dictionary<object, Func<Func<bool>, Composite>>
        {
            {blood_boil, cond => Spell.Cast(blood_boil, req => Spell.UseAOE && cond())},
            {blood_tap, cond => Spell.BuffSelf(blood_tap, req => talent.blood_tap.enabled && cond())},
            {bone_shield, cond => Spell.BuffSelf(bone_shield, req => cond())},
            {breath_of_sindragosa, cond => Spell.Buff(breath_of_sindragosa, req => talent.breath_of_sindragosa.enabled && Me.HasAura(breath_of_sindragosa) == false && Spell.UseAOE && cond())},
            {dancing_rune_weapon, cond => Spell.BuffSelf(dancing_rune_weapon, req => cond())},
            {dark_transformation, cond => Spell.Cast(dark_transformation, on => Me.Pet, req => cond())},
            {death_and_decay, cond => Spell.CastOnGround(death_and_decay, on => Me.CurrentTarget, req => Spell.UseAOE && cond())},
            {death_coil, cond => Spell.Cast(death_coil, req => cond())},
            {death_grip, cond => Spell.Cast(death_grip, req => cond())},
            {death_strike, cond => Spell.Cast(death_strike, req => cond())},
            {defile, cond => Spell.CastOnGround(defile, on => Me.CurrentTarget, req => talent.defile.enabled && Spell.UseAOE && cond())},
            {empower_rune_weapon, cond => Spell.BuffSelf(empower_rune_weapon, req => cond())},
            {festering_strike, cond => Spell.Cast(festering_strike, req => cond())},
            {horn_of_winter, cond => Spell.BuffSelf(horn_of_winter, req => cond())},
            {icebound_fortitude, cond => Spell.BuffSelf(icebound_fortitude, req => cond())},
            {icy_touch, cond => Spell.Cast(icy_touch, req => cond())},
            {lichborne, cond => Spell.BuffSelf(lichborne, req => talent.lichborne.enabled && cond())},
            {outbreak, cond => Spell.Cast(outbreak, req => cond())},
            {pillar_of_frost, cond => Spell.BuffSelf(pillar_of_frost, req => cond())},
            {plague_leech, cond => Spell.BuffSelf(plague_leech, req => talent.plague_leech.enabled && disease.ticking && cond())},
            {plague_strike, cond => Spell.Cast(plague_strike, req => cond())},
            {raise_dead, cond => Spell.Cast(raise_dead, req => StyxWoW.Me.GotAlivePet == false && SingularSettings.Instance.DisablePetUsage == false && cond())},
            {rune_tap, cond => Spell.BuffSelf(rune_tap, req => cond())},
            {scourge_strike, cond => Spell.Cast(scourge_strike, req => cond())},
            {soul_reaper, cond => Spell.Cast(soul_reaper, req => cond())},
            {summon_gargoyle, cond => Spell.Cast(summon_gargoyle, req => cond())},
            {unholy_blight, cond => Spell.BuffSelfAndWait(unholy_blight, req => talent.unholy_blight.enabled && cond())},
            {vampiric_blood, cond => Spell.BuffSelf(vampiric_blood, req => cond())},
        };

        #endregion

        #region Properties

        private static int blood
        {
            get { return Me.GetRuneCount(0) + Me.GetRuneCount(1); }
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

        private static int death
        {
            get { return Me.GetRuneCount(RuneType.Death); }
        }

        private static int frost
        {
            get { return Me.GetRuneCount(2) + Me.GetRuneCount(3); }
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

        private static uint runic_power
        {
            get { return Me.CurrentRunicPower; }
        }

        private static int unholy
        {
            get { return Me.GetRuneCount(4) + Me.GetRuneCount(5); }
        }

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

        #endregion

        #region Public Methods

        [Behavior(BehaviorType.Combat, WoWClass.DeathKnight, WoWSpec.DeathKnightBlood)]
        public static Composite BloodActionList()
        {
            return new PrioritySelector(
                Spell.Buff(raise_dead, re => !StyxWoW.Me.GotAlivePet),
                Helpers.Common.EnsureReadyToAttackFromMelee(),
                Spell.WaitForCastOrChannel(),
                new Decorator(
                    ret => !Spell.IsGlobalCooldown(),
                    new PrioritySelector(
                        SingularRoutine.MoveBehaviorInlineToCombat(BehaviorType.Heal),
                        SingularRoutine.MoveBehaviorInlineToCombat(BehaviorType.CombatBuffs),
                        Helpers.Common.CreateInterruptBehavior(),
                        Movement.WaitForFacing(),
                        Movement.WaitForLineOfSpellSight(),
                // actions=auto_attack
                // actions+=/potion,name=draenic_armor,if=buff.potion.down&buff.blood_shield.down&!unholy&!frost
                // # if=time>10
                // actions+=/blood_fury
                // # if=time>10
                // actions+=/berserking
                // # if=time>10
                // actions+=/arcane_torrent
                // actions+=/antimagic_shell
                // actions+=/conversion,if=!buff.conversion.up&runic_power>50&health.pct<90
                // actions+=/lichborne,if=health.pct<90
                // actions+=/death_strike,if=incoming_damage_5s>=health.max*0.65
                // actions+=/army_of_the_dead,if=buff.bone_shield.down&buff.dancing_rune_weapon.down&buff.icebound_fortitude.down&buff.vampiric_blood.down
                // actions+=/bone_shield,if=buff.army_of_the_dead.down&buff.bone_shield.down&buff.dancing_rune_weapon.down&buff.icebound_fortitude.down&buff.vampiric_blood.down
                        Actions[bone_shield](() => buff.army_of_the_dead.down && buff.bone_shield.down && buff.dancing_rune_weapon.down && buff.icebound_fortitude.down && buff.vampiric_blood.down),
                // actions+=/vampiric_blood,if=health.pct<50
                // actions+=/icebound_fortitude,if=health.pct<30&buff.army_of_the_dead.down&buff.dancing_rune_weapon.down&buff.bone_shield.down&buff.vampiric_blood.down
                // actions+=/rune_tap,if=health.pct<50&buff.army_of_the_dead.down&buff.dancing_rune_weapon.down&buff.bone_shield.down&buff.vampiric_blood.down&buff.icebound_fortitude.down
                // actions+=/dancing_rune_weapon,if=health.pct<80&buff.army_of_the_dead.down&buff.icebound_fortitude.down&buff.bone_shield.down&buff.vampiric_blood.down
                        Actions[dancing_rune_weapon](() => health.pct < 80 && buff.army_of_the_dead.down && buff.icebound_fortitude.down && buff.bone_shield.down && buff.vampiric_blood.down),
                // actions+=/death_pact,if=health.pct<50
                // actions+=/outbreak,if=(!talent.necrotic_plague.enabled&disease.min_remains<8)|!disease.ticking
                        Actions[outbreak](() => (!talent.necrotic_plague.enabled && disease.min_remains < 8) || !disease.ticking),
                // actions+=/death_coil,if=runic_power>90
                        Actions[death_coil](() => runic_power > 90),
                // actions+=/plague_strike,if=(!talent.necrotic_plague.enabled&!dot.blood_plague.ticking)|(talent.necrotic_plague.enabled&!dot.necrotic_plague.ticking)
                        Actions[plague_strike](() => (!talent.necrotic_plague.enabled && !dot.blood_plague.ticking) || (talent.necrotic_plague.enabled && !dot.necrotic_plague.ticking)),
                // actions+=/icy_touch,if=(!talent.necrotic_plague.enabled&!dot.frost_fever.ticking)|(talent.necrotic_plague.enabled&!dot.necrotic_plague.ticking)
                        Actions[icy_touch](() => (!talent.necrotic_plague.enabled && !dot.frost_fever.ticking) || (talent.necrotic_plague.enabled && !dot.necrotic_plague.ticking)),
                // actions+=/defile
                        Actions[defile](() => true),
                // actions+=/plague_leech,if=((!blood&!unholy)|(!blood&!frost)|(!unholy&!frost))&cooldown.outbreak.remains<=gcd
                        Actions[plague_leech](() => ((blood == 0 && unholy == 0) || (blood == 0 && frost == 0) || (unholy == 0 && frost == 0)) && cooldown.outbreak.remains <= gcd),
                // actions+=/call_action_list,name=bt,if=talent.blood_tap.enabled
                        new Decorator(req => talent.blood_tap.enabled, BloodBt()),
                // actions+=/call_action_list,name=re,if=talent.runic_empowerment.enabled
                        new Decorator(req => talent.runic_empowerment.enabled, BloodRe()),
                // actions+=/call_action_list,name=rc,if=talent.runic_corruption.enabled
                        new Decorator(req => talent.runic_corruption.enabled, BloodRc()),
                // actions+=/call_action_list,name=nrt,if=!talent.blood_tap.enabled&!talent.runic_empowerment.enabled&!talent.runic_corruption.enabled
                        new Decorator(req => !talent.blood_tap.enabled && !talent.runic_empowerment.enabled && !talent.runic_corruption.enabled, BloodNrt()),
                // actions+=/defile,if=buff.crimson_scourge.react
                        Actions[defile](() => buff.crimson_scourge.react),
                // actions+=/death_and_decay,if=buff.crimson_scourge.react
                        Actions[death_and_decay](() => buff.crimson_scourge.react),
                // actions+=/blood_boil,if=buff.crimson_scourge.react
                        Actions[blood_boil](() => buff.crimson_scourge.react),
                // actions+=/death_coil
                        Actions[death_coil](() => true),
                // actions+=/empower_rune_weapon,if=!blood&!unholy&!frost
                        Actions[empower_rune_weapon](() => blood == 0 && unholy == 0 && frost == 0),

                        new ActionAlwaysFail()
                        )
                    )
                );
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
                Actions[bone_shield](() => Me.Specialization == WoWSpec.DeathKnightBlood),
                Actions[horn_of_winter](() => !Me.HasPartyBuff(PartyBuffType.AttackPower)),
                new ActionAlwaysFail()
                );
        }

        [Behavior(BehaviorType.Pull, WoWClass.DeathKnight, (WoWSpec)int.MaxValue, WoWContext.Normal | WoWContext.Battlegrounds)]
        public static Composite NormalAndPvPPull()
        {
            return new PrioritySelector(
                Helpers.Common.EnsureReadyToAttackFromMelee(),
                Spell.WaitForCastOrChannel(),
                new Decorator(
                    req => !Spell.IsGlobalCooldown(),
                    new PrioritySelector(
                        Helpers.Common.CreateInterruptBehavior(),
                        Movement.WaitForFacing(),
                        Movement.WaitForLineOfSpellSight(),
                        Actions[death_grip](
                            () =>
                                MovementManager.IsMovementDisabled == false &&
                                Me.CurrentTarget.IsBoss() == false &&
                                Me.CurrentTarget.DistanceSqr > 10 * 10 &&
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
            return new PrioritySelector(
                Spell.Buff(raise_dead, re => !StyxWoW.Me.GotAlivePet),
                Helpers.Common.EnsureReadyToAttackFromMelee(),
                Spell.WaitForCastOrChannel(),
                new Decorator(
                    ret => !Spell.IsGlobalCooldown(),
                    new PrioritySelector(
                        SingularRoutine.MoveBehaviorInlineToCombat(BehaviorType.Heal),
                        SingularRoutine.MoveBehaviorInlineToCombat(BehaviorType.CombatBuffs),
                        Helpers.Common.CreateInterruptBehavior(),
                        Movement.WaitForFacing(),
                        Movement.WaitForLineOfSpellSight(),
                // actions=auto_attack
                // actions+=/deaths_advance,if=movement.remains>2
                // actions+=/antimagic_shell,damage=100000,if=((dot.breath_of_sindragosa.ticking&runic_power<25)|cooldown.breath_of_sindragosa.remains>40)|!talent.breath_of_sindragosa.enabled
                // actions+=/blood_fury,if=!talent.breath_of_sindragosa.enabled
                // actions+=/berserking,if=!talent.breath_of_sindragosa.enabled
                // actions+=/arcane_torrent,if=!talent.breath_of_sindragosa.enabled
                // actions+=/potion,name=draenic_strength,if=(buff.dark_transformation.up&target.time_to_die<=60)&!talent.breath_of_sindragosa.enabled
                // actions+=/run_action_list,name=unholy
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

        private static Composite BloodNrt()
        {
            return new PrioritySelector(
                // actions.nrt=death_strike,if=unholy=2|frost=2
                Actions[death_strike](() => unholy == 2 || frost == 2),
                // actions.nrt+=/death_coil,if=runic_power>70
                Actions[death_coil](() => runic_power > 70),
                // actions.nrt+=/soul_reaper,if=target.health.pct-3*(target.health.pct%target.time_to_die)<=35&blood>=1
                Actions[soul_reaper](() => target.health.pct <= 36 && blood >= 1),
                // actions.nrt+=/blood_boil,if=blood>=1
                Actions[blood_boil](() => blood >= 1),

                new ActionAlwaysFail()
                );
        }

        private static Composite BloodRc()
        {
            return new PrioritySelector(
                // actions.rc=death_strike,if=unholy=2|frost=2
                Actions[death_strike](() => unholy == 2 || frost == 2),
                // actions.rc+=/death_coil,if=runic_power>70
                Actions[death_coil](() => runic_power > 70),
                // actions.rc+=/soul_reaper,if=target.health.pct-3*(target.health.pct%target.time_to_die)<=35&blood>=1
                Actions[soul_reaper](() => target.health.pct <= 36 && blood >= 1),
                // actions.rc+=/blood_boil,if=blood=2
                Actions[blood_boil](() => blood == 2),

                new ActionAlwaysFail()
                );
        }

        private static Composite BloodRe()
        {
            return new PrioritySelector(
                // actions.re=death_strike,if=unholy&frost
                Actions[death_strike](() => unholy > 0 && frost > 0),
                // actions.re+=/death_coil,if=runic_power>70
                Actions[death_coil](() => runic_power > 70),
                // actions.re+=/soul_reaper,if=target.health.pct-3*(target.health.pct%target.time_to_die)<=35&blood=2
                Actions[soul_reaper](() => target.health.pct <= 36 && blood == 2),
                // actions.re+=/blood_boil,if=blood=2
                Actions[blood_boil](() => blood == 2),

                new ActionAlwaysFail()
                );
        }

        private static Composite BloodBt()
        {
            return new PrioritySelector(
                // actions.bt=death_strike,if=unholy=2|frost=2
                Actions[death_strike](() => unholy == 2 || frost == 2),
                // actions.bt+=/blood_tap,if=buff.blood_charge.stack>=5&!blood
                Actions[blood_tap](() => buff.blood_charge.stack >= 5 && blood == 0),
                // actions.bt+=/death_strike,if=buff.blood_charge.stack>=10&unholy&frost
                Actions[death_strike](() => buff.blood_charge.stack >= 10 && unholy > 0 && frost > 0),
                // actions.bt+=/blood_tap,if=buff.blood_charge.stack>=10&!unholy&!frost
                Actions[blood_tap](() => buff.blood_charge.stack >= 10 && unholy == 0 && frost == 0),
                // actions.bt+=/blood_tap,if=buff.blood_charge.stack>=5&(!unholy|!frost)
                Actions[blood_tap](() => buff.blood_charge.stack >= 5 && (unholy == 0 || frost == 0)),
                // actions.bt+=/blood_tap,if=buff.blood_charge.stack>=5&blood.death&!unholy&!frost
                Actions[blood_tap](() => buff.blood_charge.stack >= 5 && blood_death > 0 && unholy == 0 && frost == 0),
                // actions.bt+=/death_coil,if=runic_power>70
                Actions[death_coil](() => runic_power > 70),
                // actions.bt+=/soul_reaper,if=target.health.pct-3*(target.health.pct%target.time_to_die)<=35&(blood=2|(blood&!blood.death))
                Actions[soul_reaper](() => target.health.pct <= 36 && (blood == 2 || (blood > 0 && blood_death == 0))),
                // actions.bt+=/blood_boil,if=blood=2|(blood&!blood.death)
                Actions[blood_boil](() => blood == 2 || (blood > 0 && blood_death == 0)),

                new ActionAlwaysFail()
                );
        }

        private static Composite UnholyBos()
        {
            return new PrioritySelector(
                // actions.bos=blood_fury,if=dot.breath_of_sindragosa.ticking
                // actions.bos+=/berserking,if=dot.breath_of_sindragosa.ticking
                // actions.bos+=/potion,name=draenic_strength,if=dot.breath_of_sindragosa.ticking
                // actions.bos+=/unholy_blight,if=!disease.ticking
                Actions[unholy_blight](() => !disease.ticking),
                // actions.bos+=/plague_strike,if=!disease.ticking
                Actions[plague_strike](() => !disease.ticking),
                // actions.bos+=/blood_boil,cycle_targets=1,if=(spell_targets.blood_boil>=2&!(dot.blood_plague.ticking|dot.frost_fever.ticking))|spell_targets.blood_boil>=4&(runic_power<88&runic_power>30)
                Actions[blood_boil](() => (spell_targets.blood_boil >= 2 && !(dot.blood_plague.ticking || dot.frost_fever.ticking)) || spell_targets.blood_boil >= 4 && (runic_power < 88 && runic_power > 30)),
                // actions.bos+=/death_and_decay,if=spell_targets.death_and_decay>=2&(runic_power<88&runic_power>30)
                Actions[death_and_decay](() => spell_targets.death_and_decay >= 2 && (runic_power < 88 && runic_power > 30)),
                // actions.bos+=/festering_strike,if=(blood=2&frost=2&(((Frost-death)>0)|((Blood-death)>0)))&runic_power<80
                Actions[festering_strike](() => (blood == 2 && frost == 2 && (((Frost - death) > 0) || ((Blood - death) > 0))) && runic_power < 80),
                // actions.bos+=/festering_strike,if=((blood=2|frost=2)&(((Frost-death)>0)&((Blood-death)>0)))&runic_power<80
                Actions[festering_strike](() => ((blood == 2 || frost == 2) && (((Frost - death) > 0) && ((Blood - death) > 0))) && runic_power < 80),
                // actions.bos+=/arcane_torrent,if=runic_power<70
                // actions.bos+=/scourge_strike,if=spell_targets.blood_boil<=3&(runic_power<88&runic_power>30)
                Actions[scourge_strike](() => spell_targets.blood_boil <= 3 && (runic_power < 88 && runic_power > 30)),
                // actions.bos+=/blood_boil,if=spell_targets.blood_boil>=4&(runic_power<88&runic_power>30)
                Actions[blood_boil](() => spell_targets.blood_boil >= 4 && (runic_power < 88 && runic_power > 30)),
                // actions.bos+=/festering_strike,if=runic_power<77
                Actions[festering_strike](() => runic_power < 77),
                // actions.bos+=/scourge_strike,if=(spell_targets.blood_boil>=4&(runic_power<88&runic_power>30))|spell_targets.blood_boil<=3
                Actions[scourge_strike](() => (spell_targets.blood_boil >= 4 && (runic_power < 88 && runic_power > 30)) || spell_targets.blood_boil <= 3),
                // actions.bos+=/dark_transformation
                Actions[dark_transformation](() => true),
                // actions.bos+=/blood_tap,if=buff.blood_charge.stack>=5
                Actions[blood_tap](() => buff.blood_charge.stack >= 5),
                // actions.bos+=/plague_leech
                Actions[plague_leech](() => true),
                // actions.bos+=/empower_rune_weapon,if=runic_power<60
                Actions[empower_rune_weapon](() => runic_power < 60),
                // actions.bos+=/death_coil,if=buff.sudden_doom.react
                Actions[death_coil](() => buff.sudden_doom.react),

                new ActionAlwaysFail()
                );
        }

        private static Composite UnholyUnholy()
        {
            return new PrioritySelector(
                // actions.unholy=plague_leech,if=((cooldown.outbreak.remains<1)|disease.min_remains<1)&((blood<1&frost<1)|(blood<1&unholy<1)|(frost<1&unholy<1))
                Actions[plague_leech](() => ((cooldown.outbreak.remains < 1) || disease.min_remains < 1) && ((blood < 1 && frost < 1) || (blood < 1 && unholy < 1) || (frost < 1 && unholy < 1))),
                // actions.unholy+=/soul_reaper,if=(target.health.pct-3*(target.health.pct%target.time_to_die))<=45
                Actions[soul_reaper](() => target.health.pct <= 46),
                // actions.unholy+=/blood_tap,if=((target.health.pct-3*(target.health.pct%target.time_to_die))<=45)&cooldown.soul_reaper.remains=0
                Actions[blood_tap](() => (target.health.pct <= 46) && cooldown.soul_reaper.remains == 0),
                // actions.unholy+=/summon_gargoyle
                Actions[summon_gargoyle](() => true),
                // actions.unholy+=/breath_of_sindragosa,if=runic_power>75
                Actions[breath_of_sindragosa](() => runic_power > 75),
                // actions.unholy+=/run_action_list,name=bos,if=dot.breath_of_sindragosa.ticking
                new Decorator(req => talent.breath_of_sindragosa.enabled && dot.breath_of_sindragosa.ticking, UnholyBos()),
                // actions.unholy+=/unholy_blight,if=!disease.min_ticking
                Actions[unholy_blight](() => !disease.min_ticking),
                // actions.unholy+=/outbreak,cycle_targets=1,if=!talent.necrotic_plague.enabled&(!(dot.blood_plague.ticking|dot.frost_fever.ticking))
                Actions[outbreak](() => !talent.necrotic_plague.enabled && (!(dot.blood_plague.ticking || dot.frost_fever.ticking))),
                // actions.unholy+=/plague_strike,if=(!talent.necrotic_plague.enabled&!(dot.blood_plague.ticking|dot.frost_fever.ticking))|(talent.necrotic_plague.enabled&!dot.necrotic_plague.ticking)
                Actions[plague_strike](() => (!talent.necrotic_plague.enabled && !(dot.blood_plague.ticking || dot.frost_fever.ticking)) || (talent.necrotic_plague.enabled && !dot.necrotic_plague.ticking)),
                // actions.unholy+=/blood_boil,cycle_targets=1,if=(spell_targets.blood_boil>1&!talent.necrotic_plague.enabled)&(!(dot.blood_plague.ticking|dot.frost_fever.ticking))
                Actions[blood_boil](() => (spell_targets.blood_boil > 1 && !talent.necrotic_plague.enabled) && (!(dot.blood_plague.ticking || dot.frost_fever.ticking))),
                // actions.unholy+=/death_and_decay,if=spell_targets.death_and_decay>1&unholy>1
                Actions[death_and_decay](() => spell_targets.death_and_decay > 1 && unholy > 1),
                // actions.unholy+=/defile,if=unholy=2
                Actions[defile](() => unholy == 2),
                // actions.unholy+=/blood_tap,if=talent.defile.enabled&cooldown.defile.remains=0
                Actions[blood_tap](() => talent.defile.enabled && cooldown.defile.remains == 0),
                // actions.unholy+=/scourge_strike,if=unholy=2
                Actions[scourge_strike](() => unholy == 2),
                // actions.unholy+=/festering_strike,if=talent.necrotic_plague.enabled&talent.unholy_blight.enabled&dot.necrotic_plague.remains<cooldown.unholy_blight.remains%2
                Actions[festering_strike](() => talent.necrotic_plague.enabled && talent.unholy_blight.enabled && dot.necrotic_plague.remains < cooldown.unholy_blight.remains % 2),
                // actions.unholy+=/dark_transformation
                Actions[dark_transformation](() => true),
                // actions.unholy+=/festering_strike,if=blood=2&frost=2&(((Frost-death)>0)|((Blood-death)>0))
                Actions[festering_strike](() => blood == 2 && frost == 2 && (((Frost - death) > 0) || ((Blood - death) > 0))),
                // actions.unholy+=/festering_strike,if=(blood=2|frost=2)&(((Frost-death)>0)&((Blood-death)>0))
                Actions[festering_strike](() => (blood == 2 || frost == 2) && (((Frost - death) > 0) && ((Blood - death) > 0))),
                // actions.unholy+=/blood_boil,cycle_targets=1,if=(talent.necrotic_plague.enabled&!dot.necrotic_plague.ticking)&spell_targets.blood_boil>1
                Actions[blood_boil](() => (talent.necrotic_plague.enabled && !dot.necrotic_plague.ticking) && spell_targets.blood_boil > 1),
                // actions.unholy+=/defile,if=blood=2|frost=2
                Actions[defile](() => blood == 2 || frost == 2),
                // actions.unholy+=/death_and_decay,if=spell_targets.death_and_decay>1
                Actions[death_and_decay](() => spell_targets.death_and_decay > 1),
                // actions.unholy+=/defile
                Actions[defile](() => true),
                // actions.unholy+=/blood_boil,if=talent.breath_of_sindragosa.enabled&((spell_targets.blood_boil>=4&(blood=2|(frost=2&death=2)))&(cooldown.breath_of_sindragosa.remains>6|runic_power<75))
                Actions[blood_boil](() => talent.breath_of_sindragosa.enabled && ((spell_targets.blood_boil >= 4 && (blood == 2 || (frost == 2 && death == 2))) && (cooldown.breath_of_sindragosa.remains > 6 || runic_power < 75))),
                // actions.unholy+=/blood_boil,if=!talent.breath_of_sindragosa.enabled&(spell_targets.blood_boil>=4&(blood=2|(frost=2&death=2)))
                Actions[blood_boil](() => !talent.breath_of_sindragosa.enabled && (spell_targets.blood_boil >= 4 && (blood == 2 || (frost == 2 && death == 2)))),
                // actions.unholy+=/blood_tap,if=buff.blood_charge.stack>10
                Actions[blood_tap](() => buff.blood_charge.stack > 10),
                // actions.unholy+=/outbreak,if=talent.necrotic_plague.enabled&debuff.necrotic_plague.stack<=14
                Actions[outbreak](() => talent.necrotic_plague.enabled && debuff.necrotic_plague.stack <= 14),
                // actions.unholy+=/death_coil,if=(buff.sudden_doom.react|runic_power>80)&(buff.blood_charge.stack<=10)
                Actions[death_coil](() => (buff.sudden_doom.react || runic_power > 80) && (buff.blood_charge.stack <= 10)),
                // actions.unholy+=/blood_boil,if=(spell_targets.blood_boil>=4&(cooldown.breath_of_sindragosa.remains>6|runic_power<75))|(!talent.breath_of_sindragosa.enabled&spell_targets.blood_boil>=4)
                Actions[blood_boil](() => (spell_targets.blood_boil >= 4 && (cooldown.breath_of_sindragosa.remains > 6 || runic_power < 75)) || (!talent.breath_of_sindragosa.enabled && spell_targets.blood_boil >= 4)),
                // actions.unholy+=/scourge_strike,if=(cooldown.breath_of_sindragosa.remains>6|runic_power<75|unholy=2)|!talent.breath_of_sindragosa.enabled
                Actions[scourge_strike](() => (cooldown.breath_of_sindragosa.remains > 6 || runic_power < 75 || unholy == 2) || !talent.breath_of_sindragosa.enabled),
                // actions.unholy+=/festering_strike,if=(cooldown.breath_of_sindragosa.remains>6|runic_power<75)|!talent.breath_of_sindragosa.enabled
                Actions[festering_strike](() => (cooldown.breath_of_sindragosa.remains > 6 || runic_power < 75) || !talent.breath_of_sindragosa.enabled),
                // actions.unholy+=/death_coil,if=(cooldown.breath_of_sindragosa.remains>20)|!talent.breath_of_sindragosa.enabled
                Actions[death_coil](() => (cooldown.breath_of_sindragosa.remains > 20) || !talent.breath_of_sindragosa.enabled),
                // actions.unholy+=/plague_leech
                Actions[plague_leech](() => true),
                // actions.unholy+=/empower_rune_weapon,if=!talent.breath_of_sindragosa.enabled
                Actions[empower_rune_weapon](() => !talent.breath_of_sindragosa.enabled),

                new ActionAlwaysFail()
                );
        }

        #endregion

        // ReSharper disable MemberHidesStaticFromOuterClass
        // ReSharper disable UnusedMember.Local

        #region Types

        private static class disease
        {
            #region Fields

            private static readonly string[] listBase = { blood_plague, frost_fever };
            private static readonly string[] listWithNecroticPlague = { necrotic_plague };

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

            #region Public Methods

            public static bool ticking_on(WoWUnit unit)
            {
                if (unit == null) return false;

                return unit.HasAllMyAuras(diseaseArray);
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

            #endregion
        }

        private static class spell_targets
        {
            #region Properties

            public static int blood_boil
            {
                get { return active_enemies_list.Count(disease.ticking_on) == 0 ? active_enemies_list.Count(u => disease.ticking_on(u) == false) : 0; }
            }

            public static int death_and_decay
            {
                get { return active_enemies_list.Count(u => u.Distance <= 10); }
            }

            #endregion
        }

        private class buff : BuffBase
        {
            #region Fields

            public static readonly buff army_of_the_dead = new buff(DeathKnight.army_of_the_dead);

            public static readonly buff blood_charge = new buff(DeathKnight.blood_charge);

            public static readonly buff bone_shield = new buff(DeathKnight.bone_shield);

            public static readonly buff crimson_scourge = new buff(DeathKnight.crimson_scourge);

            public static readonly buff dancing_rune_weapon = new buff(DeathKnight.dancing_rune_weapon);

            public static readonly buff dark_transformation = new buff(DeathKnight.dark_transformation);

            public static readonly buff icebound_fortitude = new buff(DeathKnight.icebound_fortitude);

            public static readonly buff killing_machine = new buff(DeathKnight.killing_machine);

            public static readonly buff rime = new buff(freezing_fog);

            public static readonly buff shadow_infusion = new buff(DeathKnight.shadow_infusion);

            public static readonly buff sudden_doom = new buff(DeathKnight.sudden_doom);

            public static readonly buff vampiric_blood = new buff(DeathKnight.vampiric_blood);

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

            public static readonly cooldown antimagic_shell = new cooldown(DeathKnight.antimagic_shell);

            public static readonly cooldown breath_of_sindragosa = new cooldown(DeathKnight.breath_of_sindragosa);

            public static readonly cooldown defile = new cooldown(DeathKnight.defile);

            public static readonly cooldown empower_rune_weapon = new cooldown(DeathKnight.empower_rune_weapon);

            public static readonly cooldown outbreak = new cooldown(DeathKnight.outbreak);

            public static readonly cooldown pillar_of_frost = new cooldown(DeathKnight.pillar_of_frost);

            public static readonly cooldown soul_reaper = new cooldown(DeathKnight.soul_reaper);

            public static readonly cooldown unholy_blight = new cooldown(DeathKnight.unholy_blight);

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

            public static readonly debuff necrotic_plague = new debuff(DeathKnight.necrotic_plague);

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

            public static readonly dot blood_plague = new dot(DeathKnight.blood_plague);

            public static readonly dot breath_of_sindragosa = new dot(DeathKnight.breath_of_sindragosa);

            public static readonly dot frost_fever = new dot(DeathKnight.frost_fever);

            public static readonly dot necrotic_plague = new dot(DeathKnight.necrotic_plague);

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

            public static readonly talent blood_tap = new talent(DeathKnightTalentsEnum.BloodTap);

            public static readonly talent breath_of_sindragosa = new talent(DeathKnightTalentsEnum.BreathOfSindragosa);

            public static readonly talent defile = new talent(DeathKnightTalentsEnum.Defile);

            public static readonly talent lichborne = new talent(DeathKnightTalentsEnum.Lichborne);

            public static readonly talent necrotic_plague = new talent(DeathKnightTalentsEnum.NecroticPlague);

            public static readonly talent plague_leech = new talent(DeathKnightTalentsEnum.PlagueLeech);

            public static readonly talent runic_corruption = new talent(DeathKnightTalentsEnum.RunicCorruption);

            public static readonly talent runic_empowerment = new talent(DeathKnightTalentsEnum.RunicEmpowerment);

            public static readonly talent unholy_blight = new talent(DeathKnightTalentsEnum.UnholyBlight);

            #endregion

            #region Constructors

            private talent(DeathKnightTalentsEnum talent)
                : base((int)talent)
            {
            }

            #endregion
        }

        #endregion
    }
}