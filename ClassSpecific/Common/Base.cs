namespace Singular.ClassSpecific.Common
{
    internal abstract class Base
    {
        #region Fields

        protected readonly int SpellId = -1;
        protected readonly string SpellName = null;

        #endregion

        #region Constructors

        protected Base(int spellId)
        {
            SpellId = spellId;
        }

        protected Base(string spellName)
        {
            SpellName = spellName;
        }

        #endregion
    }
}