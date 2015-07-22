using Singular.Helpers;

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