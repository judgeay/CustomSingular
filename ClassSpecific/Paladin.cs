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
    // ReSharper disable ClassNeverInstantiated.Global
    // ReSharper disable InconsistentNaming
    // ReSharper disable CompareOfFloatsByEqualityOperator
    public class Paladin : Common.Common
    {
        /**
         * @todo SealBase
         * @todo gcd.max
         **/

        #region Fields

        private const byte DIVINE_STORM_DISTANCE = 8;
        private const byte DIVINE_STORM_EMPOWERED_DISTANCE = 12;
        private const byte EXORCISM_DISTANCE = 8;
        private const byte GCD_MAX = 1;
        private const byte HAMMER_OF_THE_RIGHTEOUS_DISTANCE = 8;

        private static readonly Func<Func<bool>, Composite> avenging_wrath = cond => Spell.BuffSelf(PalSpells.avenging_wrath, req => Spell.UseCooldown && cond());
        private static readonly Func<Func<bool>, Composite> benediction_of_kings = cond => Spell.BuffSelf(PalSpells.benediction_of_kings, req => cond());
        private static readonly Func<Func<bool>, Composite> benediction_of_might = cond => Spell.BuffSelf(PalSpells.benediction_of_might, req => cond());
        private static readonly Func<Func<bool>, Composite> crusader_strike = cond => Spell.Cast(PalSpells.crusader_strike, req => cond());
        private static readonly Func<Func<bool>, Composite> divine_protection = cond => Spell.BuffSelf(PalSpells.divine_protection, req => Spell.UseCooldown && cond());
        private static readonly Func<Func<bool>, Composite> divine_shield = cond => Spell.BuffSelf(PalSpells.divine_shield, req => Spell.UseCooldown && cond());
        private static readonly Func<Func<bool>, Composite> divine_storm = cond => Spell.Cast(PalSpells.divine_storm, req => Spell.UseAoe && cond());
        private static readonly Func<Func<bool>, Composite> execution_sentence = cond => Spell.Cast(PalSpells.execution_sentence, req => talent.execution_sentence.enabled && cond());
        private static readonly Func<Func<bool>, Composite> exorcism = cond => Spell.Cast(PalSpells.exorcism, req => cond());
        private static readonly Func<Func<bool>, Composite> final_verdict = cond => Spell.Buff(PalSpells.final_verdict, req => talent.final_verdict.enabled && cond());
        private static readonly Func<Func<bool>, Composite> hammer_of_the_righteous = cond => Spell.Cast(PalSpells.hammer_of_the_righteous, req => Spell.UseAoe && cond());
        private static readonly Func<Func<bool>, Composite> hammer_of_wrath = cond => Spell.Cast(PalSpells.hammer_of_wrath, req => cond());
        private static readonly Func<Func<bool>, Composite> holy_avenger = cond => Spell.BuffSelf(PalSpells.holy_avenger, req => talent.holy_avenger.enabled && Spell.UseCooldown && cond());
        private static readonly Func<Func<bool>, Composite> holy_light = cond => Spell.Cast(PalSpells.holy_light, on => Me, req => cond());
        private static readonly Func<Func<WoWUnit>, Func<bool>, Composite> holy_prism = (target, cond) => Spell.Cast(PalSpells.holy_prism, on => target(), req => cond());
        private static readonly Func<Func<bool>, Composite> judgment = cond => Spell.Cast(PalSpells.judgment, req => cond());
        private static readonly Func<Func<bool>, Composite> lay_on_hands = cond => Spell.Cast(PalSpells.lay_on_hands, on => Me, req => Spell.UseCooldown && cond());
        private static readonly Func<Func<bool>, Composite> lights_hammer = cond => Spell.CastOnGround(PalSpells.lights_hammer, on => Me.CurrentTarget, req => Spell.UseAoe && cond());
        private static readonly Func<Func<bool>, Composite> seal_of_command = cond => Spell.BuffSelf(PalSpells.seal_of_command, req => cond());
        private static readonly Func<Func<bool>, Composite> seal_of_insight = cond => Spell.BuffSelf(PalSpells.seal_of_insight, req => cond());
        private static readonly Func<Func<bool>, Composite> seal_of_righteousness = cond => Spell.BuffSelf(PalSpells.seal_of_righteousness, req => cond());
        private static readonly Func<Func<bool>, Composite> seal_of_truth = cond => Spell.BuffSelf(PalSpells.seal_of_truth, req => cond());
        private static readonly Func<Func<bool>, Composite> seraphim = cond => Spell.BuffSelf(PalSpells.seraphim, req => talent.seraphim.enabled && cond());
        private static readonly Func<Func<bool>, Composite> templars_verdict = cond => Spell.Cast(PalSpells.templars_verdict, req => cond());
        private static readonly Func<Func<bool>, Composite> word_of_glory = cond => Spell.Cast(PalSpells.word_of_glory, on => Me, req => cond());

        #endregion

        #region Enums

        public enum PalTalentsEnum
        {
            // ReSharper disable UnusedMember.Local
            SpeedOfLight = 1,
            LongArmOfTheLaw,
            PursuitOfJustice,

            FistOfJustice,
            Repentance,
            BlindingLight,

            SelflessHealer,
            EternalFlame,
            SacredShield,

            HandOfPurity,
            UnbreakableSpirit,
            Clemency,

            HolyAvenger,
            SanctifiedWrath,
            DivinePurpose,

            HolyPrism,
            LightsHammer,
            ExecutionSentence,

            BeaconOfFaith,
            EmpoweredSeals = BeaconOfFaith,
            BeaconOfInsight,
            Seraphim = BeaconOfInsight,
            SavedbyTheLight,
            HolyShield = SavedbyTheLight,
            FinalVerdict = SavedbyTheLight
            // ReSharper restore UnusedMember.Local
        }

        #endregion

        #region Properties

        public static uint holy_power
        {
            get { return Me.CurrentHolyPower; }
        }

        public static WoWUnit last_judgment_target { get; set; }

        #endregion

        #region Public Methods

        [Behavior(BehaviorType.PreCombatBuffs | BehaviorType.PullBuffs, WoWClass.Paladin)]
        public static Composite Buffs()
        {
            return new PrioritySelector(
                seal_of_truth(() => Me.Specialization == WoWSpec.PaladinRetribution),
                seal_of_insight(() => (Me.Specialization == WoWSpec.PaladinProtection || Me.Specialization == WoWSpec.PaladinHoly)),
                benediction_of_might(() => !Me.HasPartyBuff(PartyBuffType.Mastery)),
                benediction_of_kings(() => !Me.HasPartyBuff(PartyBuffType.Stats) && !Me.HasMyAura(PalSpells.benediction_of_might)),
                new ActionAlwaysFail()
                );
        }

        [Behavior(BehaviorType.Combat, WoWClass.Paladin, WoWSpec.PaladinRetribution)]
        public static Composite RetributionActionList()
        {
            return new PrioritySelector(Helpers.Common.EnsureReadyToAttackFromMelee(), Spell.WaitForCastOrChannel(),
                new Decorator(ret => !Spell.IsGlobalCooldown(), new PrioritySelector(
                    SingularRoutine.MoveBehaviorInlineToCombat(BehaviorType.Heal),
                    SingularRoutine.MoveBehaviorInlineToCombat(BehaviorType.CombatBuffs),
                    Helpers.Common.CreateInterruptBehavior(),
                    Movement.WaitForFacing(),
                    Movement.WaitForLineOfSpellSight(),
                    use_trinket(),
                    //actions=rebuke
                    //actions+=/potion,name=draenic_strength,if=(buff.bloodlust.react|buff.avenging_wrath.up|target.time_to_die<=40)
                    //actions+=/auto_attack,target_if=dot.censure.remains<4
                    //actions+=/speed_of_light,if=movement.distance>5
                    //actions+=/judgment,if=talent.empowered_seals.enabled&time<2
                    judgment(() => (talent.empowered_seals.enabled)), // ADD COMBAT TIME
                    //actions+=/execution_sentence,if=!talent.seraphim.enabled
                    execution_sentence(() => (!talent.seraphim.enabled)),
                    //actions+=/execution_sentence,sync=seraphim,if=talent.seraphim.enabled
                    execution_sentence(() => (cooldown.seraphim.up && talent.seraphim.enabled)),
                    //actions+=/lights_hammer,if=!talent.seraphim.enabled
                    lights_hammer(() => (!talent.seraphim.enabled)),
                    //actions+=/lights_hammer,sync=seraphim,if=talent.seraphim.enabled
                    lights_hammer(() => (cooldown.seraphim.up && talent.seraphim.enabled)),
                    //actions+=/use_item,name=thorasus_the_stone_heart_of_draenor,if=buff.avenging_wrath.up
                    //actions+=/avenging_wrath,sync=seraphim,if=talent.seraphim.enabled
                    avenging_wrath(() => (cooldown.seraphim.up && talent.seraphim.enabled)),
                    //actions+=/avenging_wrath,if=!talent.seraphim.enabled&set_bonus.tier18_4pc=0
                    avenging_wrath(() => (!talent.seraphim.enabled && !set_bonus.tier18_4pc)),
                    //actions+=/avenging_wrath,if=!talent.seraphim.enabled&time<20&set_bonus.tier18_4pc=1
                    avenging_wrath(() => (!talent.seraphim.enabled && set_bonus.tier18_4pc)), // ADD COMBAT TIME
                    //actions+=/avenging_wrath,if=prev.execution_sentence&set_bonus.tier18_4pc=1&talent.execution_sentence.enabled&!talent.seraphim.enabled
                    avenging_wrath(() => (prev_gcd == PalSpells.execution_sentence && set_bonus.tier18_4pc && talent.execution_sentence.enabled && !talent.seraphim.enabled)),
                    //actions+=/avenging_wrath,if=prev.lights_hammer&set_bonus.tier18_4pc=1&talent.lights_hammer.enabled&!talent.seraphim.enabled
                    avenging_wrath(() => (prev_gcd == PalSpells.lights_hammer && set_bonus.tier18_4pc && talent.lights_hammer.enabled && !talent.seraphim.enabled)),
                    //actions+=/holy_avenger,sync=avenging_wrath,if=!talent.seraphim.enabled
                    holy_avenger(() => (cooldown.avenging_wrath.up && !talent.seraphim.enabled)),
                    //actions+=/holy_avenger,sync=seraphim,if=talent.seraphim.enabled
                    holy_avenger(() => (cooldown.seraphim.up && talent.seraphim.enabled)),
                    //actions+=/holy_avenger,if=holy_power<=2&!talent.seraphim.enabled
                    holy_avenger(() => (holy_power <= 2 && !talent.seraphim.enabled)),
                    //actions+=/blood_fury
                    blood_fury(() => true),
                    //actions+=/berserking
                    berserking(() => true),
                    //actions+=/arcane_torrent
                    arcane_torrent(() => true),
                    //actions+=/seraphim
                    seraphim(() => (true)),
                    //actions+=/wait,sec=cooldown.seraphim.remains,if=talent.seraphim.enabled&cooldown.seraphim.remains>0&cooldown.seraphim.remains<gcd.max&holy_power>=5
                    //actions+=/call_action_list,name=cleave,if=spell_targets.divine_storm>=3
                    new Decorator(RetributionCleave(), req => spell_targets.divine_storm >= 3),
                    //actions+=/call_action_list,name=single
                    new Decorator(RetributionSingle()),
                    new ActionAlwaysFail()
                    )));
        }

        [Behavior(BehaviorType.Pull, WoWClass.Paladin, WoWSpec.PaladinRetribution, WoWContext.Instances)]
        public static Composite RetributionInstancePull()
        {
            return RetributionActionList();
        }

        #endregion

        #region Private Methods

        private static Composite RetributionCleave()
        {
            return new PrioritySelector(
                //actions.cleave=final_verdict,if=buff.final_verdict.down&holy_power=5
                final_verdict(() => (buff.final_verdict.down && holy_power == 5)),
                //actions.cleave+=/divine_storm,if=buff.divine_crusader.react&holy_power=5&buff.final_verdict.up
                divine_storm(() => (buff.divine_crusader.react && holy_power == 5 && buff.final_verdict.up)),
                //actions.cleave+=/divine_storm,if=holy_power=5&buff.final_verdict.up
                divine_storm(() => (holy_power == 5 && buff.final_verdict.up)),
                //actions.cleave+=/divine_storm,if=buff.divine_crusader.react&holy_power=5&!talent.final_verdict.enabled
                divine_storm(() => (buff.divine_crusader.react && holy_power == 5 && !talent.final_verdict.enabled)),
                //actions.cleave+=/divine_storm,if=holy_power=5&(!talent.seraphim.enabled|cooldown.seraphim.remains>gcd*4)&!talent.final_verdict.enabled
                divine_storm(() => (holy_power == 5 && (!talent.seraphim.enabled || cooldown.seraphim.remains > gcd * 4) && !talent.final_verdict.enabled)),
                //actions.cleave+=/hammer_of_wrath
                hammer_of_wrath(() => true),
                //actions.cleave+=/hammer_of_the_righteous,if=t18_class_trinket=1&buff.focus_of_vengeance.remains<gcd.max*2
                hammer_of_the_righteous(() => (t18_class_trinket && buff.focus_of_vengeance.remains < GCD_MAX * 2)),
                //actions.cleave+=/judgment,if=talent.empowered_seals.enabled&seal.righteousness&buff.liadrins_righteousness.remains<cooldown.judgment.duration
                judgment(() => (talent.empowered_seals.enabled && buff.seal_of_righteousness.up && buff.liadrins_righteousness.remains < cooldown.judgment.duration)),
                //actions.cleave+=/exorcism,if=buff.blazing_contempt.up&holy_power<=2&buff.holy_avenger.down
                exorcism(() => (buff.blazing_contempt.up && holy_power <= 2 && buff.holy_avenger.down)),
                //actions.cleave+=/divine_storm,if=buff.divine_crusader.react&buff.final_verdict.up&(buff.avenging_wrath.up|target.health.pct<35)
                divine_storm(() => (buff.divine_crusader.react && buff.final_verdict.up && (buff.avenging_wrath.up || target.health.pct < 35))),
                //actions.cleave+=/divine_storm,if=buff.final_verdict.up&(buff.avenging_wrath.up|target.health.pct<35)
                divine_storm(() => (buff.final_verdict.up && (buff.avenging_wrath.up || target.health.pct < 35))),
                //actions.cleave+=/final_verdict,if=buff.final_verdict.down&(buff.avenging_wrath.up|target.health.pct<35)
                final_verdict(() => (buff.final_verdict.down && (buff.avenging_wrath.up || target.health.pct < 35))),
                //actions.cleave+=/divine_storm,if=buff.divine_crusader.react&(buff.avenging_wrath.up|target.health.pct<35)&!talent.final_verdict.enabled
                divine_storm(() => (buff.divine_crusader.react && (buff.avenging_wrath.up || target.health.pct < 35) && !talent.final_verdict.enabled)),
                //actions.cleave+=/divine_storm,if=holy_power=5&(buff.avenging_wrath.up|target.health.pct<35)&(!talent.seraphim.enabled|cooldown.seraphim.remains>gcd*3)&!talent.final_verdict.enabled
                divine_storm(() => (holy_power == 5 && (buff.avenging_wrath.up || target.health.pct < 35) && (!talent.seraphim.enabled || cooldown.seraphim.remains > gcd * 3) && !talent.final_verdict.enabled)),
                //actions.cleave+=/divine_storm,if=holy_power=4&(buff.avenging_wrath.up|target.health.pct<35)&(!talent.seraphim.enabled|cooldown.seraphim.remains>gcd*4)&!talent.final_verdict.enabled
                divine_storm(() => (holy_power == 4 && (buff.avenging_wrath.up || target.health.pct < 35) && (!talent.seraphim.enabled || cooldown.seraphim.remains > gcd * 4) && !talent.final_verdict.enabled)),
                //actions.cleave+=/divine_storm,if=holy_power=3&(buff.avenging_wrath.up|target.health.pct<35)&(!talent.seraphim.enabled|cooldown.seraphim.remains>gcd*5)&!talent.final_verdict.enabled
                divine_storm(() => (holy_power == 3 && (buff.avenging_wrath.up || target.health.pct < 35) && (!talent.seraphim.enabled || cooldown.seraphim.remains > gcd * 5) && !talent.final_verdict.enabled)),
                //actions.cleave+=/hammer_of_the_righteous,if=spell_targets.hammer_of_the_righteous>=4&talent.seraphim.enabled
                hammer_of_the_righteous(() => (spell_targets.hammer_of_the_righteous >= 4 && talent.seraphim.enabled)),
                //actions.cleave+=/hammer_of_the_righteous,,if=spell_targets.hammer_of_the_righteous>=4&(holy_power<=3|(holy_power=4&target.health.pct>=35&buff.avenging_wrath.down))
                hammer_of_the_righteous(() => (spell_targets.hammer_of_the_righteous >= 4 && (holy_power <= 3 || (holy_power == 4 && target.health.pct >= 35 && buff.avenging_wrath.down)))),
                //actions.cleave+=/crusader_strike,if=talent.seraphim.enabled
                crusader_strike(() => (talent.seraphim.enabled)),
                //actions.cleave+=/crusader_strike,if=holy_power<=3|(holy_power=4&target.health.pct>=35&buff.avenging_wrath.down)
                crusader_strike(() => (holy_power <= 3 || (holy_power == 4 && target.health.pct >= 35 && buff.avenging_wrath.down))),
                //actions.cleave+=/exorcism,if=glyph.mass_exorcism.enabled&!set_bonus.tier17_4pc=1
                exorcism(() => (glyph.mass_exorcism.enabled && !set_bonus.tier17_4pc)),
                //actions.cleave+=/judgment,cycle_targets=1,if=last_judgment_target!=target&talent.seraphim.enabled&glyph.double_jeopardy.enabled
                //judgment(() => (talent.seraphim.enabled && glyph.double_jeopardy.enabled && active_enemies_list.Any(x => last_judgment_target != x))),
                //actions.cleave+=/judgment,if=talent.seraphim.enabled
                judgment(() => (talent.seraphim.enabled)),
                //actions.cleave+=/judgment,cycle_targets=1,if=last_judgment_target!=target&glyph.double_jeopardy.enabled&(holy_power<=3|(holy_power=4&cooldown.crusader_strike.remains>=gcd*2&target.health.pct>35&buff.avenging_wrath.down))
                //judgment(() => active_enemies_list.Any(x => last_judgment_target != x) && glyph.double_jeopardy.enabled && (holy_power <= 3 || (holy_power == 4 && cooldown.crusader_strike.remains >= gcd * 2 && target.health.pct > 35 && buff.avenging_wrath.down))),
                //actions.cleave+=/judgment,if=holy_power<=3|(holy_power=4&cooldown.crusader_strike.remains>=gcd*2&target.health.pct>35&buff.avenging_wrath.down)
                judgment(() => (holy_power <= 3 || (holy_power == 4) && cooldown.crusader_strike.remains >= gcd * 2 && target.health.pct > 35 && buff.avenging_wrath.down)),
                //actions.cleave+=/divine_storm,if=buff.divine_crusader.react&buff.final_verdict.up
                divine_storm(() => (buff.divine_crusader.react && buff.final_verdict.up)),
                //actions.cleave+=/divine_storm,if=buff.divine_purpose.react&buff.final_verdict.up
                divine_storm(() => (buff.divine_purpose.react && buff.final_verdict.up)),
                //actions.cleave+=/divine_storm,if=holy_power>=4&buff.final_verdict.up
                divine_storm(() => (holy_power >= 4 && buff.final_verdict.up)),
                //actions.cleave+=/final_verdict,if=buff.divine_purpose.react&buff.final_verdict.down
                final_verdict(() => (buff.divine_purpose.react && buff.final_verdict.down)),
                //actions.cleave+=/final_verdict,if=holy_power>=4&buff.final_verdict.down
                final_verdict(() => (holy_power >= 4 && buff.final_verdict.down)),
                //actions.cleave+=/divine_storm,if=buff.divine_crusader.react&!talent.final_verdict.enabled
                divine_storm(() => (buff.divine_crusader.react && !talent.final_verdict.enabled)),
                //actions.cleave+=/divine_storm,if=holy_power>=4&(!talent.seraphim.enabled|cooldown.seraphim.remains>gcd*5)&!talent.final_verdict.enabled
                divine_storm(() => (holy_power >= 4 && (!talent.seraphim.enabled || cooldown.seraphim.remains > gcd * 5) && !talent.final_verdict.enabled)),
                //actions.cleave+=/exorcism,if=talent.seraphim.enabled
                exorcism(() => (talent.seraphim.enabled)),
                //actions.cleave+=/exorcism,if=holy_power<=3|(holy_power=4&(cooldown.judgment.remains>=gcd*2&cooldown.crusader_strike.remains>=gcd*2&target.health.pct>35&buff.avenging_wrath.down))
                exorcism(() => (holy_power <= 3 || (holy_power == 4 && (cooldown.judgment.remains >= gcd * 2 && cooldown.crusader_strike.remains >= gcd * 2 && target.health.pct > 35 && buff.avenging_wrath.down)))),
                //actions.cleave+=/divine_storm,if=holy_power>=3&(!talent.seraphim.enabled|cooldown.seraphim.remains>gcd*6)&!talent.final_verdict.enabled
                divine_storm(() => (holy_power >= 3 && (!talent.seraphim.enabled || cooldown.seraphim.remains > gcd * 6) && !talent.final_verdict.enabled)),
                //actions.cleave+=/divine_storm,if=holy_power>=3&buff.final_verdict.up
                divine_storm(() => (holy_power >= 3 && buff.final_verdict.up)),
                //actions.cleave+=/final_verdict,if=holy_power>=3&buff.final_verdict.down
                final_verdict(() => (holy_power >= 3 && buff.final_verdict.down)),
                //actions.cleave+=/holy_prism,target=self
                holy_prism(() => Me, () => true),
                new ActionAlwaysFail()
                );
        }

        private static Composite RetributionSingle()
        {
            return new PrioritySelector(
                //actions.single=divine_storm,if=buff.divine_crusader.react&(holy_power=5|buff.holy_avenger.up&holy_power>=3)&buff.final_verdict.up
                divine_storm(() => (buff.divine_crusader.react && (holy_power == 5 || buff.holy_avenger.up && holy_power >= 3) && buff.final_verdict.up)),
                //actions.single+=/divine_storm,if=buff.divine_crusader.react&(holy_power=5|buff.holy_avenger.up&holy_power>=3)&spell_targets.divine_storm=2&!talent.final_verdict.enabled
                divine_storm(() => (buff.divine_crusader.react && (holy_power == 5 || buff.holy_avenger.up && holy_power >= 3) && spell_targets.divine_storm == 2 && !talent.final_verdict.enabled)),
                //actions.single+=/divine_storm,if=(holy_power=5|buff.holy_avenger.up&holy_power>=3)&spell_targets.divine_storm=2&buff.final_verdict.up
                divine_storm(() => ((holy_power == 5 || buff.holy_avenger.up && holy_power >= 3) && spell_targets.divine_storm == 2 && buff.final_verdict.up)),
                //actions.single+=/divine_storm,if=buff.divine_crusader.react&(holy_power=5|buff.holy_avenger.up&holy_power>=3)&(talent.seraphim.enabled&cooldown.seraphim.remains<gcd*4)
                divine_storm(() => (buff.divine_crusader.react && (holy_power == 5 || buff.holy_avenger.up && holy_power >= 3) && (talent.seraphim.enabled && cooldown.seraphim.remains < gcd * 4))),
                //actions.single+=/templars_verdict,if=(holy_power=5|buff.holy_avenger.up&holy_power>=3)&(buff.avenging_wrath.down|target.health.pct>35)&(!talent.seraphim.enabled|cooldown.seraphim.remains>gcd*4)
                templars_verdict(() => ((holy_power == 5 || buff.holy_avenger.up && holy_power >= 3) && (buff.avenging_wrath.down || target.health.pct > 35) && (!talent.seraphim.enabled || cooldown.seraphim.remains > gcd * 4))),
                //actions.single+=/templars_verdict,if=buff.divine_purpose.react&buff.divine_purpose.remains<3
                templars_verdict(() => (buff.divine_purpose.react && buff.divine_purpose.remains < 3)),
                //actions.single+=/divine_storm,if=buff.divine_crusader.react&buff.divine_crusader.remains<3&buff.final_verdict.up
                divine_storm(() => (buff.divine_crusader.react && buff.divine_crusader.remains < 3 && buff.final_verdict.up)),
                //actions.single+=/final_verdict,if=holy_power=5|buff.holy_avenger.up&holy_power>=3
                final_verdict(() => (holy_power == 5 || buff.holy_avenger.up && holy_power >= 3)),
                //actions.single+=/final_verdict,if=buff.divine_purpose.react&buff.divine_purpose.remains<3
                final_verdict(() => (buff.divine_purpose.react && buff.divine_purpose.remains < 3)),
                //actions.single+=/crusader_strike,if=t18_class_trinket=1&buff.focus_of_vengeance.remains<gcd.max*2
                crusader_strike(() => (t18_class_trinket && buff.focus_of_vengeance.remains < GCD_MAX * 2)),
                //actions.single+=/hammer_of_wrath
                hammer_of_wrath(() => true),
                //actions.single+=/exorcism,if=buff.blazing_contempt.up&holy_power<=2&buff.holy_avenger.down
                exorcism(() => (buff.blazing_contempt.up && holy_power <= 2 && buff.holy_avenger.down)),
                //actions.single+=/divine_storm,if=buff.divine_crusader.react&buff.final_verdict.up&(buff.avenging_wrath.up|target.health.pct<35)
                divine_storm(() => (buff.divine_crusader.react && buff.final_verdict.up && (buff.avenging_wrath.up || target.health.pct < 35))),
                //actions.single+=/divine_storm,if=spell_targets.divine_storm=2&buff.final_verdict.up&(buff.avenging_wrath.up|target.health.pct<35)
                divine_storm(() => (spell_targets.divine_storm == 2 && buff.final_verdict.up && (buff.avenging_wrath.up || target.health.pct < 35))),
                //actions.single+=/final_verdict,if=buff.avenging_wrath.up|target.health.pct<35
                final_verdict(() => (buff.avenging_wrath.up || target.health.pct < 35)),
                //actions.single+=/divine_storm,if=buff.divine_crusader.react&spell_targets.divine_storm=2&(buff.avenging_wrath.up|target.health.pct<35)&!talent.final_verdict.enabled
                divine_storm(() => (buff.divine_crusader.react && spell_targets.divine_storm == 2 && (buff.avenging_wrath.up || target.health.pct < 35) && !talent.final_verdict.enabled)),
                //actions.single+=/templars_verdict,if=holy_power=5&(buff.avenging_wrath.up|target.health.pct<35)&(!talent.seraphim.enabled|cooldown.seraphim.remains>gcd*3)
                templars_verdict(() => (holy_power == 5 && (buff.avenging_wrath.up || target.health.pct < 35) && (!talent.seraphim.enabled || cooldown.seraphim.remains > gcd * 3))),
                //actions.single+=/templars_verdict,if=holy_power=4&(buff.avenging_wrath.up|target.health.pct<35)&(!talent.seraphim.enabled|cooldown.seraphim.remains>gcd*4)
                templars_verdict(() => (holy_power == 4 && (buff.avenging_wrath.up || target.health.pct < 35) && (!talent.seraphim.enabled || cooldown.seraphim.remains > gcd * 4))),
                //actions.single+=/templars_verdict,if=holy_power=3&(buff.avenging_wrath.up|target.health.pct<35)&(!talent.seraphim.enabled|cooldown.seraphim.remains>gcd*5)
                templars_verdict(() => (holy_power == 3 && (buff.avenging_wrath.up || target.health.pct < 35) && (!talent.seraphim.enabled || cooldown.seraphim.remains > gcd * 5))),
                //actions.single+=/judgment,if=talent.empowered_seals.enabled&seal.truth&buff.maraads_truth.remains<cooldown.judgment.duration*1.5
                judgment(() => (talent.empowered_seals.enabled && buff.seal_of_truth.up && buff.maraads_truth.remains < cooldown.judgment.duration * 1.5)),
                //actions.single+=/judgment,if=talent.empowered_seals.enabled&seal.righteousness&buff.liadrins_righteousness.remains<cooldown.judgment.duration*1.5
                judgment(() => (talent.empowered_seals.enabled && buff.seal_of_righteousness.up && buff.liadrins_righteousness.remains < cooldown.judgment.duration * 1.5)),
                //actions.single+=/seal_of_truth,if=talent.empowered_seals.enabled&buff.maraads_truth.remains<(cooldown.judgment.duration|buff.maraads_truth.down)&(buff.avenging_wrath.up|target.health.pct<35)&!buff.wings_of_liberty.up
                // TODO Need to recheck this line
                seal_of_truth(() => (talent.empowered_seals.enabled && buff.maraads_truth.remains < (cooldown.judgment.duration * buff.maraads_truth.down.ToInt()) && (buff.avenging_wrath.up || target.health.pct < 35) && !buff.wings_of_liberty.up)),
                //actions.single+=/seal_of_righteousness,if=talent.empowered_seals.enabled&buff.liadrins_righteousness.remains<cooldown.judgment.duration&buff.maraads_truth.remains>cooldown.judgment.duration*1.5&target.health.pct<35&!buff.wings_of_liberty.up&!buff.bloodlust.up
                seal_of_righteousness(
                    () =>
                        (talent.empowered_seals.enabled && buff.liadrins_righteousness.remains < cooldown.judgment.duration && buff.maraads_truth.remains > cooldown.judgment.duration * 1.5 && target.health.pct < 35 && !buff.wings_of_liberty.up &&
                         !buff.bloodlust.up)),
                //actions.single+=/crusader_strike,if=talent.seraphim.enabled
                crusader_strike(() => (talent.seraphim.enabled)),
                //actions.single+=/crusader_strike,if=holy_power<=3|(holy_power=4&target.health.pct>=35&buff.avenging_wrath.down)
                crusader_strike(() => (holy_power <= 3 || (holy_power == 4 && target.health.pct >= 35 && buff.avenging_wrath.down))),
                //actions.single+=/divine_storm,if=buff.divine_crusader.react&(buff.avenging_wrath.up|target.health.pct<35)&!talent.final_verdict.enabled
                divine_storm(() => (buff.divine_crusader.react && (buff.avenging_wrath.up || target.health.pct < 35) && !talent.final_verdict.enabled)),
                //actions.single+=/exorcism,if=glyph.mass_exorcism.enabled&spell_targets.exorcism>=2&!glyph.double_jeopardy.enabled&!set_bonus.tier17_4pc=1
                exorcism(() => (glyph.mass_exorcism.enabled && spell_targets.exorcism >= 2 && !glyph.double_jeopardy.enabled && !set_bonus.tier17_4pc)),
                //actions.single+=/judgment,cycle_targets=1,if=last_judgment_target!=target&talent.seraphim.enabled&glyph.double_jeopardy.enabled
                judgment(() => last_judgment_target != target.current && talent.seraphim.enabled && glyph.double_jeopardy.enabled),
                //actions.single+=/judgment,if=talent.seraphim.enabled
                judgment(() => (talent.seraphim.enabled)),
                //actions.single+=/judgment,cycle_targets=1,if=last_judgment_target!=target&glyph.double_jeopardy.enabled&(holy_power<=3|(holy_power=4&cooldown.crusader_strike.remains>=gcd*2&target.health.pct>35&buff.avenging_wrath.down))
                judgment(() => last_judgment_target != target.current && glyph.double_jeopardy.enabled && (holy_power <= 3 || (holy_power == 4 && cooldown.crusader_strike.remains >= gcd * 2 && target.health.pct > 35 && buff.avenging_wrath.down))),
                //actions.single+=/judgment,if=holy_power<=3|(holy_power=4&cooldown.crusader_strike.remains>=gcd*2&target.health.pct>35&buff.avenging_wrath.down)
                judgment(() => (holy_power <= 3 || (holy_power == 4 && cooldown.crusader_strike.remains >= gcd * 2 && target.health.pct > 35 && buff.avenging_wrath.down))),
                //actions.single+=/divine_storm,if=buff.divine_crusader.react&buff.final_verdict.up
                divine_storm(() => (buff.divine_crusader.react && buff.final_verdict.up)),
                //actions.single+=/divine_storm,if=spell_targets.divine_storm=2&holy_power>=4&buff.final_verdict.up
                divine_storm(() => (spell_targets.divine_storm == 2 && holy_power >= 4 && buff.final_verdict.up)),
                //actions.single+=/final_verdict,if=buff.divine_purpose.react
                final_verdict(() => (buff.divine_purpose.react)),
                //actions.single+=/final_verdict,if=holy_power>=4
                final_verdict(() => (holy_power >= 4)),
                //actions.single+=/seal_of_truth,if=talent.empowered_seals.enabled&buff.maraads_truth.remains<cooldown.judgment.duration*1.5&buff.liadrins_righteousness.remains>cooldown.judgment.duration*1.5
                seal_of_truth(() => talent.empowered_seals.enabled && buff.maraads_truth.remains < cooldown.judgment.duration * 1.5 && buff.liadrins_righteousness.remains > cooldown.judgment.duration * 1.5),
                //actions.single+=/seal_of_righteousness,if=talent.empowered_seals.enabled&buff.liadrins_righteousness.remains<cooldown.judgment.duration*1.5&buff.maraads_truth.remains>cooldown.judgment.duration*1.5&!buff.bloodlust.up
                seal_of_righteousness(() => talent.empowered_seals.enabled && buff.liadrins_righteousness.remains < cooldown.judgment.duration * 1.5 && buff.maraads_truth.remains > cooldown.judgment.duration * 1.5 && !buff.bloodlust.up),
                //actions.single+=/divine_storm,if=buff.divine_crusader.react&spell_targets.divine_storm=2&holy_power>=4&!talent.final_verdict.enabled
                divine_storm(() => buff.divine_crusader.react && spell_targets.divine_storm == 2 && holy_power >= 4 && !talent.final_verdict.enabled),
                //actions.single+=/templars_verdict,if=buff.divine_purpose.react
                templars_verdict(() => (buff.divine_purpose.react)),
                //actions.single+=/divine_storm,if=buff.divine_crusader.react&!talent.final_verdict.enabled
                divine_storm(() => (buff.divine_crusader.react && !talent.final_verdict.enabled)),
                //actions.single+=/templars_verdict,if=holy_power>=4&(!talent.seraphim.enabled|cooldown.seraphim.remains>gcd*5)
                templars_verdict(() => (holy_power >= 4 && (!talent.seraphim.enabled || cooldown.seraphim.remains > gcd * 5))),
                //actions.single+=/exorcism,if=talent.seraphim.enabled
                exorcism(() => (talent.seraphim.enabled)),
                //actions.single+=/exorcism,if=holy_power<=3|(holy_power=4&(cooldown.judgment.remains>=gcd*2&cooldown.crusader_strike.remains>=gcd*2&target.health.pct>35&buff.avenging_wrath.down))
                exorcism(() => (holy_power <= 3 || (holy_power == 4 && (cooldown.judgment.remains > gcd * 2 && cooldown.crusader_strike.remains >= gcd * 2 && target.health.pct > 35 && buff.avenging_wrath.down)))),
                //actions.single+=/divine_storm,if=spell_targets.divine_storm=2&holy_power>=3&buff.final_verdict.up
                divine_storm(() => (spell_targets.divine_storm == 2 && holy_power >= 3 && buff.final_verdict.up)),
                //actions.single+=/final_verdict,if=holy_power>=3
                final_verdict(() => (holy_power >= 3)),
                //actions.single+=/templars_verdict,if=holy_power>=3&(!talent.seraphim.enabled|cooldown.seraphim.remains>gcd*6)
                templars_verdict(() => (holy_power >= 3 && (!talent.seraphim.enabled || cooldown.seraphim.remains > gcd * 6))),
                //actions.single+=/holy_prism
                holy_prism(() => Me.CurrentTarget, () => true),
                new ActionAlwaysFail()
                );
        }

        #endregion

        // ReSharper disable MemberHidesStaticFromOuterClass
        // ReSharper disable UnusedMember.Local

        #region Types

        public static class PalSpells
        {
            // ReSharper disable UnusedMember.Local

            #region Fields

            public const string avenging_wrath = "Avenging Wrath";
            public const string benediction_of_kings = "Benediction of Kings";
            public const string benediction_of_might = "Benediction of Might";
            public const int blazing_contempt = 166831;
            public const string censure = "Censure";
            public const string crusader_strike = "Crusader Strike";
            public const int divine_crusader = 144595;
            public const string divine_protection = "Divine Protection";
            public const int divine_purpose = 86172;
            public const string divine_shield = "Divine Shield";
            public const string divine_storm = "Divine Storm";
            public const string double_jeopardy = "Glyph of Double Jeopardy";
            public const string empowered_seals = "Empowered Seals";
            public const string execution_sentence = "Execution Sentence";
            public const string exorcism = "Exorcism";
            public const string final_verdict = "Final Verdict";
            public const string focus_of_vengeance = "Focus of Vengeance"; // 184911
            public const string hammer_of_the_righteous = "Hammer of the Righteous";
            public const string hammer_of_wrath = "Hammer of Wrath";
            public const string holy_avenger = "Holy Avenger";
            public const string holy_light = "Holy Light";
            public const string holy_prism = "Holy Prism";
            public const string judgment = "Judgment";
            public const string lay_on_hands = "Lay on Hands";
            public const string liadrins_righteousness = "Liadrin's Righteousness";
            public const string lights_hammer = "Light's Hammer";
            public const string maraads_truth = "Maraad's Truth";
            public const string mass_exorcism = "Glyph of Mass Exorcism";
            public const string seal_of_command = "Seal of Command";
            public const string seal_of_insight = "Seal of Insight";
            public const string seal_of_righteousness = "Seal of Righteousness";
            public const string seal_of_truth = "Seal of Truth";
            public const string seraphim = "Seraphim";
            public const string templars_verdict = "Templar's Verdict";
            public const string wings_of_liberty = "Wings of Liberty"; // 185647
            public const string word_of_glory = "Word of Glory";

            #endregion

            // ReSharper restore UnusedMember.Local
        }

        private static class spell_targets
        {
            #region Properties

            public static int divine_storm
            {
                get { return EnemiesCountNearTarget(Me, buff.final_verdict.up ? DIVINE_STORM_EMPOWERED_DISTANCE : DIVINE_STORM_DISTANCE); }
            }

            public static int exorcism
            {
                get { return EnemiesCountNearTarget(Me.CurrentTarget, EXORCISM_DISTANCE); }
            }

            public static int hammer_of_the_righteous
            {
                get { return EnemiesCountNearTarget(Me.CurrentTarget, HAMMER_OF_THE_RIGHTEOUS_DISTANCE); }
            }

            #endregion
        }

        private class buff : BuffBase
        {
            #region Fields

            public static readonly buff avenging_wrath = new buff(PalSpells.avenging_wrath);

            public static readonly buff benediction_of_kings = new buff(PalSpells.benediction_of_kings);

            public static readonly buff benediction_of_might = new buff(PalSpells.benediction_of_might);

            public static readonly buff blazing_contempt = new buff(PalSpells.blazing_contempt);

            public static readonly buff bloodlust = new buff(Common.Common.bloodlust);

            public static readonly buff divine_crusader = new buff(PalSpells.divine_crusader);

            public static readonly buff divine_protection = new buff(PalSpells.divine_protection);

            public static readonly buff divine_purpose = new buff(PalSpells.divine_purpose);

            public static readonly buff divine_shield = new buff(PalSpells.divine_shield);

            public static readonly buff final_verdict = new buff(PalSpells.final_verdict);

            public static readonly buff focus_of_vengeance = new buff(PalSpells.focus_of_vengeance);

            public static readonly buff holy_avenger = new buff(PalSpells.holy_avenger);

            public static readonly buff liadrins_righteousness = new buff(PalSpells.liadrins_righteousness);

            public static readonly buff maraads_truth = new buff(PalSpells.maraads_truth);

            public static readonly buff seal_of_command = new buff(PalSpells.seal_of_command);

            public static readonly buff seal_of_insight = new buff(PalSpells.seal_of_insight);

            public static readonly buff seal_of_righteousness = new buff(PalSpells.seal_of_righteousness);

            public static readonly buff seal_of_truth = new buff(PalSpells.seal_of_truth);

            public static readonly buff seraphim = new buff(PalSpells.seraphim);

            public static readonly buff wings_of_liberty = new buff(PalSpells.wings_of_liberty);

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

            public static readonly cooldown avenging_wrath = new cooldown(PalSpells.avenging_wrath);

            public static readonly cooldown crusader_strike = new cooldown(PalSpells.crusader_strike);

            public static readonly cooldown divine_protection = new cooldown(PalSpells.divine_protection);

            public static readonly cooldown divine_shield = new cooldown(PalSpells.divine_shield);

            public static readonly cooldown execution_sentence = new cooldown(PalSpells.execution_sentence);

            public static readonly cooldown judgment = new cooldown(PalSpells.judgment);

            public static readonly cooldown lay_on_hands = new cooldown(PalSpells.lay_on_hands);

            public static readonly cooldown lights_hammer = new cooldown(PalSpells.lights_hammer);

            public static readonly cooldown seraphim = new cooldown(PalSpells.seraphim);

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

            public static readonly debuff censure = new debuff(PalSpells.censure);

            public static readonly debuff execution_sentence = new debuff(PalSpells.execution_sentence);

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

            public static readonly dot censure = new dot(PalSpells.censure);

            public static readonly dot execution_sentence = new dot(PalSpells.execution_sentence);

            #endregion

            #region Constructors

            private dot(string spell)
                : base(spell)
            {
            }

            #endregion
        }

        private class glyph : GlyphBase
        {
            #region Fields

            public static readonly glyph double_jeopardy = new glyph(PalSpells.double_jeopardy);

            public static readonly glyph mass_exorcism = new glyph(PalSpells.mass_exorcism);

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

            public static readonly talent divine_purpose = new talent(PalTalentsEnum.DivinePurpose);

            public static readonly talent empowered_seals = new talent(PalTalentsEnum.EmpoweredSeals);

            public static readonly talent execution_sentence = new talent(PalTalentsEnum.ExecutionSentence);

            public static readonly talent final_verdict = new talent(PalTalentsEnum.FinalVerdict);

            public static readonly talent holy_avenger = new talent(PalTalentsEnum.HolyAvenger);

            public static readonly talent holy_prism = new talent(PalTalentsEnum.HolyPrism);

            public static readonly talent lights_hammer = new talent(PalTalentsEnum.LightsHammer);

            public static readonly talent sanctified_wrath = new talent(PalTalentsEnum.SanctifiedWrath);

            public static readonly talent seraphim = new talent(PalTalentsEnum.Seraphim);

            #endregion

            #region Constructors

            private talent(PalTalentsEnum talent)
                : base((int) talent)
            {
            }

            #endregion
        }

        #endregion
    }
}