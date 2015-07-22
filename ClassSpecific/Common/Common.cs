using System;
using System.Collections.Generic;
using System.Linq;
using Singular.Helpers;
using Singular.Managers;
using Singular.Settings;
using Styx;
using Styx.CommonBot;
using Styx.TreeSharp;
using Styx.WoWInternals.WoWObjects;

namespace Singular.ClassSpecific.Common
{
    public abstract class Common
    {
        // ReSharper disable InconsistentNaming

        #region Fields

        protected static readonly Func<Composite> use_trinket = () =>
        {
            if (SingularSettings.Instance.Trinket1Usage == TrinketUsage.Never &&
                SingularSettings.Instance.Trinket2Usage == TrinketUsage.Never)
            {
                return new Styx.TreeSharp.Action(ret => RunStatus.Failure);
            }

            var ps = new PrioritySelector();

            if (SingularSettings.IsTrinketUsageWanted(TrinketUsage.OnCooldownInCombat))
            {
                ps.AddChild(new Decorator(
                    ret => StyxWoW.Me.Combat && StyxWoW.Me.GotTarget() && ((StyxWoW.Me.IsMelee() && StyxWoW.Me.CurrentTarget.IsWithinMeleeRange) || StyxWoW.Me.CurrentTarget.SpellDistance() < 40),
                    Item.UseEquippedTrinket(TrinketUsage.OnCooldownInCombat)));
            }

            return ps;
        };

        private static readonly WoWItemWeaponClass[] _oneHandWeaponClasses = {WoWItemWeaponClass.Axe, WoWItemWeaponClass.Mace, WoWItemWeaponClass.Sword, WoWItemWeaponClass.Dagger, WoWItemWeaponClass.Fist};

        #endregion

        #region Properties

        public static bool t18_class_trinket
        {
            get { return StyxWoW.Me.Inventory.GetItemBySlot((uint) WoWInventorySlot.Trinket1).ItemInfo.Id == 124230 || StyxWoW.Me.Inventory.GetItemBySlot((uint) WoWInventorySlot.Trinket2).ItemInfo.Id == 124230; }
        }

        protected static LocalPlayer Me
        {
            get { return StyxWoW.Me; }
        }

        protected static int active_enemies
        {
            get { return Spell.UseAoe ? active_enemies_list.Count() : 1; }
        }

        protected static IEnumerable<WoWUnit> active_enemies_list
        {
            get
            {
                var distance = 40;

                switch (StyxWoW.Me.Specialization)
                {
                    case WoWSpec.DeathKnightUnholy:
                    case WoWSpec.DeathKnightFrost:
                        distance = TalentManager.HasGlyph(DeathKnight.DkSpells.blood_boil) ? 15 : 10;
                        break;
                    case WoWSpec.DeathKnightBlood:
                        distance = 20;
                        break;
                }

                return SingularRoutine.Instance.ActiveEnemies.Where(u => u.Distance <= distance);
            }
        }

        protected static double gcd
        {
            get { return SpellManager.GlobalCooldownLeft.TotalSeconds; }
        }

        protected static string prev_gcd
        {
            get { return Spell.PreviousGcdSpell; }
        }

        protected static double spell_haste
        {
            get { return StyxWoW.Me.SpellHasteModifier; }
        }

        #endregion

        #region Private Methods

        protected static int EnemiesCountNearTarget(WoWUnit target, byte distance)
        {
            return active_enemies_list.Where(x => target != x).Count(x => target.Location.Distance(x.Location) <= distance);
        }

        #endregion

        #region Types

        protected static class health
        {
            #region Properties

            public static double pct
            {
                get { return Me.HealthPercent; }
            }

            #endregion
        }

        protected static class main_hand
        {
            #region Properties

            public static bool _1h
            {
                get { return Me.Inventory.Equipped.MainHand != null && _oneHandWeaponClasses.Contains(Me.Inventory.Equipped.MainHand.ItemInfo.WeaponClass); }
            }

            public static bool _2h
            {
                get { return Me.Inventory.Equipped.MainHand != null && _oneHandWeaponClasses.Contains(Me.Inventory.Equipped.MainHand.ItemInfo.WeaponClass) == false; }
            }

            #endregion
        }

        protected static class mana
        {
            #region Properties

            public static double pct
            {
                get { return Me.ManaPercent; }
            }

            #endregion
        }

        protected static class target
        {
            // ReSharper disable MemberHidesStaticFromOuterClass

            #region Properties

            public static double distance
            {
                get { return StyxWoW.Me.CurrentTarget.Distance; }
            }

            public static long time_to_die
            {
                get { return StyxWoW.Me.CurrentTarget.TimeToDeath(long.MaxValue); }
            }

            #endregion

            #region Types

            public static class health
            {
                #region Properties

                public static double pct
                {
                    get { return StyxWoW.Me.CurrentTarget.HealthPercent; }
                }

                #endregion
            }

            #endregion
        }

        #endregion
    }
}