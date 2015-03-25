using System;
using Styx;


namespace Singular.Dynamics
{
    [AttributeUsage(AttributeTargets.Method, Inherited = false, AllowMultiple = true)]
    internal sealed class IgnoreBehaviorCountAttribute : Attribute
    {
        #region Constructors

        public IgnoreBehaviorCountAttribute(BehaviorType type)
        {
            Type = type;
        }

        #endregion

        #region Properties

        public BehaviorType Type { get; private set; }

        #endregion
    }

    [AttributeUsage(AttributeTargets.Method, Inherited = false, AllowMultiple = true)]
    internal sealed class BehaviorAttribute : Attribute
    {
        #region Constructors

        public BehaviorAttribute(BehaviorType type, WoWClass @class = WoWClass.None, WoWSpec spec = (WoWSpec) int.MaxValue, WoWContext context = WoWContext.All, int priority = 0)
        {
            Type = type;
            SpecificClass = @class;
            SpecificSpec = spec;
            SpecificContext = context;
            PriorityLevel = priority;
        }

        #endregion

        #region Properties

        public int PriorityLevel { get; private set; }
        public WoWClass SpecificClass { get; private set; }
        public WoWContext SpecificContext { get; private set; }
        public WoWSpec SpecificSpec { get; private set; }
        public BehaviorType Type { get; private set; }

        #endregion
    }
}