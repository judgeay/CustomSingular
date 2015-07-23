using Singular.Helpers;
using Styx.CommonBot;

namespace Singular.ClassSpecific.Common
{
    internal abstract class CooldownBase : Base
    {
        #region Constructors

        protected CooldownBase(string spell)
            : base(spell)
        {
        }

        #endregion

        // ReSharper disable InconsistentNaming

        #region Properties

        public double duration
        {
            get
            {
                SpellFindResults sfr;
                if (SpellManager.FindSpell(SpellName, out sfr) == false || sfr == null || (sfr.Original == null && sfr.Override == null)) return int.MaxValue;

                var spell = sfr.Override ?? sfr.Original;

                if (spell.Cooldown) return spell.CooldownTimeLeft.TotalSeconds;
                return spell.BaseCooldown / 1000.0;
            }
        }

        public double remains
        {
            get { return Spell.GetSpellCooldown(SpellName).TotalSeconds; }
        }

        public bool up
        {
            get { return remains == 0; }
        }

        #endregion

        // ReSharper restore InconsistentNaming
    }
}