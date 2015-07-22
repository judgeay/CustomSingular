using Singular.Helpers;
using Styx.CommonBot;

namespace Singular.ClassSpecific.Common
{
    internal class ActionBase : Base
    {
        #region Constructors

        public ActionBase(string spellName)
            : base(spellName)
        {
        }

        #endregion

        // ReSharper disable InconsistentNaming

        #region Properties

        public int charges
        {
            get { return Spell.GetCharges(SpellName); }
        }

        public double execute_time
        {
            get { return Spell.GetSpellCastTime(SpellName).TotalSeconds; }
        }

        public double recharge_time
        {
            get
            {
                SpellFindResults sfr;
                if (SpellManager.FindSpell(SpellName, out sfr) == false || sfr == null || (sfr.Original == null && sfr.Override == null)) return 0;

                var spell = sfr.Override ?? sfr.Original;

                if (spell.Cooldown) return spell.CooldownTimeLeft.TotalSeconds;
                return spell.BaseCooldown / 1000.0;
            }
        }

        #endregion

        // ReSharper restore InconsistentNaming
    }
}