//using Singular.Helpers;

using Singular.Helpers;
using Styx;
using Styx.WoWInternals.WoWObjects;

namespace Singular.ClassSpecific.DeathKnight
{
    public abstract class DeathKnightBase : CommonBase
    {
        #region Fields

        public const string antimagic_shell = "Anti-Magic Shell";
        public const string army_of_the_dead = "Army of the Dead";
        public const string blood_charge = "Blood Charge";
        public const string bone_shield = "Bone Shield";
        public const string breath_of_sindragosa = "Breath of Sindragosa";
        public const string conversion = "Conversion";
        public const string crimson_sourge = "Crimson Scourge";
        public const string dancing_rune_weapon = "Dancing Rune Weapon";
        public const string dark_transformation = "Dark Transformation";
        public const string defile = "Defile";
        public const string empower_rune_weapon = "Empower Rune Weapon";
        public const string icebound_fortitude = "Icebound Fortitude";
        public const string outbreak = "Outbreak";
        public const string pillar_of_frost = "Pillar of Frost";
        public const string shadow_infusion = "Shadow Infusion";
        public const string soul_reaper = "Soul Reaper";
        public const string unholy_blight = "Unholy Blight";
        public const string vampiric_blood = "Vampiric Blood";

        protected const string blood_boil = "Blood Boil";
        protected const string blood_tap = "Blood Tap";
        protected const string death_and_decay = "Death and Decay";
        protected const string death_coil = "Death Coil";
        protected const string festering_strike = "Festering Strike";
        protected const string icy_touch = "Icy Touch";
        protected const string lichborne = "Lichborne";
        protected const string plague_leech = "Plague Leech";
        protected const string plague_strike = "Plague Strike";
        protected const string rune_tap = "Rune Tap";
        protected const string scourge_strike = "Scourge Strike";
        protected const string summon_gargoyle = "Summon Gargoyle";

        #endregion

        #region Properties

        protected static int blood
        {
            get { return Common.BloodRuneSlotsActive; }
        }

        protected static int death
        {
            get { return Common.DeathRuneSlotsActive; }
        }

        protected static int frost
        {
            get { return Common.FrostRuneSlotsActive; }
        }

        protected static uint runic_power
        {
            get { return StyxWoW.Me.CurrentRunicPower; }
        }

        protected static int unholy
        {
            get { return Common.UnholyRuneSlotsActive; }
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

    internal static class buff
    {
        #region Fields

        public static readonly BuffBase antimagic_shell;
        public static readonly BuffBase army_of_the_dead;
        public static readonly BuffBase blood_charge;
        public static readonly BuffBase bone_shield;
        public static readonly BuffBase conversion;
        public static readonly BuffBase dancing_rune_weapon;
        public static readonly PetBuffBase dark_transformation;
        public static readonly BuffBase icebound_fortitude;
        public static readonly BuffBase shadow_infusion;
        public static readonly BuffBase vampiric_blood;

        #endregion

        #region Constructors

        static buff()
        {
            antimagic_shell = new BuffBase(DeathKnightBase.antimagic_shell);
            blood_charge = new BuffBase(DeathKnightBase.blood_charge);
            bone_shield = new BuffBase(DeathKnightBase.bone_shield);
            conversion = new BuffBase(DeathKnightBase.conversion);
            dancing_rune_weapon = new BuffBase(DeathKnightBase.dancing_rune_weapon);
            dark_transformation = new PetBuffBase(DeathKnightBase.dark_transformation);
            icebound_fortitude = new BuffBase(DeathKnightBase.icebound_fortitude);
            vampiric_blood = new BuffBase(DeathKnightBase.vampiric_blood);
            shadow_infusion = new BuffBase(DeathKnightBase.shadow_infusion);
            army_of_the_dead = new BuffBase(DeathKnightBase.army_of_the_dead);
        }

        #endregion

        #region Properties

        public static bool crimson_scourge_react
        {
            get { return StyxWoW.Me.HasAura(DeathKnightBase.crimson_sourge); }
        }

        public static bool killing_machine_react
        {
            get { return StyxWoW.Me.HasAura(Common.KillingMachine); }
        }

        public static bool rime_react
        {
            get { return StyxWoW.Me.HasAura(Common.FreezingFog); }
        }

        public static bool sudden_doom_react
        {
            get { return StyxWoW.Me.HasAura(Common.SuddenDoom); }
        }

        #endregion
    }

    public static class cooldown
    {
        #region Properties

        public static double antimagic_shell_remains
        {
            get { return Spell.GetSpellCooldown(DeathKnightBase.antimagic_shell).TotalSeconds; }
        }

        public static double breath_of_sindragosa_remains
        {
            get { return Spell.GetSpellCooldown(DeathKnightBase.breath_of_sindragosa).TotalSeconds; }
        }

        public static double defile_remains
        {
            get { return Spell.GetSpellCooldown(DeathKnightBase.defile).TotalSeconds; }
        }

        public static double empower_rune_weapon_remains
        {
            get { return Spell.GetSpellCooldown(DeathKnightBase.empower_rune_weapon).TotalSeconds; }
        }

        public static double outbreak_remains
        {
            get { return Spell.GetSpellCooldown(DeathKnightBase.outbreak).TotalSeconds; }
        }

        public static double pillar_of_frost_remains
        {
            get { return Spell.GetSpellCooldown(DeathKnightBase.pillar_of_frost).TotalSeconds; }
        }

        public static double soul_reaper_remains
        {
            get { return Spell.GetSpellCooldown(DeathKnightBase.soul_reaper).TotalSeconds; }
        }

        public static double unholy_blight_remains
        {
            get { return Spell.GetSpellCooldown(DeathKnightBase.unholy_blight).TotalSeconds; }
        }

        #endregion
    }

    public static class obliterate
    {
        #region Properties

        public static double ready_in
        {
            get { return 0; }
        }

        #endregion
    }

    public static class talent
    {
        #region Properties

        public static bool blood_tap_enabled
        {
            get { return Common.HasTalent(DeathKnightTalents.BloodTap); }
        }

        public static bool breath_of_sindragosa_enabled
        {
            get { return Common.HasTalent(DeathKnightTalents.BreathOfSindragosa); }
        }

        public static bool defile_enabled
        {
            get { return Common.HasTalent(DeathKnightTalents.Defile); }
        }

        public static bool necrotic_plague_enabled
        {
            get { return Common.HasTalent(DeathKnightTalents.NecroticPlague); }
        }

        public static bool runic_empowerment_enabled
        {
            get { return Common.HasTalent(DeathKnightTalents.RunicEmpowerment); }
        }

        public static bool unholy_blight_enabled
        {
            get { return Common.HasTalent(DeathKnightTalents.UnholyBlight); }
        }

        #endregion
    }

    public static class disease
    {
        #region Fields

        private static readonly string[] listBase = {"Blood Plague", "Frost Fever"};
        private static readonly string[] listWithNecroticPlague = {"Necrotic Plague"};

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
            get { return talent.necrotic_plague_enabled ? listWithNecroticPlague : listBase; }
        }

        #endregion

        #region Public Methods

        public static double max_remains_on(WoWUnit unit)
        {
            double max = double.MinValue;
            foreach (var s in diseaseArray)
            {
                double rmn = unit.GetAuraTimeLeft(s).TotalSeconds;
                if (rmn > max)
                    max = rmn;
            }

            if (max == double.MinValue)
                max = 0;

            return max;
        }

        public static double min_remains_on(WoWUnit unit)
        {
            double min = double.MaxValue;
            foreach (var s in diseaseArray)
            {
                double rmn = unit.GetAuraTimeLeft(s).TotalSeconds;
                if (rmn < min)
                    min = rmn;
            }

            if (min == double.MaxValue)
                min = 0;

            return min;
        }

        public static bool ticking_on(WoWUnit unit)
        {
            return unit.HasAllMyAuras(diseaseArray);
        }

        #endregion

        #region Private Methods

        private static bool max_ticking_on(WoWUnit unit)
        {
            return unit.HasAnyOfMyAuras(diseaseArray);
        }

        #endregion
    }

    public static class dot
    {
        #region Properties

        public static double blood_plague_remains
        {
            get { return StyxWoW.Me.CurrentTarget.GetAuraTimeLeft("Blood Plague").TotalSeconds; }
        }

        public static bool blood_plague_ticking
        {
            get { return blood_plague_remains > 0; }
        }

        public static double breath_of_sindragosa_remains
        {
            get { return StyxWoW.Me.CurrentTarget.GetAuraTimeLeft(DeathKnightBase.breath_of_sindragosa).TotalSeconds; }
        }

        public static bool breath_of_sindragosa_ticking
        {
            get { return breath_of_sindragosa_remains > 0; }
        }

        public static double frost_fever_remains
        {
            get { return StyxWoW.Me.CurrentTarget.GetAuraTimeLeft("Frost Fever").TotalSeconds; }
        }

        public static bool frost_fever_ticking
        {
            get { return frost_fever_remains > 0; }
        }

        public static double necrotic_plague_remains
        {
            get { return StyxWoW.Me.CurrentTarget.GetAuraTimeLeft("Necrotic Plague").TotalSeconds; }
        }

        public static bool necrotic_plague_ticking
        {
            get { return necrotic_plague_remains > 0; }
        }

        #endregion

        #region Public Methods

        public static double necrotic_plague_remains_on(WoWUnit unit)
        {
            return unit.GetAuraTimeLeft("Necrotic Plague").TotalSeconds;
        }

        public static bool necrotic_plague_ticking_on(WoWUnit unit)
        {
            return necrotic_plague_remains_on(unit) > 0;
        }

        #endregion
    }
}