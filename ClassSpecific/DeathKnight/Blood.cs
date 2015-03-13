using Singular.Dynamics;
using Singular.Helpers;
using Singular.Managers;
using Styx;
using Styx.TreeSharp;
using Action = Styx.TreeSharp.Action;

namespace Singular.ClassSpecific.DeathKnight
{
    public class Blood : DeathKnightBase
    {
        private const string death_pact = "Death Pact";

        #region Public Methods

        [Behavior(BehaviorType.CombatBuffs, WoWClass.DeathKnight, WoWSpec.DeathKnightBlood)]
        public static Composite CreateDeathKnightBloodCombatBuffs()
        {
            return CreateDeathKnightBloodSimCCombat();
        }

        [Behavior(BehaviorType.Heal, WoWClass.DeathKnight, WoWSpec.DeathKnightBlood, WoWContext.All, priority: 1)]
        public static Composite CreateDeathKnightBloodHealsDiagnostic()
        {
            return CreateDeathKnightBloodSimCCombat();
        }

        [Behavior(BehaviorType.Pull, WoWClass.DeathKnight, WoWSpec.DeathKnightBlood, WoWContext.Instances)]
        public static Composite CreateDeathKnightBloodInstancePull()
        {
            return CreateDeathKnightBloodSimCCombat();
        }

        [Behavior(BehaviorType.Combat, WoWClass.DeathKnight, WoWSpec.DeathKnightBlood, WoWContext.Normal)]
        public static Composite CreateDeathKnightBloodNormalCombat()
        {
            return CreateDeathKnightBloodSimCCombat();
        }

        [Behavior(BehaviorType.Pull, WoWClass.DeathKnight, WoWSpec.DeathKnightBlood, WoWContext.All, priority: 1)]
        public static Composite CreateDeathKnightBloodPullDiagnostic()
        {
            return CreateDeathKnightBloodSimCCombat();
        }

        [Behavior(BehaviorType.Combat, WoWClass.DeathKnight, WoWSpec.DeathKnightBlood, WoWContext.Battlegrounds)]
        public static Composite CreateDeathKnightBloodPvPCombat()
        {
            return CreateDeathKnightBloodSimCCombat();
        }

        [Behavior(BehaviorType.Combat, WoWClass.DeathKnight, WoWSpec.DeathKnightBlood, WoWContext.Instances)]
        public static Composite CreateDeathKnightBloodSimCCombat()
        {
            Generic.SuppressGenericRacialBehavior = true;
            TankManager.NeedTankTargeting = (SingularRoutine.CurrentWoWContext == WoWContext.Instances);

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
                            Common.Scenario.Update(StyxWoW.Me.CurrentTarget);
                            return RunStatus.Failure;
                        }),
                        Helpers.Common.CreateInterruptBehavior(),
                        Movement.WaitForFacing(),
                        Movement.WaitForLineOfSpellSight(),

                        new PrioritySelector(
                            //# Executed every time the actor is available.
                            //actions=auto_attack
                            //actions+=/blood_fury
                            //actions+=/berserking
                            //actions+=/arcane_torrent
                            //actions+=/potion,name=draenic_armor,if=buff.potion.down&buff.blood_shield.down&!unholy&!frost
                            //actions+=/antimagic_shell
                            Spell.BuffSelf(antimagic_shell),
                            //actions+=/conversion,if=!buff.conversion.up&runic_power>50&health.pct<90
                            Spell.BuffSelf(conversion, req => !buff.conversion.up && runic_power > 50 && health_pct < 90),
                            //actions+=/lichborne,if=health.pct<90
                            Spell.BuffSelf(lichborne, ret => health_pct < 90),
                            //actions+=/death_strike,if=incoming_damage_5s>=health.max*0.65
                            //actions+=/army_of_the_dead,if=buff.bone_shield.down&buff.dancing_rune_weapon.down&buff.icebound_fortitude.down&buff.vampiric_blood.down
                            Spell.BuffSelf(army_of_the_dead,
                                ret =>
                                    buff.bone_shield.down && buff.dancing_rune_weapon.down &&
                                    buff.icebound_fortitude.down && buff.vampiric_blood.down),
                            //actions+=/bone_shield,if=buff.army_of_the_dead.down&buff.bone_shield.down&buff.dancing_rune_weapon.down&buff.icebound_fortitude.down&buff.vampiric_blood.down
                            Spell.BuffSelf(bone_shield,
                                ret =>
                                    buff.army_of_the_dead.down && buff.bone_shield.down && buff.dancing_rune_weapon.down &&
                                    buff.icebound_fortitude.down && buff.vampiric_blood.down),
                            //actions+=/vampiric_blood,if=health.pct<50
                            Spell.BuffSelf(vampiric_blood, req => health_pct < 50),
                            //actions+=/icebound_fortitude,if=health.pct<30&buff.army_of_the_dead.down&buff.dancing_rune_weapon.down&buff.bone_shield.down&buff.vampiric_blood.down
                            Spell.BuffSelf(icebound_fortitude,
                                req =>
                                    health_pct < 30 && buff.army_of_the_dead.down && buff.dancing_rune_weapon.down &&
                                    buff.bone_shield.down && buff.vampiric_blood.down),
                            //actions+=/rune_tap,if=health.pct<50&buff.army_of_the_dead.down&buff.dancing_rune_weapon.down&buff.bone_shield.down&buff.vampiric_blood.down&buff.icebound_fortitude.down
                            Spell.BuffSelf(rune_tap,
                                req =>
                                    health_pct < 50 && buff.army_of_the_dead.down && buff.dancing_rune_weapon.down &&
                                    buff.bone_shield.down && buff.vampiric_blood.down && buff.icebound_fortitude.down),
                            //actions+=/dancing_rune_weapon,if=health.pct<80&buff.army_of_the_dead.down&buff.icebound_fortitude.down&buff.bone_shield.down&buff.vampiric_blood.down
                            Spell.Cast(dancing_rune_weapon,
                                req =>
                                    health_pct < 80 && buff.army_of_the_dead.down && buff.icebound_fortitude.down &&
                                    buff.bone_shield.down && buff.vampiric_blood.down),
                            //actions+=/death_pact,if=health.pct<50
                            Spell.BuffSelf(death_pact, req => health_pct < 30),
                            //actions+=/outbreak,if=(!talent.necrotic_plague.enabled&disease.min_remains<8)|!disease.ticking
                            Spell.Cast(outbreak,
                                req => (!talent.necrotic_plague_enabled && disease.min_remains < 8) || !disease.ticking),
                            //actions+=/death_coil,if=runic_power>90
                            Spell.Cast(death_coil, req => runic_power > 90),
                            //actions+=/plague_strike,if=(!talent.necrotic_plague.enabled&!dot.blood_plague.ticking)|(talent.necrotic_plague.enabled&!dot.necrotic_plague.ticking)
                            Spell.Buff(plague_strike,
                                req =>
                                    (!talent.necrotic_plague_enabled && !dot.blood_plague_ticking) ||
                                    (talent.necrotic_plague_enabled && !dot.necrotic_plague_ticking)),
                            //actions+=/icy_touch,if=(!talent.necrotic_plague.enabled&!dot.frost_fever.ticking)|(talent.necrotic_plague.enabled&!dot.necrotic_plague.ticking)
                            Spell.Buff(icy_touch,
                                req =>
                                    (!talent.necrotic_plague_enabled && !dot.frost_fever_ticking) ||
                                    (talent.necrotic_plague_enabled && !dot.necrotic_plague_ticking)),
                            //actions+=/defile
                            Spell.CastOnGround(defile, on => Me.CurrentTarget, req => Spell.UseAOE),
                            //actions+=/plague_leech,if=((!blood&!unholy)|(!blood&!frost)|(!unholy&!frost))&cooldown.outbreak.remains<=gcd
                            Spell.Cast(plague_leech,
                                req => ((blood == 0 && unholy == 0) || (blood == 0 && frost == 0) || (unholy == 0 && frost == 0)) & cooldown.outbreak_remains <= 1),
                            //actions+=/call_action_list,name=bt,if=talent.blood_tap.enabled
                            //actions+=/call_action_list,name=re,if=talent.runic_empowerment.enabled
                            //actions+=/call_action_list,name=rc,if=talent.runic_corruption.enabled
                            //actions+=/call_action_list,name=nrt,if=!talent.blood_tap.enabled&!talent.runic_empowerment.enabled&!talent.runic_corruption.enabled
                            //actions+=/defile,if=buff.crimson_scourge.react
                            Spell.CastOnGround(defile, on => Me.CurrentTarget, req => Spell.UseAOE && buff.crimson_scourge_react),
                            //actions+=/death_and_decay,if=buff.crimson_scourge.react
                            Spell.CastOnGround(defile, on => Me.CurrentTarget, req => Spell.UseAOE && buff.crimson_scourge_react),
                            //actions+=/blood_boil,if=buff.crimson_scourge.react
                            Spell.Cast(blood_boil, on => Me.CurrentTarget, req => Spell.UseAOE && buff.crimson_scourge_react),
                            //actions+=/death_coil
                            Spell.Cast(death_coil),
                            //actions+=/empower_rune_weapon,if=!blood&!unholy&!frost
                            Spell.Cast(empower_rune_weapon, req => blood == 0 && unholy == 0 && frost == 0)

                            )
                        )
                    )
                );
        }

        #endregion
    }
}