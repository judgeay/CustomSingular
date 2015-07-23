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

        protected const string bloodlust = "Bloodlust";

        protected static readonly Func<Func<bool>, Composite> arcane_torrent = cond => Spell.Cast("Arcane Torrent", req => Spell.UseCooldown && cond());
        protected static readonly Func<Func<bool>, Composite> berserking = cond => Spell.Cast("Berserking", req => Spell.UseCooldown && cond());
        protected static readonly Func<Func<bool>, Composite> blood_fury = cond => Spell.Cast("Blood Fury", req => Spell.UseCooldown && cond());

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
            get
            {
                int classTrinketId;
                switch (Me.Class)
                {
                    case WoWClass.DeathKnight:
                        classTrinketId = 124513;
                        break;
                    case WoWClass.Druid:
                        classTrinketId = 124514;
                        break;
                    case WoWClass.Hunter:
                        classTrinketId = 124515;
                        break;
                    case WoWClass.Mage:
                        classTrinketId = 124516;
                        break;
                    case WoWClass.Monk:
                        classTrinketId = 124517;
                        break;
                    case WoWClass.Paladin:
                        classTrinketId = 124518;
                        break;
                    case WoWClass.Priest:
                        classTrinketId = 124519;
                        break;
                    case WoWClass.Rogue:
                        classTrinketId = 124520;
                        break;
                    case WoWClass.Shaman:
                        classTrinketId = 124521;
                        break;
                    case WoWClass.Warlock:
                        classTrinketId = 124522;
                        break;
                    case WoWClass.Warrior:
                        classTrinketId = 124523;
                        break;
                    default:
                        classTrinketId = 0;
                        break;
                }

                if (classTrinketId == 0) return false;
                
                var trinket1 = StyxWoW.Me.Inventory.GetItemBySlot((uint) WoWInventorySlot.Trinket1);
                var trinket2 = StyxWoW.Me.Inventory.GetItemBySlot((uint) WoWInventorySlot.Trinket2);

                if (trinket1 != null && trinket2 != null)
                    return trinket1.ItemInfo.Id == classTrinketId || trinket2.ItemInfo.Id == classTrinketId;
                if (trinket1 != null)
                    return trinket1.ItemInfo.Id == classTrinketId;
                if (trinket2 != null)
                    return trinket2.ItemInfo.Id == classTrinketId;

                return false;
            }
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