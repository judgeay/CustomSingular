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
            get { return Remains(StyxWoW.Me.CurrentTarget); }
        }

        public bool ticking
        {
            get { return remains > 0; }
        }

        #endregion

        // ReSharper restore InconsistentNaming

        #region Public Methods

        public double Remains(WoWUnit target)
        {
            return target != null ? target.GetAuraTimeLeft(SpellName).TotalSeconds : 0;
        }

        public bool Ticking(WoWUnit target)
        {
            return Remains(target) > 0;
        }

        #endregion
    }
}