using System;
using System.Collections.Generic;
using Singular.Settings;
using Styx;
using Styx.TreeSharp;
using Styx.WoWInternals;
using Styx.WoWInternals.WoWObjects;
using Action = Styx.TreeSharp.Action;

namespace Singular.Helpers
{
    /// <summary>
    /// following class should probably be in Unit, but made a separate
    /// .. extension class to separate the private property names.
    /// --
    /// credit to Handnavi.  the following is a wrapping of his code
    /// </summary>
    public static class TimeToDeathExtension
    {
        #region Fields

        private const double NANOSECONDS_PER_SECOND = 10000.0 * 1000.0;
        private const int SECONDS_PER_HOUR = 3600;

        private static readonly Dictionary<WoWGuid, UnitLifeTime> _guids = new Dictionary<WoWGuid, UnitLifeTime>();

        private static long _lastReportedTime = long.MinValue;

        #endregion

        #region Public Methods

        /// <summary>
        /// creates behavior to write timetodeath values to debug log.  only
        /// evaluated if Singular Debug setting is enabled
        /// </summary>
        /// <returns></returns>
        public static Composite CreateWriteDebugTimeToDeath()
        {
            return new Action(ret =>
            {
                if (SingularSettings.Debug && StyxWoW.Me.GotTarget())
                {
                    var timeNow = StyxWoW.Me.CurrentTarget.TimeToDeath();
                    if (timeNow != _lastReportedTime || _guids.ContainsKey(StyxWoW.Me.CurrentTargetGuid) == false)
                    {
                        _lastReportedTime = timeNow;
                        Logger.WriteFile("TimeToDeath: {0} (GUID: {1}, Entry: {2}) dies in {3}", StyxWoW.Me.CurrentTarget.SafeName(), StyxWoW.Me.CurrentTarget.Guid, StyxWoW.Me.CurrentTarget.Entry, _lastReportedTime);
                    }
                }

                return RunStatus.Failure;
            });
        }

        /// <summary>
        /// seconds until the target dies.  first call initializes values. subsequent
        /// return estimate or indeterminateValue if death can't be calculated
        /// </summary>
        /// <param name="target">unit to monitor</param>
        /// <param name="indeterminateValue">return value if death cannot be calculated ( -1 or int.MaxValue are common)</param>
        /// <returns>number of seconds </returns>
        public static int TimeToDeath(this WoWUnit target, int indeterminateValue = int.MaxValue)
        {
            if (target == null || !target.IsValid || !target.IsAlive) return indeterminateValue;

            // Fill variables on new target or on target switch, this will loose all calculations from last target ! Eheh no no no DarkNinjaCsharper solve this !
            if (_guids.ContainsKey(target.Guid) == false || _guids.ContainsKey(target.Guid) && (target.MaxHealth != _guids[target.Guid].MaxHealth || target.CurrentHealth > _guids[target.Guid].CurrentHealth))
            {
                var unitLifeTime = new UnitLifeTime
                {
                    FirstHealth = target.CurrentHealth,
                    FirstTime = DateTime.Now.TotalSeconds(),
                    MaxHealth = target.MaxHealth
                };

                if (_guids.ContainsKey(target.Guid)) _guids[target.Guid] = unitLifeTime;
                else _guids.Add(target.Guid, unitLifeTime);
            }

            var current = _guids[target.Guid];

            current.CurrentHealth = target.CurrentHealth;
            current.CurrentTime = DateTime.Now.TotalSeconds();

            var timeDiff = current.CurrentTime - current.FirstTime;
            var hpDiff = current.FirstHealthPercent - current.CurrentHealthPercent;

            if (hpDiff > 0)
            {
                /*
                * Rule of three (Dreisatz):
                * If in a given timespan a certain value of damage is done, what timespan is needed to do 100% damage?
                * The longer the timespan the more precise the prediction
                * time_diff/hp_diff = x/first_life_max
                * x = time_diff*first_life_max/hp_diff
                * 
                * For those that forgot, http://mathforum.org/library/drmath/view/60822.html
                */

                var timeToDie = current.CurrentHealthPercent * (timeDiff / hpDiff);
                if (timeToDie < 1) return 1;
                if (timeToDie > SECONDS_PER_HOUR) return SECONDS_PER_HOUR;

                return Convert.ToInt32(timeToDie);
            }

            return indeterminateValue;
        }

        #endregion

        #region Private Methods

        private static double TotalSeconds(this DateTime time)
        {
            return time.Ticks / NANOSECONDS_PER_SECOND;
        }

        #endregion

        #region Types

        private class UnitLifeTime
        {
            #region Fields

            public uint CurrentHealth; // life of mob now
            public double CurrentTime; // time now
            public uint FirstHealth; // life of mob when first seen
            public double FirstTime; // time mob was first seen
            public uint MaxHealth;

            #endregion

            #region Properties

            public double CurrentHealthPercent
            {
                get
                {
                    if (MaxHealth == 0 || MaxHealth < CurrentHealth) return 0;
                    return (Convert.ToDouble(CurrentHealth) / MaxHealth) * 100.0;
                }
            }

            public double FirstHealthPercent
            {
                get
                {
                    if (MaxHealth == 0 || MaxHealth < FirstHealth) return 0;
                    return (Convert.ToDouble(FirstHealth) / MaxHealth) * 100.0;
                }
            }

            #endregion
        }

        #endregion
    }
}