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

        public bool down
        {
            get { return remains == 0; }
        }

        public double remains
        {
            get { return StyxWoW.Me.CurrentTarget.GetAuraTimeLeft(SpellName).TotalSeconds; }
        }

        public uint stack
        {
            get { return StyxWoW.Me.CurrentTarget.GetAuraStacks(SpellName); }
        }

        public bool up
        {
            get { return remains > 0; }
        }

        #endregion

        // ReSharper restore InconsistentNaming
    }
}