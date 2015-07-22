using Singular.Managers;

namespace Singular.ClassSpecific.Common
{
    internal class GlyphBase : Base
    {
        #region Constructors

        public GlyphBase(string spellName)
            : base(spellName)
        {
        }

        #endregion

        // ReSharper disable InconsistentNaming

        #region Properties

        public bool enabled
        {
            get { return TalentManager.HasGlyph(SpellName); }
        }

        #endregion

        // ReSharper restore InconsistentNaming
    }
}