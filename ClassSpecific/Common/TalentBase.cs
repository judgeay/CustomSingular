using Singular.Managers;

namespace Singular.ClassSpecific.Common
{
    internal class TalentBase
    {
        #region Fields

        private readonly int _talent;

        #endregion

        #region Constructors

        public TalentBase(int talent)
        {
            _talent = talent;
        }

        #endregion

        // ReSharper disable InconsistentNaming

        #region Properties

        public bool enabled
        {
            get { return HasTalent(); }
        }

        #endregion

        // ReSharper restore InconsistentNaming

        #region Private Methods

        private bool HasTalent()
        {
            return TalentManager.IsSelected(_talent);
        }

        #endregion
    }
}