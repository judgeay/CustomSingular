using Singular.Helpers;
using Styx;

namespace Singular.ClassSpecific.Common
{
    internal abstract class DebuffBase : Base
    {
        #region Constructors

        protected DebuffBase(int spellId)
            : base(spellId)
        {
        }

        protected DebuffBase(string spell)
            : base(spell)
        {
        }

        #endregion

        // ReSharper disable InconsistentNaming

        #region Properties

        public decimal stack
        {
            get { return Stack(); }
        }

        #endregion

        // ReSharper restore InconsistentNaming

        #region Private Methods

        private decimal Stack()
        {
            return StyxWoW.Me.CurrentTarget.GetAuraStacks(Spell.Id);
        }

        #endregion
    }
}