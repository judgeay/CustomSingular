// #define REACT_TO_HOTKEYS_IN_PULSE

#define HONORBUDDY_SUPPORTS_HOTKEYS_WITHOUT_REQUIRING_A_MODIFIER

using Styx;
using Styx.WoWInternals;
using System;
using Singular.Settings;
using System.Drawing;
using System.Collections.Generic;
using Singular.Helpers;
using Styx.Common;
using System.Windows.Forms;
using System.Runtime.InteropServices;

namespace Singular.Managers
{
    internal static class HotkeyDirector
    {
        // state of each toggle kept here

        #region Fields

        public static Keys[] RegisteredMovementSuspendKeys;

        private static bool _aoeEnabled;
        private static bool _combatEnabled;
        private static bool _cooldownEnabled;
        private static bool _defensiveCooldownEnabled;
        private static bool _hotkeysRegistered;
        private static bool _interruptEnabled;
        private static bool _lastIsAoeEnabled;
        private static bool _lastIsCombatEnabled;
        private static bool _lastIsCooldownEnabled;
        private static bool _lastIsDefensiveCooldownEnabled;
        private static bool _lastIsInterruptEnabled;
        private static bool _lastIsMovementEnabled;
        private static bool _lastIsMovementTemporarilySuspended;
        private static bool _lastIsPullMoreEnabled;
        private static Keys _lastMovementTemporarySuspendKey;
        private static bool _movementEnabled;
        private static DateTime _movementTemporarySuspendEndtime = DateTime.MinValue;
        private static bool _pullMoreEnabled;

        #endregion

        #region Properties

        /// <summary>
        /// True: if AOE spells are allowed, False: Single target only
        /// </summary>
        public static bool IsAoeEnabled
        {
            get { return _aoeEnabled; }
        }

        /// <summary>
        /// True: allow normal combat, False: CombatBuff and Combat behaviors are suppressed
        /// </summary>
        public static bool IsCombatEnabled
        {
            get { return _combatEnabled; }
        }

        public static bool IsCooldownEnabled
        {
            get { return _cooldownEnabled; }
        }

        public static bool IsDefensiveCooldownEnabled
        {
            get { return _defensiveCooldownEnabled; }
        }

        public static bool IsInterruptEnabled
        {
            get { return _interruptEnabled; }
        }

        /// <summary>
        /// True: allow normal Bot movement, False: prevent any movement by Bot, Combat Routine, or Plugins
        /// </summary>
        public static bool IsMovementEnabled
        {
            get { return _movementEnabled && !IsMovementTemporarilySuspended; }
        }

        /// <summary>
        /// True: if PullMore spells are allowed, False: Single target only
        /// </summary>
        public static bool IsPullMoreEnabled
        {
            get { return _pullMoreEnabled; }
        }

        private static HotkeySettings HotkeySettings
        {
            get { return SingularSettings.Instance.Hotkeys(); }
        }

        private static bool IsMovementTemporarilySuspended
        {
            get
            {
                // check if not suspended
                if (_movementTemporarySuspendEndtime == DateTime.MinValue)
                    return false;

                // check if still suspended
                if (_movementTemporarySuspendEndtime > DateTime.Now)
                    return true;

                // suspend has timed out, so refresh suspend timer if key is still down
                // -- currently only check last key pressed rather than every suspend key configured
                // if ( HotkeySettings.SuspendMovementKeys.Any( k => IsKeyDown( k )))
                if (IsKeyDown(_lastMovementTemporarySuspendKey))
                {
                    _movementTemporarySuspendEndtime = DateTime.Now + TimeSpan.FromSeconds(HotkeySettings.SuspendDuration);
                    return true;
                }

                _movementTemporarySuspendEndtime = DateTime.MinValue;
                return false;
            }

            set
            {
                if (value)
                    _movementTemporarySuspendEndtime = DateTime.Now + TimeSpan.FromSeconds(HotkeySettings.SuspendDuration);
                else
                    _movementTemporarySuspendEndtime = DateTime.MinValue;
            }
        }

        #endregion

        #region Public Methods

        [DllImport("user32.dll", CharSet = CharSet.Auto, ExactSpelling = true)]
        public static extern IntPtr GetActiveWindow();

        [DllImport("user32.dll", CharSet = CharSet.Auto, ExactSpelling = true)]
        public static extern short GetAsyncKeyState(int vkey);

        #endregion

        #region Private Methods

        internal static void AoeKeyHandler()
        {
            if (_aoeEnabled != _lastIsAoeEnabled)
            {
                _lastIsAoeEnabled = _aoeEnabled;
                if (_lastIsAoeEnabled)
                    TellUser("AoE now active!");
                else
                    TellUser("AoE disabled... press {0} to enable", HotkeySettings.AoeToggle.ToFormattedString());
            }
        }

        internal static void CombatKeyHandler()
        {
            if (_combatEnabled != _lastIsCombatEnabled)
            {
                _lastIsCombatEnabled = _combatEnabled;
                if (_lastIsCombatEnabled)
                    TellUser("Combat now enabled!");
                else
                    TellUser("Combat disabled... press {0} to enable", HotkeySettings.CombatToggle.ToFormattedString());
            }
        }

        internal static void CooldownKeyHandler()
        {
            if (_cooldownEnabled != _lastIsCooldownEnabled)
            {
                _lastIsCooldownEnabled = _cooldownEnabled;
                if (_lastIsCooldownEnabled)
                    TellUser("Automatic Cooldown usage now active!");
                else
                    TellUser("Automatic Cooldown usage disabled... press {0} to enable", HotkeySettings.CooldownToggle.ToFormattedString());
            }
        }

        internal static void DefensiveCooldownKeyHandler()
        {
            if (_defensiveCooldownEnabled != _lastIsDefensiveCooldownEnabled)
            {
                _lastIsDefensiveCooldownEnabled = _defensiveCooldownEnabled;
                if (_lastIsDefensiveCooldownEnabled)
                    TellUser("Automatic Defensive Cooldown usage now active!");
                else
                    TellUser("Automatic Defensive Cooldown usage disabled... press {0} to enable", HotkeySettings.DefensiveCooldownToggle.ToFormattedString());
            }
        }

        /// <summary>
        /// sets initial values for all key states. registers a local botevent handler so 
        /// we know when we are running and when we arent to enable/disable hotkeys
        /// </summary>
        internal static void Init()
        {
            InitKeyStates();

            SingularRoutine.OnBotEvent += (src, arg) =>
            {
                if (arg.Event == SingularBotEvent.BotStarted)
                    Start(true);
                else if (arg.Event == SingularBotEvent.BotStopped)
                    Stop();
            };
        }

        internal static void InterruptKeyHandler()
        {
            if (_interruptEnabled != _lastIsInterruptEnabled)
            {
                _lastIsInterruptEnabled = _interruptEnabled;
                if (_lastIsInterruptEnabled)
                    TellUser("Interrupt usage now active!");
                else
                    TellUser("Interrupt usage disabled... press {0} to enable", HotkeySettings.InterruptToogle.ToFormattedString());
            }
        }

        internal static void MovementKeyHandler()
        {
            if (_movementEnabled != _lastIsMovementEnabled)
            {
                _lastIsMovementEnabled = _movementEnabled;
                if (_lastIsMovementEnabled)
                    TellUser("Movement now enabled!");
                else
                    TellUser("Movement disabled... press {0} to enable", HotkeySettings.MovementToggle.ToFormattedString());

                MovementManager.Update();
            }
        }

        internal static void PullMoreKeyHandler()
        {
            if (_pullMoreEnabled != _lastIsPullMoreEnabled)
            {
                _lastIsPullMoreEnabled = _pullMoreEnabled;
                if (_lastIsPullMoreEnabled)
                    TellUser("PullMore now allowed!");
                else
                    TellUser("PullMore disabled... press {0} to enable", HotkeySettings.PullMoreToggle.ToFormattedString());
            }
        }

        /// <summary>
        /// checks whether the state of any of the ability toggles we control via hotkey
        /// has changed.  if so, update the user with a message
        /// </summary>
        internal static void Pulse()
        {
            // since we are polling system keybd, make sure our game window is active
            if (GetActiveWindow() != StyxWoW.Memory.Process.MainWindowHandle)
                return;

            // handle release of key here if not using toggle behavior
            if (!HotkeySettings.KeysToggleBehavior)
            {
                if (HotkeySettings.AoeToggle != Keys.None)
                {
                    _aoeEnabled = !IsKeyDown(HotkeySettings.AoeToggle);
                    AoeKeyHandler();
                }
                if (HotkeySettings.CombatToggle != Keys.None)
                {
                    _combatEnabled = !IsKeyDown(HotkeySettings.CombatToggle);
                    CombatKeyHandler();
                }
                if (HotkeySettings.CooldownToggle != Keys.None)
                {
                    _cooldownEnabled = !IsKeyDown(HotkeySettings.CooldownToggle);
                    CooldownKeyHandler();
                }
                if (HotkeySettings.DefensiveCooldownToggle != Keys.None)
                {
                    _defensiveCooldownEnabled = !IsKeyDown(HotkeySettings.DefensiveCooldownToggle);
                    DefensiveCooldownKeyHandler();
                }
                if (HotkeySettings.InterruptToogle != Keys.None)
                {
                    _interruptEnabled = !IsKeyDown(HotkeySettings.InterruptToogle);
                    InterruptKeyHandler();
                }
                if (HotkeySettings.MovementToggle != Keys.None)
                {
                    _movementEnabled = !IsKeyDown(HotkeySettings.MovementToggle);
                    MovementKeyHandler();
                }
            }

            TemporaryMovementKeyHandler();
        }

        internal static void Start(bool needReset = false)
        {
            if (needReset)
                InitKeyStates();

            _hotkeysRegistered = true;

            // Hook the  hotkeys for the appropriate WOW Window...
            HotkeysManager.Initialize(StyxWoW.Memory.Process.MainWindowHandle);

            // define hotkeys for behaviors when using them as toggles (press once to disable, press again to enable)
            // .. otherwise, keys are polled for in Pulse()
            if (HotkeySettings.KeysToggleBehavior)
            {
                // register hotkey for commands with 1:1 key assignment
                if (HotkeySettings.AoeToggle != Keys.None)
                    RegisterHotkeyAssignment("AOE", HotkeySettings.AoeToggle, hk => AoeToggle());

                if (HotkeySettings.CombatToggle != Keys.None)
                    RegisterHotkeyAssignment("Combat", HotkeySettings.CombatToggle, hk => CombatToggle());

                if (HotkeySettings.CooldownToggle != Keys.None)
                    RegisterHotkeyAssignment("Cooldown", HotkeySettings.CooldownToggle, hk => CooldownToggle());

                if (HotkeySettings.DefensiveCooldownToggle != Keys.None)
                    RegisterHotkeyAssignment("DefensiveCooldown", HotkeySettings.DefensiveCooldownToggle, hk => DefensiveCooldownToggle());

                if (HotkeySettings.InterruptToogle != Keys.None)
                    RegisterHotkeyAssignment("Interrupt", HotkeySettings.InterruptToogle, hk => InterruptToogle());

                // register hotkey for commands with 1:1 key assignment
                if (HotkeySettings.PullMoreToggle != Keys.None)
                    RegisterHotkeyAssignment("PullMore", HotkeySettings.PullMoreToggle, hk => PullMoreToggle());

                // note: important to not check MovementManager if movement disabled here, since MovementManager calls us
                // .. and the potential for side-effects exists.  check SingularSettings directly for this only
                if (!SingularSettings.Instance.DisableAllMovement && HotkeySettings.MovementToggle != Keys.None)
                    RegisterHotkeyAssignment("Movement", HotkeySettings.MovementToggle, hk => MovementToggle());
            }
        }

        internal static void Stop()
        {
            if (!_hotkeysRegistered)
                return;

            _hotkeysRegistered = false;

            // remove hotkeys for commands with 1:1 key assignment          
            HotkeysManager.Unregister("AOE");
            HotkeysManager.Unregister("Combat");
            HotkeysManager.Unregister("Cooldown");
            HotkeysManager.Unregister("PullMore");
            HotkeysManager.Unregister("Movement");

            ////Suspend Movement keys have to be polled for now instead of using HotKey interface since defining a HotKey won't allow the key to pass through to game client window
            //// remove hotkeys for commands with 1:M key assignment
            //if (_registeredMovementSuspendKeys != null)
            //{
            //    foreach (var key in _registeredMovementSuspendKeys)
            //    {
            //        HotkeysManager.Unregister("Movement Suspend(" + key.ToString() + ")");
            //    }
            //}
        }

        internal static void TemporaryMovementKeyHandler()
        {
            // bail out if temporary movement suspensio not enabled
            if (!HotkeySettings.SuspendMovement)
                return;

            // loop through array (ugghhh) polling for keys current state
            foreach (Keys key in HotkeySettings.SuspendMovementKeys)
            {
                if (IsKeyDown(key))
                {
                    MovementTemporary_Suspend(key);
                    break;
                }
            }

            if (IsMovementTemporarilySuspended != _lastIsMovementTemporarilySuspended)
            {
                _lastIsMovementTemporarilySuspended = IsMovementTemporarilySuspended;

                // keep these notifications in Log window only
                Logger.Write(LogColor.Hilite, _lastIsMovementTemporarilySuspended ? "Bot Movement disabled during user movement..." : "Bot Movement restored!");

                MovementManager.Update();
            }
        }

        internal static void Update()
        {
            if (_hotkeysRegistered)
            {
                Stop();
                Start();
            }
        }

        // state toggle helpers
        private static bool AoeToggle()
        {
            _aoeEnabled = !_aoeEnabled;
#if !REACT_TO_HOTKEYS_IN_PULSE
            AoeKeyHandler();
#endif
            return (_aoeEnabled);
        }

        private static bool CombatToggle()
        {
            _combatEnabled = !_combatEnabled;
#if !REACT_TO_HOTKEYS_IN_PULSE
            CombatKeyHandler();
#endif
            return (_combatEnabled);
        }

        private static bool CooldownToggle()
        {
            _cooldownEnabled = !_cooldownEnabled;
#if !REACT_TO_HOTKEYS_IN_PULSE
            CooldownKeyHandler();
#endif
            return (_cooldownEnabled);
        }

        private static bool DefensiveCooldownToggle()
        {
            _defensiveCooldownEnabled = !_defensiveCooldownEnabled;
#if !REACT_TO_HOTKEYS_IN_PULSE
            CooldownKeyHandler();
#endif
            return (_defensiveCooldownEnabled);
        }

        private static void InitKeyStates()
        {
            // reset these values so we begin at same state every Start
            _aoeEnabled = true;
            _combatEnabled = true;
            _cooldownEnabled = true;
            _defensiveCooldownEnabled = true;
            _interruptEnabled = true;
            _pullMoreEnabled = true;
            _movementEnabled = true;
            _movementTemporarySuspendEndtime = DateTime.MinValue;

            _lastIsAoeEnabled = true;
            _lastIsCombatEnabled = true;
            _lastIsCooldownEnabled = true;
            _lastIsDefensiveCooldownEnabled = true;
            _lastIsInterruptEnabled = true;
            _lastIsPullMoreEnabled = true;
            _lastIsMovementEnabled = true;
            _lastIsMovementTemporarilySuspended = false;
        }

        private static bool InterruptToogle()
        {
            _interruptEnabled = !_interruptEnabled;
#if !REACT_TO_HOTKEYS_IN_PULSE
            InterruptKeyHandler();
#endif
            return (_interruptEnabled);
        }

        private static bool IsKeyDown(Keys key)
        {
            return (GetAsyncKeyState((int) key) & 0x8000) != 0;
        }

        /// <summary>
        /// returns true if WOW keyboard focus is in a frame/entry field
        /// </summary>
        /// <returns></returns>
        private static bool IsWowKeyBoardFocusInFrame()
        {
            List<string> ret = Lua.GetReturnValues("return GetCurrentKeyBoardFocus()");
            return ret != null;
        }

        private static void MovementTemporary_Suspend(Keys key)
        {
            _lastMovementTemporarySuspendKey = key;
            if (_movementEnabled)
            {
                if (!IsWowKeyBoardFocusInFrame())
                    IsMovementTemporarilySuspended = true;

#if !REACT_TO_HOTKEYS_IN_PULSE
                TemporaryMovementKeyHandler();
#endif
            }
        }

        private static bool MovementToggle()
        {
            _movementEnabled = !_movementEnabled;
            if (!_movementEnabled)
                StopMoving.Now();

#if !REACT_TO_HOTKEYS_IN_PULSE
            MovementKeyHandler();
#endif
            return (_movementEnabled);
        }

        private static bool PullMoreToggle()
        {
            _pullMoreEnabled = _pullMoreEnabled ? false : true;
#if !REACT_TO_HOTKEYS_IN_PULSE
            PullMoreKeyHandler();
#endif
            return (_pullMoreEnabled);
        }

        private static void RegisterHotkeyAssignment(string name, Keys key, Action<Hotkey> callback)
        {
            Keys keyCode = key & Keys.KeyCode;
            ModifierKeys mods = ModifierKeys.NoRepeat;

            if ((key & Keys.Shift) != 0)
                mods |= ModifierKeys.Shift;
            if ((key & Keys.Alt) != 0)
                mods |= ModifierKeys.Alt;
            if ((key & Keys.Control) != 0)
                mods |= ModifierKeys.Control;

            Logger.Write(LogColor.Hilite, "Hotkey: To disable {0}, press: [{1}]", name, key.ToFormattedString());
            HotkeysManager.Register(name, keyCode, mods, callback);
        }

        private static void TellUser(string template, params object[] args)
        {
            string msg = string.Format(template, args);
            Logger.Write(Color.Yellow, string.Format("Hotkey: " + msg));
            if (HotkeySettings.ChatFrameMessage)
                Lua.DoString(string.Format("print('{0}!')", msg));
        }

        private static string ToFormattedString(this Keys key)
        {
            string txt = "";

            if ((key & Keys.Shift) != 0)
                txt += "Shift+";
            if ((key & Keys.Alt) != 0)
                txt += "Alt+";
            if ((key & Keys.Control) != 0)
                txt += "Ctrl+";
            txt += (key & Keys.KeyCode).ToString();
            return txt;
        }

        #endregion
    }
}