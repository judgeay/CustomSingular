using Singular.Helpers;
using Styx;

namespace Singular.ClassSpecific.Common
{
    internal class DotBase : Base
    {
        #region Constructors

        public DotBase(int spellId)
            : base(spellId)
        {
        }

        public DotBase(string spell)
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

        public bool ticking
        {
            get { return Ticking(); }
        }

        #endregion

        // ReSharper restore InconsistentNaming

        #region Private Methods

        private double Remains()
        {
            return StyxWoW.Me.CurrentTarget.GetAuraTimeLeft(Spell.Id).TotalSeconds;
        }

        private bool Ticking()
        {
            return Remains() > 0;
        }

        #endregion
    }
}