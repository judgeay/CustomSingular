using Singular.Helpers;
using Styx.TreeSharp;

namespace Singular.Dynamics
{
    public static class NoContextAvailable
    {
        public static Composite CreateDoNothingBehavior()
        {
            return new Throttle(15,
                new Action(r => Logger.Write("No Context Available - do nothing while we wait"))
                );
        }
    }
}