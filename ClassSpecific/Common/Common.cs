using System.Collections.Generic;
using System.Linq;
using Singular.Helpers;
using Singular.Managers;
using Styx;
using Styx.CommonBot;
using Styx.WoWInternals.WoWObjects;

namespace Singular.ClassSpecific.Common
{
    // ReSharper disable InconsistentNaming
    public abstract class Common
    {
        #region Properties

        protected static LocalPlayer Me
        {
            get { return StyxWoW.Me; }
        }

        protected static int active_enemies
        {
            get { return Spell.UseAOE ? active_enemies_list.Count() : 1; }
        }

        protected static IEnumerable<WoWUnit> active_enemies_list
        {
            get
            {
                var distance = 40;

                switch (StyxWoW.Me.Specialization)
                {
                    case WoWSpec.DeathKnightUnholy:
                        distance = TalentManager.HasGlyph(DeathKnight.DkSpells.blood_boil) ? 15 : 10;
                        break;
                    case WoWSpec.DeathKnightBlood:
                        distance = 20;
                        break;
                }

                return SingularRoutine.Instance.ActiveEnemies.Where(u => u.Distance < distance);
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

            //public static long time_to_die
            //{
            //    get { return StyxWoW.Me.CurrentTarget.TimeToDeath(int.MaxValue); }
            //}
        }

        #endregion
    }
}