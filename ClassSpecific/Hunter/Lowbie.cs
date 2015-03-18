using Singular.Dynamics;
using Singular.Helpers;
using Styx;

using Styx.TreeSharp;

namespace Singular.ClassSpecific.Hunter
{
    public class Lowbie
    {
        [Behavior(BehaviorType.Combat|BehaviorType.Pull,WoWClass.Hunter,0)]
        public static Composite CreateLowbieCombat()
        {
            return new PrioritySelector(
                Helpers.Common.EnsureReadyToAttackFromMediumRange(),

                Spell.WaitForCastOrChannel(),

                new Decorator( 
                    ret => !Spell.IsGlobalCooldown(),
                    new PrioritySelector(
                        Helpers.Common.CreateInterruptBehavior(),

                        // Heal pet when below 70
                        Spell.Cast("Mend Pet", ret =>StyxWoW.Me.GotAlivePet && StyxWoW.Me.Pet.HealthPercent < 70 && !StyxWoW.Me.Pet.HasAura("Mend Pet")),

                        Movement.WaitForFacing(),
                        Movement.WaitForLineOfSpellSight(),

                        Spell.Cast(
                            "Concussive Shot",
                            ret => StyxWoW.Me.CurrentTarget.CurrentTarget == null || StyxWoW.Me.CurrentTarget.CurrentTarget == StyxWoW.Me),
                        Spell.Cast("Arcane Shot"),
                        Spell.Cast("Steady Shot")
                        )
                    )
                );
        }
    }
}
