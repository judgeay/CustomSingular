using Styx.CommonBot;
using Styx.WoWInternals;

namespace Singular.ClassSpecific.Common
{
    internal abstract class Base
    {
        #region Fields

        protected readonly WoWSpell Spell;

        #endregion

        #region Constructors

        protected Base(int spellId)
        {
            SpellFindResults sfr;
            SpellManager.FindSpell(spellId, out sfr);

            Spell = sfr.Override ?? sfr.Original;
        }

        protected Base(string spell)
        {
            SpellFindResults sfr;
            SpellManager.FindSpell(spell, out sfr);

            Spell = sfr.Override ?? sfr.Original;
        }

        #endregion
    }
}