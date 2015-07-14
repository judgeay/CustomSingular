using System.Linq;
using CommonBehaviors.Actions;
using Singular.Dynamics;
using Singular.Helpers;
using Singular.Managers;
using Styx;
using Styx.TreeSharp;
using Styx.WoWInternals.WoWObjects;

namespace Singular.ClassSpecific
{
    // ReSharper disable ClassNeverInstantiated.Global
    // ReSharper disable InconsistentNaming
    public class DeathKnight : Common
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

        public const string blood_boil = "Blood Boil";

        //private const string antimagic_shell = "Anti-Magic Shell";
        //private const string army_of_the_dead = "Army of the Dead";
        private const string blood_charge = "Blood Charge";
        private const string blood_plague = "Blood Plague";
        private const string blood_tap = "Blood Tap";
        private const string bone_shield = "Bone Shield";
        private const string breath_of_sindragosa = "Breath of Sindragosa";
        //private const string conversion = "Conversion";
        //private const int crimson_scourge = 81141;
        //private const string dancing_rune_weapon = "Dancing Rune Weapon";
        private const string dark_transformation = "Dark Transformation";
        private const string death_and_decay = "Death and Decay";
        private const string death_coil = "Death Coil";
        private const string death_grip = "Death Grip";
        private const string defile = "Defile";
        private const string empower_rune_weapon = "Empower Rune Weapon";
        private const string festering_strike = "Festering Strike";
        //private const int freezing_fog = 59052;
        private const string frost_fever = "Frost Fever";
        private const string horn_of_winter = "Horn of Winter";
        //private const string icebound_fortitude = "Icebound Fortitude";
        private const string icy_touch = "Icy Touch";
        //private const int killing_machine = 51124;
        //private const string lichborne = "Lichborne";
        private const string necrotic_plague = "Necrotic Plague";
        private const string outbreak = "Outbreak";
        //private const string pillar_of_frost = "Pillar of Frost";
        private const string plague_leech = "Plague Leech";
        private const string plague_strike = "Plague Strike";
        private const string raise_dead = "Raise Dead";
        //private const string rune_tap = "Rune Tap";
        //private const string runic_empowerment = "Runic Empowerment";
        private const string scourge_strike = "Scourge Strike";
        private const string shadow_infusion = "Shadow Infusion";
        private const string soul_reaper = "Soul Reaper";
        private const int sudden_doom = 81340;
        private const string summon_gargoyle = "Summon Gargoyle";
        private const string unholy_blight = "Unholy Blight";
        //private const string vampiric_blood = "Vampiric Blood";

        #endregion

        #region Properties

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
            get { return StyxWoW.Me.CurrentRunicPower; }
        }

        private static int unholy
        {
            get { return Me.GetRuneCount(4) + Me.GetRuneCount(5); }
        }

        #endregion

        #region Public Methods

        [Behavior(BehaviorType.PreCombatBuffs | BehaviorType.PullBuffs, WoWClass.DeathKnight)]
        public static Composite Buffs()
        {
            return new PrioritySelector(
                Spell.BuffSelf(bone_shield, req => Me.Specialization == WoWSpec.DeathKnightBlood),
                Spell.BuffSelf(horn_of_winter, req => !Me.HasPartyBuff(PartyBuffType.AttackPower)),
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
                        Spell.Cast(death_grip,
                            req =>
                                !MovementManager.IsMovementDisabled && !Me.CurrentTarget.IsBoss() && Me.CurrentTarget.DistanceSqr > 10 * 10 &&
                                (Me.CurrentTarget.IsPlayer || Me.CurrentTarget.TaggedByMe ||
                                 (!Me.CurrentTarget.TaggedByOther && CompositeBuilder.CurrentBehaviorType == BehaviorType.Pull && SingularRoutine.CurrentWoWContext != WoWContext.Instances))),
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
                        new Decorator(unholy_action_list()),

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

        private static Composite bos()
        {
            return new PrioritySelector(
                // actions.bos=blood_fury,if=dot.breath_of_sindragosa.ticking
                // actions.bos+=/berserking,if=dot.breath_of_sindragosa.ticking
                // actions.bos+=/potion,name=draenic_strength,if=dot.breath_of_sindragosa.ticking
                // actions.bos+=/unholy_blight,if=!disease.ticking
                // actions.bos+=/plague_strike,if=!disease.ticking
                // actions.bos+=/blood_boil,cycle_targets=1,if=(spell_targets.blood_boil>=2&!(dot.blood_plague.ticking|dot.frost_fever.ticking))|spell_targets.blood_boil>=4&(runic_power<88&runic_power>30)
                // actions.bos+=/death_and_decay,if=spell_targets.death_and_decay>=2&(runic_power<88&runic_power>30)
                // actions.bos+=/festering_strike,if=(blood=2&frost=2&(((Frost-death)>0)|((Blood-death)>0)))&runic_power<80
                // actions.bos+=/festering_strike,if=((blood=2|frost=2)&(((Frost-death)>0)&((Blood-death)>0)))&runic_power<80
                // actions.bos+=/arcane_torrent,if=runic_power<70
                // actions.bos+=/scourge_strike,if=spell_targets.blood_boil<=3&(runic_power<88&runic_power>30)
                // actions.bos+=/blood_boil,if=spell_targets.blood_boil>=4&(runic_power<88&runic_power>30)
                // actions.bos+=/festering_strike,if=runic_power<77
                // actions.bos+=/scourge_strike,if=(spell_targets.blood_boil>=4&(runic_power<88&runic_power>30))|spell_targets.blood_boil<=3
                // actions.bos+=/dark_transformation
                // actions.bos+=/blood_tap,if=buff.blood_charge.stack>=5
                // actions.bos+=/plague_leech
                // actions.bos+=/empower_rune_weapon,if=runic_power<60
                // actions.bos+=/death_coil,if=buff.sudden_doom.react

                new ActionAlwaysFail()
                );
        }

        private static Composite unholy_action_list()
        {
            return new PrioritySelector(
                // actions.unholy=plague_leech,if=((cooldown.outbreak.remains<1)|disease.min_remains<1)&((blood<1&frost<1)|(blood<1&unholy<1)|(frost<1&unholy<1))
                Spell.Cast(plague_leech, req => (((cooldown.outbreak_remains < 1) || disease.min_remains < 1) && ((blood < 1 && frost < 1) || (blood < 1 && unholy < 1) || (frost < 1 && unholy < 1))) && disease.min_ticking),
                // actions.unholy+=/soul_reaper,if=(target.health.pct-3*(target.health.pct%target.time_to_die))<=45
                Spell.Cast(soul_reaper, req => target.health_pct <= 46),
                // actions.unholy+=/blood_tap,if=((target.health.pct-3*(target.health.pct%target.time_to_die))<=45)&cooldown.soul_reaper.remains=0
                Spell.Cast(blood_tap, req => (target.health_pct <= 46) && cooldown.soul_reaper_remains == 0),
                // actions.unholy+=/summon_gargoyle
                Spell.Cast(summon_gargoyle),
                // actions.unholy+=/breath_of_sindragosa,if=runic_power>75
                Spell.Buff(breath_of_sindragosa, req => runic_power > 75 && !Me.HasAura(breath_of_sindragosa)),
                // actions.unholy+=/run_action_list,name=bos,if=dot.breath_of_sindragosa.ticking
                new Decorator(req => talent.breath_of_sindragosa_enabled && dot.breath_of_sindragosa_ticking, bos()),
                // actions.unholy+=/unholy_blight,if=!disease.min_ticking
                Spell.BuffSelfAndWait(unholy_blight, req => !disease.min_ticking),
                // actions.unholy+=/outbreak,cycle_targets=1,if=!talent.necrotic_plague.enabled&(!(dot.blood_plague.ticking|dot.frost_fever.ticking))
                Spell.Cast(outbreak, req => !talent.necrotic_plague_enabled && (!(dot.blood_plague_ticking || dot.frost_fever_ticking))),
                // actions.unholy+=/plague_strike,if=(!talent.necrotic_plague.enabled&!(dot.blood_plague.ticking|dot.frost_fever.ticking))|(talent.necrotic_plague.enabled&!dot.necrotic_plague.ticking)
                Spell.Cast(plague_strike, req => (!talent.necrotic_plague_enabled && !(dot.blood_plague_ticking || dot.frost_fever_ticking)) || (talent.necrotic_plague_enabled && !dot.necrotic_plague_ticking)),
                // actions.unholy+=/blood_boil,cycle_targets=1,if=(spell_targets.blood_boil>1&!talent.necrotic_plague.enabled)&(!(dot.blood_plague.ticking|dot.frost_fever.ticking))
                Spell.Cast(blood_boil, req => Spell.UseAOE && (spell_targets.blood_boil > 1 & !talent.necrotic_plague_enabled) & (!(dot.blood_plague_ticking | dot.frost_fever_ticking))),
                // actions.unholy+=/death_and_decay,if=spell_targets.death_and_decay>1&unholy>1
                Spell.CastOnGround(death_and_decay, on => Me.CurrentTarget, req => Spell.UseAOE && spell_targets.death_and_decay > 1 && unholy > 1),
                // actions.unholy+=/defile,if=unholy=2
                Spell.CastOnGround(defile, on => Me.CurrentTarget, req => Spell.UseAOE && unholy == 2),
                // actions.unholy+=/blood_tap,if=talent.defile.enabled&cooldown.defile.remains=0
                Spell.Cast(blood_tap, req => talent.defile_enabled && cooldown.defile_remains == 0),
                // actions.unholy+=/scourge_strike,if=unholy=2
                Spell.Cast(scourge_strike, req => unholy == 2),
                // actions.unholy+=/festering_strike,if=talent.necrotic_plague.enabled&talent.unholy_blight.enabled&dot.necrotic_plague.remains<cooldown.unholy_blight.remains%2
                Spell.Cast(festering_strike, req => talent.necrotic_plague_enabled && talent.unholy_blight_enabled && dot.necrotic_plague_remains < cooldown.unholy_blight_remains % 2),
                // actions.unholy+=/dark_transformation
                Spell.Cast(dark_transformation, req => Me.Pet),
                // actions.unholy+=/festering_strike,if=blood=2&frost=2&(((Frost-death)>0)|((Blood-death)>0))
                Spell.Cast(festering_strike, req => blood == 2 && frost == 2 && (((frost - death) > 0) || ((blood - death) > 0))),
                // actions.unholy+=/festering_strike,if=(blood=2|frost=2)&(((Frost-death)>0)&((Blood-death)>0))
                Spell.Cast(festering_strike, req => (blood == 2 || frost == 2) && (((frost - death) > 0) && ((blood - death) > 0))),
                // actions.unholy+=/blood_boil,cycle_targets=1,if=(talent.necrotic_plague.enabled&!dot.necrotic_plague.ticking)&spell_targets.blood_boil>1
                Spell.Cast(blood_boil, req => Spell.UseAOE && (talent.necrotic_plague_enabled && !dot.necrotic_plague_ticking) && spell_targets.blood_boil > 1),
                // actions.unholy+=/defile,if=blood=2|frost=2
                Spell.CastOnGround(defile, on => Me.CurrentTarget, req => Spell.UseAOE && (blood == 2 || frost == 2)),
                // actions.unholy+=/death_and_decay,if=spell_targets.death_and_decay>1
                Spell.CastOnGround(death_and_decay, on => Me.CurrentTarget, req => Spell.UseAOE && spell_targets.death_and_decay > 1),
                // actions.unholy+=/defile
                Spell.CastOnGround(defile, on => Me.CurrentTarget, req => true),
                // actions.unholy+=/blood_boil,if=talent.breath_of_sindragosa.enabled&((spell_targets.blood_boil>=4&(blood=2|(frost=2&death=2)))&(cooldown.breath_of_sindragosa.remains>6|runic_power<75))
                Spell.Cast(blood_boil, req => Spell.UseAOE && (talent.breath_of_sindragosa_enabled && ((spell_targets.blood_boil >= 4 && (blood == 2 || (frost == 2 && death == 2))) && (cooldown.breath_of_sindragosa_remains > 6 || runic_power < 75)))),
                // actions.unholy+=/blood_boil,if=!talent.breath_of_sindragosa.enabled&(spell_targets.blood_boil>=4&(blood=2|(frost=2&death=2)))
                Spell.Cast(blood_boil, req => Spell.UseAOE && (!talent.breath_of_sindragosa_enabled && (spell_targets.blood_boil >= 4 && (blood == 2 || (frost == 2 && death == 2))))),
                // actions.unholy+=/blood_tap,if=buff.blood_charge.stack>10
                Spell.Cast(blood_tap, req => buff.blood_charge_stack > 10),
                // actions.unholy+=/outbreak,if=talent.necrotic_plague.enabled&debuff.necrotic_plague.stack<=14
                Spell.Cast(outbreak, req => talent.necrotic_plague_enabled && debuff.necrotic_plague_stack <= 14),
                // actions.unholy+=/death_coil,if=(buff.sudden_doom.react|runic_power>80)&(buff.blood_charge.stack<=10)
                Spell.Cast(death_coil, req => (buff.sudden_doom_react || runic_power > 80) && (buff.blood_charge_stack <= 10)),
                // actions.unholy+=/blood_boil,if=(spell_targets.blood_boil>=4&(cooldown.breath_of_sindragosa.remains>6|runic_power<75))|(!talent.breath_of_sindragosa.enabled&spell_targets.blood_boil>=4)
                Spell.Cast(blood_boil, req => Spell.UseAOE && (spell_targets.blood_boil >= 4 && (cooldown.breath_of_sindragosa_remains > 6 || runic_power < 75)) || (!talent.breath_of_sindragosa_enabled && spell_targets.blood_boil >= 4)),
                // actions.unholy+=/scourge_strike,if=(cooldown.breath_of_sindragosa.remains>6|runic_power<75|unholy=2)|!talent.breath_of_sindragosa.enabled
                Spell.Cast(scourge_strike, req => (cooldown.breath_of_sindragosa_remains > 6 || runic_power < 75 || unholy == 2) || !talent.breath_of_sindragosa_enabled),
                // actions.unholy+=/festering_strike,if=(cooldown.breath_of_sindragosa.remains>6|runic_power<75)|!talent.breath_of_sindragosa.enabled
                Spell.Cast(festering_strike, req => (cooldown.breath_of_sindragosa_remains > 6 || runic_power < 75) || !talent.breath_of_sindragosa_enabled),
                // actions.unholy+=/death_coil,if=(cooldown.breath_of_sindragosa.remains>20)|!talent.breath_of_sindragosa.enabled
                Spell.Cast(death_coil, req => (cooldown.breath_of_sindragosa_remains > 20) || !talent.breath_of_sindragosa_enabled),
                // actions.unholy+=/plague_leech
                Spell.Cast(plague_leech, req => disease.min_ticking),
                // actions.unholy+=/empower_rune_weapon,if=!talent.breath_of_sindragosa.enabled
                Spell.Cast(empower_rune_weapon, req => !talent.breath_of_sindragosa_enabled),

                new ActionAlwaysFail()
                );
        }

        private static Composite unholy_spread()
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

        #region Types

        private static class buff
        {
            #region Properties

            public static uint blood_charge_stack
            {
                get { return Stack(blood_charge); }
            }

            //public static bool crimson_scourge_react
            //{
            //    get { return React(crimson_scourge); }
            //}

            public static bool dark_transformation_down
            {
                get { return PetDown(dark_transformation); }
            }

            //public static bool killing_machine_react
            //{
            //    get { return React(killing_machine); }
            //}

            //public static bool rime_react
            //{
            //    get { return React(freezing_fog); }
            //}

            public static uint shadow_infusion_stack
            {
                get { return Stack(shadow_infusion); }
            }

            public static bool sudden_doom_react
            {
                get { return React(sudden_doom); }
            }

            #endregion

            #region Private Methods

            private static bool PetDown(string aura)
            {
                return !PetUp(aura);
            }

            private static bool PetUp(string aura)
            {
                return StyxWoW.Me.GotAlivePet && StyxWoW.Me.Pet.ActiveAuras.ContainsKey(aura);
            }

            private static bool React(int aura)
            {
                return StyxWoW.Me.HasAura(aura);
            }

            private static uint Stack(string aura)
            {
                return StyxWoW.Me.GetAuraStacks(aura);
            }

            #endregion
        }

        private static class cooldown
        {
            //public static double antimagic_shell_remains
            //{
            //    get { return Remains(antimagic_shell); }
            //}

            #region Properties

            public static double breath_of_sindragosa_remains
            {
                get { return Remains(breath_of_sindragosa); }
            }

            public static double defile_remains
            {
                get { return Remains(defile); }
            }

            public static double empower_rune_weapon_remains
            {
                get { return Remains(empower_rune_weapon); }
            }

            public static double outbreak_remains
            {
                get { return Remains(outbreak); }
            }

            //public static double pillar_of_frost_remains
            //{
            //    get { return Remains(pillar_of_frost); }
            //}

            public static double soul_reaper_remains
            {
                get { return Remains(soul_reaper); }
            }

            public static double unholy_blight_remains
            {
                get { return Remains(unholy_blight); }
            }

            #endregion

            #region Private Methods

            private static double Remains(string spell)
            {
                return Spell.GetSpellCooldown(spell).TotalSeconds;
            }

            #endregion
        }

        private static class debuff
        {
            public static uint necrotic_plague_stack
            {
                get { return StyxWoW.Me.CurrentTarget.GetAuraStacks(necrotic_plague); }
            }
        }

        private static class disease
        {
            #region Fields

            private static readonly string[] listBase = { blood_plague, frost_fever };
            private static readonly string[] listWithNecroticPlague = { necrotic_plague };

            #endregion

            //public static double max_remains
            //{
            //    get { return max_remains_on(StyxWoW.Me.CurrentTarget); }
            //}

            //public static bool max_ticking
            //{
            //    get { return max_ticking_on(StyxWoW.Me.CurrentTarget); }
            //}

            #region Properties

            public static double min_remains
            {
                get { return min_remains_on(StyxWoW.Me.CurrentTarget); }
            }

            public static bool min_ticking
            {
                get { return ticking; }
            }

            private static string[] diseaseArray
            {
                get { return talent.necrotic_plague_enabled ? listWithNecroticPlague : listBase; }
            }

            private static bool ticking
            {
                get { return ticking_on(StyxWoW.Me.CurrentTarget); }
            }

            #endregion

            #region Public Methods

            public static bool ticking_on(WoWUnit unit)
            {
                if (unit == null) return false;

                return unit.HasAllMyAuras(diseaseArray);
            }

            #endregion

            //private static double max_remains_on(WoWUnit unit)
            //{
            //    if (unit == null) return 0;

            //    var max = double.MinValue;

            //    // ReSharper disable once LoopCanBeConvertedToQuery
            //    foreach (var s in diseaseArray)
            //    {
            //        var rmn = unit.GetAuraTimeLeft(s).TotalSeconds;
            //        if (rmn > max)
            //            max = rmn;
            //    }

            //    if (max <= double.MinValue)
            //        max = 0;

            //    return max;
            //}

            //private static bool max_ticking_on(WoWUnit unit)
            //{
            //    if(unit == null) return false;

            //    return unit.HasAnyOfMyAuras(diseaseArray);
            //}

            #region Private Methods

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

        private static class dot
        {
            #region Properties

            public static bool blood_plague_ticking
            {
                get { return blood_plague_remains > 0; }
            }

            public static bool breath_of_sindragosa_ticking
            {
                get { return breath_of_sindragosa_remains > 0; }
            }

            public static bool frost_fever_ticking
            {
                get { return frost_fever_remains > 0; }
            }

            public static double necrotic_plague_remains
            {
                get { return Remains(necrotic_plague); }
            }

            public static bool necrotic_plague_ticking
            {
                get { return necrotic_plague_remains > 0; }
            }

            private static double blood_plague_remains
            {
                get { return Remains(blood_plague); }
            }

            private static double breath_of_sindragosa_remains
            {
                get { return Remains(breath_of_sindragosa); }
            }

            private static double frost_fever_remains
            {
                get { return Remains(frost_fever); }
            }

            #endregion

            #region Private Methods

            private static double Remains(string aura)
            {
                return StyxWoW.Me.CurrentTarget.GetAuraTimeLeft(aura).TotalSeconds;
            }

            #endregion
        }

        private static class spell_targets
        {
            public static int blood_boil
            {
                get { return active_enemies_list.Count(u => disease.ticking_on(u)) == 0 ? active_enemies_list.Where(u => disease.ticking_on(u) == false).Count() : 0; }
            }

            public static int death_and_decay
            {
                get { return active_enemies_list.Count(u => u.Distance <= 10); }
            }
        }

        private static class talent
        {
            //public static bool blood_tap_enabled
            //{
            //    get { return HasTalent(DeathKnightTalentsEnum.BloodTap); }
            //}

            #region Properties

            public static bool breath_of_sindragosa_enabled
            {
                get { return HasTalent(DeathKnightTalentsEnum.BreathOfSindragosa); }
            }

            public static bool defile_enabled
            {
                get { return HasTalent(DeathKnightTalentsEnum.Defile); }
            }

            public static bool necrotic_plague_enabled
            {
                get { return HasTalent(DeathKnightTalentsEnum.NecroticPlague); }
            }

            //public static bool runic_empowerment_enabled
            //{
            //    get { return HasTalent(DeathKnightTalentsEnum.RunicEmpowerment); }
            //}

            public static bool unholy_blight_enabled
            {
                get { return HasTalent(DeathKnightTalentsEnum.UnholyBlight); }
            }

            #endregion

            #region Private Methods

            private static bool HasTalent(DeathKnightTalentsEnum tal)
            {
                return TalentManager.IsSelected((int)tal);
            }

            #endregion
        }

        #endregion
    }
}