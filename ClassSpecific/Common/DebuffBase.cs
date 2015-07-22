using Singular.Helpers;
using Styx;

namespace Singular.ClassSpecific.Common
{
    internal abstract class DebuffBase : Base
    {
        #region Constructors

        protected DebuffBase(string spell)
            : base(spell)
        {
        }

        #endregion

        // ReSharper disable InconsistentNaming

        #region Properties

        public decimal stack
        {
            get { return StyxWoW.Me.CurrentTarget.GetAuraStacks(SpellName); }
        }

        public double remains
        {
            get { return StyxWoW.Me.CurrentTarget.GetAuraTimeLeft(SpellName).TotalSeconds; }
        }

        #endregion

        // ReSharper restore InconsistentNaming
    }
}