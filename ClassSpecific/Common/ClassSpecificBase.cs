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
using Action = Styx.TreeSharp.Action;

namespace Singular.ClassSpecific.Common
{
    // ReSharper disable InconsistentNaming
    public abstract class ClassSpecificBase
    {
        #region Fields

        public const string ancient_hysteria = "Ancient Hysteria";
        public const string bloodlust = "Bloodlust";
        public const string time_warp = "Time Warp";

        public static readonly string[] BloodlustEquivalents = {ancient_hysteria, bloodlust, time_warp};

        protected static readonly Func<Func<bool>, Composite> arcane_torrent = cond => Spell.BuffSelfAndWait("Arcane Torrent", req => Spell.UseCooldown && cond(), gcd: HasGcd.No);
        protected static readonly Func<Func<bool>, Composite> berserking = cond => Spell.BuffSelfAndWait("Berserking", req => Spell.UseCooldown && cond(), gcd: HasGcd.No);
        protected static readonly Func<Func<bool>, Composite> blood_fury = cond => Spell.BuffSelfAndWait("Blood Fury", req => Spell.UseCooldown && cond(), gcd: HasGcd.No);

        protected static readonly Func<Composite> use_trinket = () =>
        {
            if (SingularSettings.Instance.Trinket1Usage == TrinketUsage.Never &&
                SingularSettings.Instance.Trinket2Usage == TrinketUsage.Never)
            {
                return new Action(ret => RunStatus.Failure);
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

        private static readonly Dictionary<WoWClass, uint> T18ClassTrinketIds = new Dictionary<WoWClass, uint>
        {
            {WoWClass.DeathKnight, 124513}, // Reaper's Harvest
            {WoWClass.Druid, 124514}, // Seed of Creation
            {WoWClass.Hunter, 124515}, // Talisman of the Master Tracker
            {WoWClass.Mage, 124516}, // Tome of Shifting Words
            {WoWClass.Monk, 124517}, // Sacred Draenic Incense
            {WoWClass.Paladin, 124518}, // Libram of Vindication
            {WoWClass.Priest, 124519}, // Repudiation of War
            {WoWClass.Rogue, 124520}, // Bleeding Hollow Toxin Vessel
            {WoWClass.Shaman, 124521}, // Core of the Primal Elements
            {WoWClass.Warlock, 124522}, // Fragment of the Dark Star
            {WoWClass.Warrior, 124523}, // Worldbreaker's Resolve
        };

        private static readonly WoWItemWeaponClass[] _oneHandWeaponClasses = {WoWItemWeaponClass.Axe, WoWItemWeaponClass.Mace, WoWItemWeaponClass.Sword, WoWItemWeaponClass.Dagger, WoWItemWeaponClass.Fist};
        private static double? _baseGcd;

        #endregion

        #region Properties

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

        protected static double gcd_max
        {
            get
            {
                if (_baseGcd == null)
                {
                    switch (Me.Class)
                    {
                        case WoWClass.DeathKnight:
                        case WoWClass.Hunter:
                        case WoWClass.Monk:
                        case WoWClass.Rogue:
                            _baseGcd = 1;
                            break;
                        case WoWClass.Druid:
                            _baseGcd = Me.Shapeshift == ShapeshiftForm.Cat ? 1 : 1.5;
                            break;
                        default:
                            _baseGcd = 1.5;
                            break;
                    }
                }

                var gcdMax = _baseGcd.Value * Me.SpellHasteModifier;

                return gcdMax < 1 ? 1.0 : gcdMax;
            }
        }

        protected static string prev_gcd
        {
            get { return Spell.PreviousGcdSpell; }
        }


        protected static double spell_haste
        {
            get { return StyxWoW.Me.SpellHasteModifier; }
        }

        protected static bool t18_class_trinket
        {
            get
            {
                if (!T18ClassTrinketIds.ContainsKey(Me.Class)) return false;
                var classTrinketId = T18ClassTrinketIds[Me.Class];

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

        #endregion

        #region Private Methods

        protected static IOrderedEnumerable<WoWUnit> Enemies(byte distance)
        {
            return active_enemies_list.Where(x => x.Distance <= distance).OrderBy(x => x.Distance);
        }

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

        protected static class set_bonus
        {
            #region fields

            private static readonly WoWInventorySlot[] _setPartsSlots =
            {
                WoWInventorySlot.Chest,
                WoWInventorySlot.Hands,
                WoWInventorySlot.Head,
                WoWInventorySlot.Legs,
                WoWInventorySlot.Shoulder
            };

            private static readonly Dictionary<WoWClass, uint[]> _t17Sets = new Dictionary<WoWClass, uint[]>
            {
                {WoWClass.DeathKnight, new uint[] {115535, 115536, 115537, 115538, 115539}},
                {WoWClass.Druid, new uint[] {115540, 115541, 115542, 115543, 115544}},
                {WoWClass.Hunter, new uint[] {115545, 115546, 115547, 115548, 115549}},
                {WoWClass.Mage, new uint[] {115550, 115551, 115552, 115553, 115554}},
                {WoWClass.Monk, new uint[] {115555, 115556, 115557, 115558, 115559}},
                {WoWClass.Paladin, new uint[] {115565, 115566, 115567, 115568, 115569}},
                {WoWClass.Priest, new uint[] {115560, 115561, 115562, 115563, 115564}},
                {WoWClass.Rogue, new uint[] {115570, 115571, 115572, 115573, 115574}},
                {WoWClass.Shaman, new uint[] {115575, 115576, 115577, 115578, 115579}},
                {WoWClass.Warlock, new uint[] {115585, 115586, 115587, 115588, 115589}},
                {WoWClass.Warrior, new uint[] {115580, 115581, 115582, 115583, 115584}}
            };

            private static readonly Dictionary<WoWClass, uint[]> _t18Sets = new Dictionary<WoWClass, uint[]>
            {
                {WoWClass.DeathKnight, new uint[] {124317, 124327, 124332, 124338, 124344}},
                {WoWClass.Druid, new uint[] {124246, 124255, 124261, 124267, 124272}},
                {WoWClass.Hunter, new uint[] {124284, 124292, 124296, 124301, 124307}},
                {WoWClass.Mage, new uint[] {124154, 124160, 124165, 124171, 124177}},
                {WoWClass.Monk, new uint[] {124247, 124256, 124262, 124268, 124273}},
                {WoWClass.Paladin, new uint[] {124318, 124328, 124333, 124339, 124345}},
                {WoWClass.Priest, new uint[] {124155, 124161, 124166, 124172, 124178}},
                {WoWClass.Rogue, new uint[] {124248, 124257, 124263, 124269, 124274}},
                {WoWClass.Shaman, new uint[] {124293, 124297, 124302, 124303, 124308}},
                {WoWClass.Warlock, new uint[] {124156, 124162, 124167, 124173, 124179}},
                {WoWClass.Warrior, new uint[] {124319, 124329, 124334, 124340, 124346}}
            };

            #endregion

            #region Properties

            public static bool tier17_2pc
            {
                get { return SetPartsCount(_t17Sets) >= 2; }
            }

            public static bool tier17_4pc
            {
                get { return SetPartsCount(_t17Sets) >= 4; }
            }

            public static bool tier18_2pc
            {
                get { return SetPartsCount(_t18Sets) >= 2; }
            }

            public static bool tier18_4pc
            {
                get { return SetPartsCount(_t18Sets) >= 4; }
            }

            #endregion

            #region Private Methods

            private static int SetPartsCount(IReadOnlyDictionary<WoWClass, uint[]> set)
            {
                if (!set.ContainsKey(Me.Class)) return 0;
                var ids = set[Me.Class];

                return _setPartsSlots.Select(woWInventorySlot => StyxWoW.Me.Inventory.GetItemBySlot((uint) woWInventorySlot)).Count(item => item != null && ids.Contains(item.ItemInfo.Id));
            }

            #endregion
        }

        protected static class target
        {
            // ReSharper disable MemberHidesStaticFromOuterClass

            #region Properties

            public static WoWUnit current
            {
                get { return Me.CurrentTarget; }
            }

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