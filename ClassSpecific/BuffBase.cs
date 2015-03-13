using Singular.Helpers;
using Styx;

namespace Singular.ClassSpecific
{
    internal class BuffBase
    {
        private readonly string _buff;

        public BuffBase(string buff)
        {
            _buff = buff;
        }

        public bool down 
        {
            get { return !up; }
        }

        public bool up
        {
            get { return remains > 0; }
        }

        public virtual double remains
        {
            get { return StyxWoW.Me.GetAuraTimeLeft(_buff).TotalSeconds; }
        }

        public virtual uint stack
        {
            get { return StyxWoW.Me.GetAuraStacks(_buff); }
        }

        protected string Buff
        {
            get { return _buff; }
        }
    }
}