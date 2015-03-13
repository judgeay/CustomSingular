using Singular.Dynamics;
using Singular.Helpers;
using Singular.Managers;
using Styx;
using Styx.TreeSharp;
using Styx.WoWInternals.WoWObjects;

namespace Singular.ClassSpecific.DeathKnight
{
    public static class Common
    {
        #region Fields

        internal const int FreezingFog = 59052;
        internal const int KillingMachine = 51124;
        internal const int SuddenDoom = 81340;

        private static CombatScenario _scenario;

        #endregion

        #region Properties

        internal static int BloodRuneSlotsActive
        {
            get { return Me.GetRuneCount(0) + Me.GetRuneCount(1); }
        }

        internal static int DeathRuneSlotsActive
        {
            get { return Me.GetRuneCount(RuneType.Death); }
        }

        internal static int FrostRuneSlotsActive
        {
            get { return Me.GetRuneCount(2) + Me.GetRuneCount(3); }
        }

        internal static CombatScenario Scenario
        {
            get { return _scenario ?? (_scenario = new CombatScenario(40, 1.5f)); }
        }

        internal static int UnholyRuneSlotsActive
        {
            get { return Me.GetRuneCount(4) + Me.GetRuneCount(5); }
        }

        private static LocalPlayer Me
        {
            get { return StyxWoW.Me; }
        }

        #endregion

        #region Public Methods

        [Behavior(BehaviorType.CombatBuffs, WoWClass.DeathKnight, WoWSpec.DeathKnightUnholy, WoWContext.Normal)]
        [Behavior(BehaviorType.CombatBuffs, WoWClass.DeathKnight, WoWSpec.DeathKnightUnholy, WoWContext.Battlegrounds)]
        [Behavior(BehaviorType.CombatBuffs, WoWClass.DeathKnight, WoWSpec.DeathKnightFrost, WoWContext.Normal)]
        [Behavior(BehaviorType.CombatBuffs, WoWClass.DeathKnight, WoWSpec.DeathKnightFrost, WoWContext.Battlegrounds)]
        public static Composite CreateDeathKnightCombatBuffs()
        {
            return null;
        }


        [Behavior(BehaviorType.Pull, WoWClass.DeathKnight, WoWSpec.DeathKnightUnholy, WoWContext.Instances)]
        [Behavior(BehaviorType.Pull, WoWClass.DeathKnight, WoWSpec.DeathKnightFrost, WoWContext.Instances)]
        public static Composite CreateDeathKnightFrostAndUnholyInstancePull()
        {
            return null;
        }

        [Behavior(BehaviorType.Heal, WoWClass.DeathKnight)]
        public static Composite CreateDeathKnightHeals()
        {
            return null;
        }

        [Behavior(BehaviorType.LossOfControl, WoWClass.DeathKnight)]
        public static Composite CreateDeathKnightLossOfControlBehavior()
        {
            return null;
        }

        [Behavior(BehaviorType.Pull, WoWClass.DeathKnight, (WoWSpec) int.MaxValue,
            WoWContext.Normal | WoWContext.Battlegrounds)]
        public static Composite CreateDeathKnightNormalAndPvPPull()
        {
            return null;
        }

        [Behavior(BehaviorType.PreCombatBuffs, WoWClass.DeathKnight)]
        public static Composite CreateDeathKnightPreCombatBuffs()
        {
            return null;
        }

        [Behavior(BehaviorType.PullBuffs, WoWClass.DeathKnight)]
        public static Composite CreateDeathKnightPullBuffs()
        {
            return null;
        }

        [Behavior(BehaviorType.Initialize, WoWClass.DeathKnight, priority: 9999)]
        public static Composite CreateUnholyDeathKnightInitialize()
        {
            return null;
        }

        [Behavior(BehaviorType.Initialize, WoWClass.DeathKnight)]
        public static Composite DeathKnightInitializeBehavior()
        {
            return null;
        }

        public static bool HasTalent(DeathKnightTalents tal)
        {
            return TalentManager.IsSelected((int) tal);
        }

        #endregion
    }

    public enum DeathKnightTalents
    {
#if PRE_WOD
        RollingBlood = 1,
        PlagueLeech,
        UnholyBlight,
        LichBorne,
        AntiMagicZone,
        Purgatory,
        DeathsAdvance,
        Chilblains,
        Asphyxiate,
        DeathPact,
        DeathSiphon,
        Conversion,
        BloodTap,
        RunicEmpowerment,
        RunicCorruption,
        GorefiendsGrasp,
        RemorselessWinter,
        DesecratedGround
#else

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

#endif
    }
}