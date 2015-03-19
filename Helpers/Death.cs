using System.Linq;
using CommonBehaviors.Actions;
using Singular.Settings;
using Styx;
using Styx.WoWInternals;
using Styx.WoWInternals.WoWObjects;
using Styx.TreeSharp;
using System;
using Action = Styx.TreeSharp.Action;
using Singular.Dynamics;
using Singular.Managers;
using System.Collections.Generic;
using System.Drawing;

namespace Singular.Helpers
{
    internal static class Death
    {
        #region Fields

        private const int REZ_MAX_MOBS_NEAR = 0;
        private const int REZ_WAIT_DIST = 20;
        private const int REZ_WAIT_TIME = 10;

        private static DateTime _nextSuppressMessage = DateTime.MinValue;

        #endregion

        #region Properties

        private static LocalPlayer Me
        {
            get { return StyxWoW.Me; }
        }

        private static int MobsNearby { get; set; }
        private static string SelfRezSpell { get; set; }

        #endregion

        #region Public Methods

        [Behavior(BehaviorType.Death)]
        public static Composite CreateDefaultDeathBehavior()
        {
            return new Throttle(60,
                new Decorator(
                    req =>
                    {
                        if (Me.IsAlive || Me.IsGhost)
                        {
                            Logger.WriteDiagnostic(LogColor.Hilite, "Death Behavior: ERROR - should not be called with Alive={0}, Ghost={1}", Me.IsAlive.ToYN(), Me.IsGhost.ToYN());
                            return false;
                        }

                        Logger.WriteDiagnostic(LogColor.Hilite, "Death Behavior: invoked!  Alive={0}, Ghost={1}", Me.IsAlive.ToYN(), Me.IsGhost.ToYN());
                        if (SingularSettings.Instance.SelfRessurect == SelfRessurectStyle.None)
                        {
                            Logger.WriteDiagnostic(LogColor.Hilite, "Death Behavior: ERROR - should not be called with Alive={0}, Ghost={1}", Me.IsAlive.ToYN(), Me.IsGhost.ToYN());
                            return false;
                        }

                        List<string> hasSoulstone = Lua.GetReturnValues("return HasSoulstone()", "hawker.lua");
                        if (hasSoulstone == null || hasSoulstone.Count == 0 || String.IsNullOrEmpty(hasSoulstone[0]) || hasSoulstone[0].ToLower() == "nil")
                        {
                            Logger.WriteDiagnostic(LogColor.Hilite, "Death Behavior: character unable to self-ressurrect currently");
                            return false;
                        }

                        if (SingularSettings.Instance.SelfRessurect == SelfRessurectStyle.Auto && MovementManager.IsMovementDisabled)
                        {
                            if (_nextSuppressMessage < DateTime.Now)
                            {
                                _nextSuppressMessage = DateTime.Now.AddSeconds(REZ_WAIT_TIME);
                                Logger.Write(Color.Aquamarine, "Suppressing automatic {0} since movement disabled...", hasSoulstone[0]);
                            }
                            return false;
                        }

                        SelfRezSpell = hasSoulstone[0];
                        Logger.WriteDiagnostic(LogColor.Hilite, "Death Behavior: beginning {0}", SelfRezSpell);
                        return true;
                    },
                    new Sequence(
                        new Action(r => Logger.Write(Color.Aquamarine, "Waiting up to {0} seconds for clear area to use {1}...", REZ_WAIT_TIME, SelfRezSpell)),
                        new Wait(
                            REZ_WAIT_TIME,
                            until =>
                            {
                                MobsNearby = Unit.UnfriendlyUnits(REZ_WAIT_DIST).Count();
                                return MobsNearby <= REZ_MAX_MOBS_NEAR || Me.IsAlive || Me.IsGhost;
                            },
                            new Action(r =>
                            {
                                if (Me.IsGhost)
                                {
                                    Logger.Write(Color.Aquamarine, "Insignia taken or corpse release by something other than Singular...");
                                    return RunStatus.Failure;
                                }

                                if (Me.IsAlive)
                                {
                                    Logger.Write(Color.Aquamarine, "Ressurected by something other than Singular...");
                                    return RunStatus.Failure;
                                }

                                return RunStatus.Success;
                            })
                            ),
                        new DecoratorContinue(
                            req => MobsNearby > REZ_MAX_MOBS_NEAR,
                            new Action(r =>
                            {
                                Logger.Write(Color.Aquamarine, "Still {0} enemies within {1} yds, skipping {2}", MobsNearby, REZ_WAIT_DIST, SelfRezSpell);
                                return RunStatus.Failure;
                            })
                            ),
                        new Action(r => Logger.Write("Ressurrecting Singular by invoking {0}...", SelfRezSpell)),
                        new Action(r => Lua.DoString("UseSoulstone()")),
                        new WaitContinue(1, until => Me.IsAlive || Me.IsGhost, new ActionAlwaysSucceed())
                        )
                    )
                );
        }

        #endregion
    }
}