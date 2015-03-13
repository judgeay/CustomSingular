using Styx;

namespace Singular.ClassSpecific
{
    internal class PetBuffBase : BuffBase
    {
        public PetBuffBase(string buff) : base(buff)
        {
        }

        public override double remains
        {
            get
            {
                var aura = StyxWoW.Me.Pet.ActiveAuras.ContainsKey(Buff) ? StyxWoW.Me.Pet.ActiveAuras[Buff] : null;
                return aura != null ? aura.TimeLeft.TotalSeconds : 0;
            }
        }

        public override uint stack
        {
            get
            {
                var aura = StyxWoW.Me.Pet.ActiveAuras.ContainsKey(Buff) ? StyxWoW.Me.Pet.ActiveAuras[Buff] : null;
                return aura != null ? aura.StackCount : 0;
            }
        }
    }
}