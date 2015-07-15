namespace Singular.ClassSpecific.Common
{
    internal abstract class CooldownBase : Base
    {
        #region Constructors

        protected CooldownBase(int spellId)
            : base(spellId)
        {
        }

        protected CooldownBase(string spell)
            : base(spell)
        {
        }

        #endregion

        // ReSharper disable InconsistentNaming

        #region Properties

        public double remains
        {
            get { return Remains(); }
        }

        #endregion

        // ReSharper restore InconsistentNaming

        #region Private Methods

        private double Remains()
        {
            return Spell.CooldownTimeLeft.TotalSeconds;
        }

        #endregion
    }
}