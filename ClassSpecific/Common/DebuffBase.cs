using Singular.Helpers;
using Styx;
using Styx.WoWInternals.WoWObjects;

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
            get { return Down(StyxWoW.Me.CurrentTarget); }
        }

        public double remains
        {
            get { return Remains(StyxWoW.Me.CurrentTarget); }
        }

        public uint stack
        {
            get { return Stack(StyxWoW.Me.CurrentTarget); }
        }

        public bool up
        {
            get { return Up(StyxWoW.Me.CurrentTarget); }
        }

        #endregion

        // ReSharper restore InconsistentNaming

        #region Public Methods

        public bool Down(WoWUnit target)
        {
            // ReSharper disable once CompareOfFloatsByEqualityOperator
            return Remains(target) == 0;
        }

        public double Remains(WoWUnit target)
        {
            return target != null ? target.GetAuraTimeLeft(SpellName).TotalSeconds : 0;
        }

        public uint Stack(WoWUnit target)
        {
            return target != null ? target.GetAuraStacks(SpellName) : 0;
        }

        public bool Up(WoWUnit target)
        {
            return Remains(target) > 0;
        }

        #endregion
    }
}