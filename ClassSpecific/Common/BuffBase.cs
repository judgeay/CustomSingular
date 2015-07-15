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
            get { return Down(); }
        }

        public bool react
        {
            get { return React(); }
        }

        public double remains
        {
            get { return Remains(); }
        }

        public uint stack
        {
            get { return Stack(); }
        }

        public bool up
        {
            get { return Up(); }
        }

        #endregion

        // ReSharper restore InconsistentNaming

        #region Private Methods

        private bool Down()
        {
            return StyxWoW.Me.HasAura(SpellName) == false;
        }

        private bool PetDown()
        {
            return !PetUp();
        }

        private bool PetUp()
        {
            return StyxWoW.Me.GotAlivePet && StyxWoW.Me.Pet.ActiveAuras.ContainsKey(SpellName);
        }

        private bool React()
        {
            return StyxWoW.Me.HasAura(SpellId);
        }

        private double Remains()
        {
            return StyxWoW.Me.GetAuraTimeLeft(SpellName).TotalSeconds;
        }

        private uint Stack()
        {
            return StyxWoW.Me.GetAuraStacks(SpellName);
        }

        private bool Up()
        {
            return StyxWoW.Me.HasAura(SpellName) == true;
        }

        #endregion
    }
}