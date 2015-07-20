using Singular.Helpers;
using Styx;
using Styx.WoWInternals.WoWObjects;

namespace Singular.ClassSpecific.Common
{
    internal class DotBase : Base
    {
        #region Constructors

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
            return Remains(StyxWoW.Me.CurrentTarget);
        }

        private bool Ticking()
        {
            return Remains() > 0;
        }

        public bool Ticking(WoWUnit target)
        {
            return Remains(target) > 0;
        }

        public double Remains(WoWUnit target)
        {
            return target != null ? target.GetAuraTimeLeft(SpellName).TotalSeconds : 0;
        }

        #endregion
    }
}