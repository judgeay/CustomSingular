using System;
using Singular.Settings;
using Styx;
using Styx.TreeSharp;
using Styx.WoWInternals;
using Styx.WoWInternals.WoWObjects;
using Action = Styx.TreeSharp.Action;

namespace Singular.Helpers
{
    public static class TimeToDeathExtension
    {
        #region Fields

        private static readonly DateTime _timeOrigin = new DateTime(2015, 1, 1); // Refernzdatum (festgelegt)

        private static uint _currentLife; // life of mob now
        private static int _currentTime; // time now
        private static uint _firstLife; // life of mob when first seen
        private static uint _firstLifeMax; // max life of mob when first seen
        private static int _firstTime; // time mob was first seen
        private static long _lastReportedTime = -111;

        #endregion

        #region Properties

        /// <summary>
        /// GUID of mob
        /// </summary>
        public static WoWGuid Guid { get; set; }

        #endregion

        #region Public Methods

        /// <summary>
        /// creates behavior to write timetodeath values to debug log.  only
        /// evaluated if Singular Debug setting is enabled
        /// </summary>
        /// <returns></returns>
        public static Composite CreateWriteDebugTimeToDeath()
        {
            return new Action(
                ret =>
                {
                    if (SingularSettings.Debug && StyxWoW.Me.GotTarget())
                    {
                        var timeNow = StyxWoW.Me.CurrentTarget.TimeToDeath();
                        if (timeNow != _lastReportedTime || Guid != StyxWoW.Me.CurrentTargetGuid)
                        {
                            _lastReportedTime = timeNow;
                            Logger.WriteFile("TimeToDeath: {0} (GUID: {1}, Entry: {2}) dies in {3}",
                                StyxWoW.Me.CurrentTarget.SafeName(),
                                StyxWoW.Me.CurrentTarget.Guid,
                                StyxWoW.Me.CurrentTarget.Entry,
                                _lastReportedTime);
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
        public static long TimeToDeath(this WoWUnit target, long indeterminateValue = -1)
        {
            if (target == null || !target.IsValid || !target.IsAlive)
            {
                //Logging.Write("TimeToDeath: {0} (GUID: {1}, Entry: {2}) is dead!", target.SafeName(), target.Guid, target.Entry);
                return 0;
            }

            if (StyxWoW.Me.CurrentTarget.IsTrainingDummy())
            {
                return 111; // pick a magic number since training dummies dont die
            }

            //Fill variables on new target or on target switch, this will loose all calculations from last target
            if (Guid != target.Guid || (Guid == target.Guid && target.CurrentHealth == _firstLifeMax))
            {
                Guid = target.Guid;
                _firstLife = target.CurrentHealth;
                _firstLifeMax = target.MaxHealth;
                _firstTime = ConvDate2Timestam(DateTime.Now);
                //Lets do a little trick and calculate with seconds / u know Timestamp from unix? we'll do so too
            }
            _currentLife = target.CurrentHealth;
            _currentTime = ConvDate2Timestam(DateTime.Now);
            var timeDiff = _currentTime - _firstTime;
            var hpDiff = _firstLife - _currentLife;
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
                var fullTime = timeDiff * _firstLifeMax / hpDiff;
                var pastFirstTime = (_firstLifeMax - _firstLife) * timeDiff / hpDiff;
                var calcTime = _firstTime - pastFirstTime + fullTime - _currentTime;
                if (calcTime < 1) calcTime = 1;
                //calc_time is a int value for time to die (seconds) so there's no need to do SecondsToTime(calc_time)
                var timeToDie = calcTime;
                //Logging.Write("TimeToDeath: {0} (GUID: {1}, Entry: {2}) dies in {3}, you are dpsing with {4} dps", target.SafeName(), target.Guid, target.Entry, timeToDie, dps);
                return timeToDie;
            }
            if (hpDiff <= 0)
            {
                //unit was healed,resetting the initial values
                Guid = target.Guid;
                _firstLife = target.CurrentHealth;
                _firstLifeMax = target.MaxHealth;
                _firstTime = ConvDate2Timestam(DateTime.Now);
                //Lets do a little trick and calculate with seconds / u know Timestamp from unix? we'll do so too
                //Logging.Write("TimeToDeath: {0} (GUID: {1}, Entry: {2}) was healed, resetting data.", target.SafeName(), target.Guid, target.Entry);
                return indeterminateValue;
            }
            if (_currentLife == _firstLifeMax)
            {
                //Logging.Write("TimeToDeath: {0} (GUID: {1}, Entry: {2}) is at full health.", target.SafeName(), target.Guid, target.Entry);
                return indeterminateValue;
            }
            //Logging.Write("TimeToDeath: {0} (GUID: {1}, Entry: {2}) no damage done, nothing to calculate.", target.SafeName(), target.Guid, target.Entry);
            return indeterminateValue;
        }

        #endregion

        #region Private Methods

        private static int ConvDate2Timestam(DateTime time)
        {
            return (int)(time - _timeOrigin).TotalSeconds;
        }

        #endregion
    }
}