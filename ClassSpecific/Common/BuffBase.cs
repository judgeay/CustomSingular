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

        public uint stack
        {
            get { return Stack(); }
        }

        #endregion

        // ReSharper restore InconsistentNaming

        #region Private Methods

        private bool Down()
        {
            return StyxWoW.Me.HasAura(Spell.Id) == false;
        }

        private bool PetDown()
        {
            return !PetUp();
        }

        private bool PetUp()
        {
            return StyxWoW.Me.GotAlivePet && StyxWoW.Me.Pet.ActiveAuras.ContainsKey(Spell.Name);
        }

        private bool React()
        {
            return StyxWoW.Me.HasAura(Spell.Id);
        }

        private uint Stack()
        {
            return StyxWoW.Me.GetAuraStacks(Spell.Id);
        }

        #endregion
    }
}