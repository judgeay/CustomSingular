using Singular.Helpers;
using Styx;

namespace Singular.ClassSpecific.Common
{
    internal abstract class BuffBase : Base
    {
        #region Constructors

        protected BuffBase(int spellId)
            : base(spellId)
        {
        }

        protected BuffBase(string spell)
            : base(spell)
        {
        }

        #endregion

        // ReSharper disable InconsistentNaming

        #region Properties

        public bool down
        {
            get { return StyxWoW.Me.HasAura(SpellName) == false; }
        }

        public bool react
        {
            get { return SpellName != null ? StyxWoW.Me.HasAura(SpellName) : StyxWoW.Me.HasAura(SpellId); }
        }

        public double remains
        {
            get { return SpellName != null ? StyxWoW.Me.GetAuraTimeLeft(SpellName).TotalSeconds : StyxWoW.Me.GetAuraTimeLeft(SpellId).TotalSeconds; }
        }

        public uint stack
        {
            get { return StyxWoW.Me.GetAuraStacks(SpellName); }
        }

        public bool up
        {
            get { return StyxWoW.Me.HasAura(SpellName); }
        }

        #endregion

        // ReSharper restore InconsistentNaming
    }
}