using System;
using System.Linq;
using Singular.Dynamics;
using Singular.Managers;
using Styx;
using Styx.CommonBot;
using Styx.TreeSharp;
using Action = Styx.TreeSharp.Action;
using Styx.WoWInternals;
using CommonBehaviors.Actions;
using Singular.Settings;
using Styx.WoWInternals.WoWObjects;
using Styx.CommonBot.Routines;

namespace Singular.Helpers
{
    internal static class Common
    {
        private static LocalPlayer Me { get { return StyxWoW.Me; } }

        /// <summary>
        ///  Creates a behavior to start auto attacking to current target.
        /// </summary>
        /// <remarks>
        ///  Created 23/05/2011
        /// </remarks>
        /// <returns></returns>
        private static Composite CreateAutoAttack()
        {
            var aaprio = new PrioritySelector();
            var saprio = new PrioritySelector();

            // const int spellIdAutoShot = 75;
            var autoAttackMelee =
                   TalentManager.CurrentSpec == WoWSpec.None
                || Me.Class == WoWClass.DeathKnight
                || TalentManager.CurrentSpec == WoWSpec.DruidGuardian
                || TalentManager.CurrentSpec == WoWSpec.DruidFeral
                || Me.Class == WoWClass.Monk && TalentManager.CurrentSpec == WoWSpec.None
                || TalentManager.CurrentSpec == WoWSpec.MonkBrewmaster
                || TalentManager.CurrentSpec == WoWSpec.MonkWindwalker
                || TalentManager.CurrentSpec == WoWSpec.MonkMistweaver && !SpellManager.HasSpell("Crackling Jade Lightning")
                || Me.Class == WoWClass.Paladin
                || Me.Class == WoWClass.Rogue
                || TalentManager.CurrentSpec == WoWSpec.ShamanEnhancement
                || Me.Class == WoWClass.Warrior
                ;

            var autoAttackRanged = Me.Class == WoWClass.Hunter;

            saprio.AddChild(
                new Decorator(
                    req => !SingularRoutine.IsAllowed(CapabilityFlags.SpecialAttacks),
                    new ThrottlePasses(
                        1, TimeSpan.FromSeconds(1), RunStatus.Success,
                        new Action(r =>
                        {
                            if (!StyxWoW.Me.IsAutoAttacking && Me.GotTarget() && Me.IsSafelyFacing(Me.CurrentTarget) && Me.CurrentTarget.IsWithinMeleeRange)
                            {
                                var unit = Me.CurrentTarget;
                                Logger.Write(LogColor.Hilite, "/startattack on {0} @ {1:F1} yds", unit.SafeName(), unit.SpellDistance());
                                Lua.DoString("StartAttack()");
                            }
                            return RunStatus.Success;
                        })
                        )
                    )
                );

            if (autoAttackMelee)
            {
                aaprio.AddChild(
                    new Decorator(
                        ret => !StyxWoW.Me.IsAutoAttacking && Me.GotTarget() && Me.CurrentTarget.IsWithinMeleeRange, // && StyxWoW.Me.AutoRepeatingSpellId != spellIdAutoShot,
                        new Action(ret =>
                        {
                            var unit = Me.CurrentTarget;
                            Logger.Write( LogColor.Hilite, "/startattack on {0} @ {1:F1}% at {2:F1} yds", unit.SafeName(), unit.HealthPercent, unit.SpellDistance());
                            Lua.DoString("StartAttack()");
                            return RunStatus.Failure;
                        })
                        )
                    );
            }

            if (autoAttackRanged)
            {
                aaprio.AddChild(
                    new Decorator(
                        ret => 
                        {
                            if (StyxWoW.Me.IsAutoAttacking)
                                return false;
                            if (!Me.GotTarget())
                                return false;
                            return Me.CurrentTarget.SpellDistance() < 40;
                        },                                
                        new Action(ret =>
                        {
                            var unit = Me.CurrentTarget;
                            Logger.Write( LogColor.Hilite, "/startattack on {0} @ {1:F1} yds", unit.SafeName(), unit.SpellDistance());
                            Lua.DoString("StartAttack()");
                            return RunStatus.Failure;
                        })
                        )
                    );
            }

            return new PrioritySelector(
                saprio,
                new ThrottlePasses(
                    TimeSpan.FromSeconds(1),
                    aaprio
                    )
                );
        }

        /// <summary>
        ///  Creates a behavior to start auto attacking to current target.
        /// </summary>
        /// <remarks>
        ///  Created 23/05/2011
        /// </remarks>
        /// <returns></returns>
        private static Composite CreatePetAttack()
        {
            var prio = new PrioritySelector();

            if (SingularRoutine.CurrentWoWContext != WoWContext.Normal || !SingularSettings.Instance.PetTankAdds)
            {
                // pet assist: always keep pet on my CurrentTarget
                prio.AddChild(
                    new ThrottlePasses(
                        1,
                        TimeSpan.FromMilliseconds(500),
                        RunStatus.Failure,
                        new Action( r => 
                        {
                            if (Me.GotAlivePet)
                            {
                                var petUse = PetManager.IsPetUseAllowed;
                                if (!petUse)
                                {
                                    if (Me.Pet.GotTarget() && Me.Pet.Combat)
                                    {
                                        PetManager.Passive();   // set to passive
                                    }
                                }
                                else
                                {
                                    if (!Me.Pet.GotTarget() || Me.Pet.CurrentTargetGuid != Me.CurrentTargetGuid)
                                    {
                                        PetManager.Attack(Me.CurrentTarget);
                                    }
                                }
                            }
                        })
                        )
                    );
            }
            else
            {
                // pet tank: if pet's target isn't targeting Me, check if we should switch to one that is targeting Me
                prio.AddChild(
                    new ThrottlePasses(
                        1,
                        TimeSpan.FromMilliseconds(500),
                        RunStatus.Failure,
                        new Action( r =>
                        {
                            if (Me.GotAlivePet)
                            {
                                var petUse = SingularRoutine.IsAllowed(CapabilityFlags.PetUse);
                                if (!petUse)
                                {
                                    if (Me.Pet.GotTarget() && Me.Pet.Combat)
                                    {
                                        PetManager.CastAction("Passive");   // set to passive
                                    }
                                }
                                else
                                {
                                    if (Me.Pet.CurrentTarget == null || Me.Pet.CurrentTarget.CurrentTargetGuid != Me.Guid)
                                    {
                                        // pickup aggroed mobs I'm not attacking (grab easy agro first)
                                        var aggroedOnMe = Unit.NearbyUnfriendlyUnits
                                            .Where(u => u.Combat && u.GotTarget() && u.CurrentTarget.IsMe && u.Guid != Me.CurrentTargetGuid && !u.IsCrowdControlled())
                                            .OrderBy(u => u.Location.DistanceSqr(Me.Pet.Location))
                                            .FirstOrDefault() ?? Me.CurrentTarget;
                                        
                                        // otherwise, pickup My CurrentTarget

                                        if (aggroedOnMe != null)
                                        {
                                            if (SingularSettings.Debug)
                                            {
                                                string reason = aggroedOnMe == Me.CurrentTarget ? "MyCurrTarget" : "PickupAggro";

                                                Logger.WriteDebug("PetManager: [reason={0}] sending Pet at {1} @ {2:F1} yds from Pet", reason, aggroedOnMe.SafeName(), Me.Pet.SpellDistance(r as WoWUnit));
                                            }

                                            PetManager.Attack(aggroedOnMe);
                                        }
                                    }
                                }
                            }
                        })
                        )
                    );
            }

            return prio;
        }

        private static WoWUnit _unitInterrupt;

        /// <summary>Creates an interrupt spell cast composite. This attempts to use spells in order of range (shortest to longest).  
        /// behavior consists only of spells that apply to current toon based upon class, spec, and race
        /// </summary>
        public static Composite CreateInterruptBehavior()
        {
            if ( SingularSettings.Instance.InterruptTarget == CheckTargets.None )
                return new ActionAlwaysFail();

            Composite actionSelectTarget;
            if (SingularSettings.Instance.InterruptTarget == CheckTargets.Current)
                actionSelectTarget = new Action( 
                    ret => {
                        _unitInterrupt = null;
                        //if (Me.Class == WoWClass.Shaman && ClassSpecific.Shaman.Totems.Exist(WoWTotem.Grounding))
                        //    return RunStatus.Failure;

                        var u = Me.CurrentTarget;
                        _unitInterrupt = IsInterruptTarget(u) ? u : null;
                        if (_unitInterrupt != null && SingularSettings.Debug)
                            Logger.WriteDebug("Possible Interrupt Target: {0} @ {1:F1} yds casting {2} #{3} for {4} ms", _unitInterrupt.SafeName(), _unitInterrupt.Distance, _unitInterrupt.CastingSpell.Name, _unitInterrupt.CastingSpell.Id, _unitInterrupt.CurrentCastTimeLeft.TotalMilliseconds );

                        return _unitInterrupt == null ? RunStatus.Failure : RunStatus.Success;
                    }
                    );
            else // if ( SingularSettings.Instance.InterruptTarget == InterruptType.All )
            {
                actionSelectTarget = new Action( 
                    ret => {
                        _unitInterrupt = null;
                        //if (Me.Class == WoWClass.Shaman && ClassSpecific.Shaman.Totems.Exist(WoWTotem.Grounding))
                        //    return RunStatus.Failure;

                        _unitInterrupt = Unit.NearbyUnitsInCombatWithMeOrMyStuff.Where(IsInterruptTarget).OrderBy(u => u.Distance).FirstOrDefault();
                        if (_unitInterrupt != null && SingularSettings.Debug)
                            Logger.WriteDebug("Possible Interrupt Target: {0} @ {1:F1} yds casting {2} #{3} for {4} ms", _unitInterrupt.SafeName(), _unitInterrupt.Distance, _unitInterrupt.CastingSpell.Name, _unitInterrupt.CastingSpell.Id, _unitInterrupt.CurrentCastTimeLeft.TotalMilliseconds);

                        return _unitInterrupt == null ? RunStatus.Failure : RunStatus.Success;
                        }
                    );
            }

            var prioSpell = new PrioritySelector();

            //#region Pet Spells First!

            //if (Me.Class == WoWClass.Warlock)
            //{
            //    // this will be either a Optical Blast or Spell Lock
            //    prioSpell.AddChild( 
            //        Spell.Cast( 
            //            "Command Demon", 
            //            on => _unitInterrupt, 
            //            ret => _unitInterrupt != null 
            //                && _unitInterrupt.Distance < 40 
            //                && (
            //                    ClassSpecific.Warlock.Common.GetCurrentPet() == WarlockPet.Felhunter 
            //                    || ClassSpecific.Warlock.Common.GetCurrentPet() == WarlockPet.Doomguard 
            //                   )
            //            )
            //        );
            //}

            //#endregion

            #region Melee Range

            if ( Me.Class == WoWClass.Paladin )
                prioSpell.AddChild( Spell.Cast("Rebuke", ctx => _unitInterrupt));

            if ( Me.Class == WoWClass.Rogue)
            {
                prioSpell.AddChild( Spell.Cast("Kick", ctx => _unitInterrupt));
                if ( TalentManager.HasGlyph("Gouge"))
                    prioSpell.AddChild(Spell.Cast("Gouge", ctx => _unitInterrupt, ret => !_unitInterrupt.IsBoss() && Me.IsSafelyFacing(_unitInterrupt, 150f)));
                else
                    prioSpell.AddChild(Spell.Cast("Gouge", ctx => _unitInterrupt, ret => !_unitInterrupt.IsBoss() && Me.IsSafelyFacing(_unitInterrupt, 150f) && _unitInterrupt.IsSafelyFacing(Me, 150f)));
            }

            if ( Me.Class == WoWClass.Warrior)
                prioSpell.AddChild( Spell.Cast("Pummel", ctx => _unitInterrupt));

            if ( Me.Class == WoWClass.Monk )
                prioSpell.AddChild( Spell.Cast("Spear Hand Strike", ctx => _unitInterrupt));

            if ( Me.Class == WoWClass.Druid)
            {
                // Spell.Cast("Skull Bash (Cat)", ctx => _unitInterrupt, ret => StyxWoW.Me.Shapeshift == ShapeshiftForm.Cat));
                // Spell.Cast("Skull Bash (Bear)", ctx => _unitInterrupt, ret => StyxWoW.Me.Shapeshift == ShapeshiftForm.Bear));
                prioSpell.AddChild( Spell.Cast("Skull Bash", ctx => _unitInterrupt, ret => StyxWoW.Me.Shapeshift == ShapeshiftForm.Bear || StyxWoW.Me.Shapeshift == ShapeshiftForm.Cat));
                prioSpell.AddChild( Spell.Cast("Mighty Bash", ctx => _unitInterrupt, ret => !_unitInterrupt.IsBoss() && _unitInterrupt.IsWithinMeleeRange));
            }

            if ( Me.Class == WoWClass.DeathKnight)
                prioSpell.AddChild( Spell.Cast("Mind Freeze", ctx => _unitInterrupt));

            if ( Me.Race == WoWRace.Pandaren )
                prioSpell.AddChild( Spell.Cast("Quaking Palm", ctx => _unitInterrupt));

            #endregion

            #region 8 Yard Range

            if ( Me.Race == WoWRace.BloodElf )
                prioSpell.AddChild(Spell.Cast("Arcane Torrent", ctx => _unitInterrupt, req => _unitInterrupt.Distance < 8 && !Unit.NearbyUnfriendlyUnits.Any(u => u.IsSensitiveDamage( 8))));

            if ( Me.Race == WoWRace.Tauren)
                prioSpell.AddChild(Spell.Cast("War Stomp", ctx => _unitInterrupt, ret => _unitInterrupt.Distance < 8 && !_unitInterrupt.IsBoss() && !Unit.NearbyUnfriendlyUnits.Any(u => u.IsSensitiveDamage( 8))));

            #endregion

            #region 10 Yards

            if (Me.Class == WoWClass.Paladin)
                prioSpell.AddChild( Spell.Cast("Hammer of Justice", ctx => _unitInterrupt));

            if (TalentManager.CurrentSpec == WoWSpec.DruidBalance )
                prioSpell.AddChild( Spell.Cast("Hammer of Justice", ctx => _unitInterrupt));

            if (Me.Class == WoWClass.Warrior) 
                prioSpell.AddChild( Spell.Cast("Disrupting Shout", ctx => _unitInterrupt));

            #endregion

            #region 25 yards

            if ( Me.Class == WoWClass.Shaman)
                prioSpell.AddChild( Spell.Cast("Wind Shear", ctx => _unitInterrupt, req => Me.IsSafelyFacing(_unitInterrupt)));

            #endregion

            #region 30 yards
            // Druid
            if (TalentManager.HasGlyph("Fae Silence"))
                prioSpell.AddChild(Spell.Cast("Faerie Fire", ctx => _unitInterrupt, req => Me.Shapeshift == ShapeshiftForm.Bear));

            if (TalentManager.CurrentSpec == WoWSpec.PaladinProtection)
                prioSpell.AddChild( Spell.Cast("Avenger's Shield", ctx => _unitInterrupt));

            if (Me.Class == WoWClass.Warrior && TalentManager.HasGlyph("Gag Order"))
                // Gag Order only works on non-bosses due to it being a silence, not an interrupt!
                prioSpell.AddChild( Spell.Cast("Heroic Throw", ctx => _unitInterrupt, ret =>  !_unitInterrupt.IsBoss()));

            if ( Me.Class == WoWClass.Priest ) 
                prioSpell.AddChild( Spell.Cast("Silence", ctx => _unitInterrupt));

            if (Me.Class == WoWClass.DeathKnight)
                prioSpell.AddChild(Spell.Cast("Strangulate", ctx => _unitInterrupt));

            if (Me.Class == WoWClass.Mage)
                prioSpell.AddChild(Spell.Cast("Frostjaw", ctx => _unitInterrupt));

            #endregion

            #region 40 yards

            if ( Me.Class == WoWClass.Mage)
                prioSpell.AddChild( Spell.Cast("Counterspell", ctx => _unitInterrupt));

            if (Me.Class == WoWClass.Hunter)
                prioSpell.AddChild(Spell.Cast("Counter Shot", ctx => _unitInterrupt));

            if (TalentManager.CurrentSpec == WoWSpec.HunterMarksmanship)
                prioSpell.AddChild(Spell.Cast("Silencing Shot", ctx => _unitInterrupt));

            if (Me.Class == WoWClass.Druid)
                prioSpell.AddChild( Spell.Cast("Solar Beam", ctx => _unitInterrupt, ret => StyxWoW.Me.Shapeshift == ShapeshiftForm.Moonkin));

            if (TalentManager.CurrentSpec == WoWSpec.ShamanElemental || TalentManager.CurrentSpec == WoWSpec.ShamanEnhancement )
                prioSpell.AddChild( Spell.Cast("Solar Beam", ctx => _unitInterrupt, ret => true));

            #endregion

            return new ThrottlePasses( 2, TimeSpan.FromMilliseconds(500),  
                new Sequence(
                    actionSelectTarget,               
                    // majority of these are off GCD, so throttle all to avoid most fail messages
                    prioSpell 
                    )
                );
        }

        private static bool IsInterruptTarget(WoWUnit u)
        {
            if (u == null || !u.IsCasting)
                return false;

            if (!SingularSettings.Debug)
                return u.CanInterruptCurrentSpellCast && u.InLineOfSight && StyxWoW.Me.IsSafelyFacing(u, 150f);

            if (!u.CanInterruptCurrentSpellCast)
                ;   // Logger.WriteDebug("IsInterruptTarget: {0} casting {1} but CanInterruptCurrentSpellCast == false", u.SafeName(), (u.CastingSpell == null ? "(null)" : u.CastingSpell.Name));
            else if (!u.InLineOfSpellSight)
                ;   // Logger.WriteDebug("IsInterruptTarget: {0} casting {1} but LoSS == false", u.SafeName(), (u.CastingSpell == null ? "(null)" : u.CastingSpell.Name));
            else if (!StyxWoW.Me.IsSafelyFacing(u))
                ;   // Logger.WriteDebug("IsInterruptTarget: {0} casting {1} but Facing == false", u.SafeName(), (u.CastingSpell == null ? "(null)" : u.CastingSpell.Name));
            else if (u.CurrentCastTimeLeft.TotalMilliseconds < 250)
                ;
            else
                return true;

            return false;
        }


        /// <summary>
        /// Creates a dismount composite that only stops if we are flying.
        /// </summary>
        /// <param name="reason">The reason to dismount</param>
        /// <returns></returns>
        private static Composite CreateDismount(string reason)
        {
            const int zoneNagrand = 6755;
            const int spellTelaariTalbuk = 165803;
            const int spellFrostwolfWarWolf = 164222;

            return new Decorator(
                ret => StyxWoW.Me.Mounted 
                    && !MovementManager.IsMovementDisabled 
                    && (StyxWoW.Me.ZoneId != zoneNagrand || !Me.HasAura( Me.IsHorde ? spellFrostwolfWarWolf : spellTelaariTalbuk)),
                new Sequence(
                    new DecoratorContinue(ret => StyxWoW.Me.IsFlying,
                        new Sequence(
                            new DecoratorContinue(ret => StyxWoW.Me.IsMoving,
                                new Sequence(
                                    new Action(ret => Logger.WriteDebug("Stopping to descend..." + (!string.IsNullOrEmpty(reason) ? (" Reason: " + reason) : string.Empty))),
                                    new Action(ret => StopMoving.Now()),
                                    new Wait( 1, ret => !StyxWoW.Me.IsMoving, new ActionAlwaysSucceed())
                                    )
                                ),
                            new Action( ret => Logger.WriteDebug( "Descending to land..." + (!string.IsNullOrEmpty(reason) ? (" Reason: " + reason) : string.Empty))),
                            new Action(ret => WoWMovement.Move(WoWMovement.MovementDirection.Descend)),
                            new PrioritySelector(
                                new Wait( 1, ret => StyxWoW.Me.IsMoving, new ActionAlwaysSucceed()),
                                new Action( ret => Logger.WriteDebug( "warning -- tried to descend but IsMoving == false ....!"))
                                ),
                            new WaitContinue(30, ret => !StyxWoW.Me.IsFlying, new ActionAlwaysSucceed()),
                            new DecoratorContinue( 
                                ret => StyxWoW.Me.IsFlying, 
                                new Action( ret => Logger.WriteDebug( "error -- still flying -- descend appears to have failed....!"))
                                ),
                            new Action(ret => WoWMovement.MoveStop(WoWMovement.MovementDirection.Descend))
                            )
                        ), // and finally dismount. 
                    new Action(r => {
                        Logger.WriteDebug( "Dismounting..." + (!string.IsNullOrEmpty(reason) ? (" Reason: " + reason) : string.Empty));
                        var shapeshift = StyxWoW.Me.Shapeshift;
                        if (StyxWoW.Me.Class == WoWClass.Druid && (shapeshift == ShapeshiftForm.FlightForm || shapeshift == ShapeshiftForm.EpicFlightForm))
                            Lua.DoString("RunMacroText('/cancelform')");
                        else
                            Lua.DoString("Dismount()");
                        })
                    )
                );
        }

        /// <summary>
        /// This is meant to replace the 'SleepForLagDuration()' method. Should only be used in a Sequence
        /// </summary>
        /// <returns></returns>
        public static Composite CreateWaitForLagDuration()
        {
            // return new WaitContinue(TimeSpan.FromMilliseconds((SingularRoutine.Latency * 2) + 150), ret => false, new ActionAlwaysSucceed());
            return CreateWaitForLagDuration(ret => false);
        }

        /// <summary>
        /// Allows waiting for SleepForLagDuration() but ending sooner if condition is met
        /// </summary>
        /// <param name="orUntil">if true will stop waiting sooner than lag maximum</param>
        /// <returns></returns>
        private static Composite CreateWaitForLagDuration( CanRunDecoratorDelegate orUntil)
        {
            return new DynaWaitContinue(ts => TimeSpan.FromMilliseconds((SingularRoutine.Latency * 2) + 150), orUntil, new ActionAlwaysSucceed());
        }

        #region Wait for Rez Sickness

        private static string ClearStealthAfterRezSickness { get; set; }

        public static Composite CreateWaitForRessSickness()
        {
            Composite compClass = new ActionAlwaysFail();

            ///////
            // NOTE: must conditionally build the following behaviors as they reference class specific settings
            //  .. failure to do this will fail since we can only load class settings for active class
            ///////
            if (Me.Class == WoWClass.Druid)
                compClass = new Decorator(
                    req => SpellManager.HasSpell("Prowl"),
                    new Sequence(
                        Spell.BuffSelfAndWait(sp => "Prowl"),
                        new Action(r => ClearStealthAfterRezSickness = "Prowl"),
                        new Action(r => Logger.Write(LogColor.Hilite, "^Prowl: maintain while waiting out Rez Sickness"))
                        )
                    );

            if (Me.Class == WoWClass.Rogue)
                compClass = new Decorator(
                    req => SpellManager.HasSpell("Stealth"),
                    new Sequence(
                        Spell.BuffSelfAndWait( sp =>"Stealth"),
                        new Action(r => ClearStealthAfterRezSickness = "Stealth"),
                        new Action(r => Logger.Write(LogColor.Hilite, "^Stealth: maintain while waiting out Rez Sickness"))
                        )
                    );

            return new PrioritySelector(

                // behavior for waiting on Rez Sickness after it expires ( clearStealth will be non-null )
                new Decorator(
                    req => SingularSettings.Instance.ResSicknessWait && !string.IsNullOrEmpty(ClearStealthAfterRezSickness) && !StyxWoW.Me.HasAura("Resurrection Sickness"),
                    new Sequence(
                        new Action( r => Logger.WriteDiagnostic( "WaitForRezSickness: clearing the {0} used flag", ClearStealthAfterRezSickness)),
                        new DecoratorContinue(
                            ret => Me.HasAura(ClearStealthAfterRezSickness),
                            new Sequence(
                                new Action(ret => Logger.Write(LogColor.Cancel, "/cancel " + ClearStealthAfterRezSickness)),
                                new Action(ret => Me.CancelAura(ClearStealthAfterRezSickness)),
                                new Wait(TimeSpan.FromMilliseconds(500), ret => !Me.HasAura(ClearStealthAfterRezSickness), new ActionAlwaysSucceed())
                                )
                            ),
                        new Action( r => ClearStealthAfterRezSickness = null )
                        )
                    ),

                // behavior for while we have Rez Sickness
                new Decorator(
                    ret => SingularSettings.Instance.ResSicknessWait && StyxWoW.Me.HasAura("Resurrection Sickness"),
                    new PrioritySelector(
                        new Throttle(TimeSpan.FromMinutes(1), new Action(r => Logger.Write("Waiting out Resurrection Sickness (expires in {0:F0} seconds)", StyxWoW.Me.GetAuraTimeLeft("Resurrection Sickness", false).TotalSeconds))),
                        new Decorator(
                            req => !Me.HasAnyAura("Stealth", "Prowl", "Shadowmeld"),
                            new PrioritySelector(
                                compClass,
                                new Decorator(
                                    req => SingularSettings.Instance.UseRacials && SpellManager.HasSpell("Shadowmeld"),
                                    new Sequence(
                                        Spell.BuffSelfAndWait(sp => "Shadowmeld"),
                                        new Action(r => ClearStealthAfterRezSickness = "Shadowmeld"),
                                        new Action(r => Logger.Write(LogColor.Hilite, "^Shadowmeld: maintain while waiting out Rez Sickness"))
                                        )
                                    )
                                )
                            ),
                        new Action(ret => { })
                        )
                    )
                );
        }

        #endregion

        public static Composite EnsureReadyToAttackFromMelee()
        {
            var prio = new PrioritySelector(
                Movement.CreatePositionMobsInFront(),
                Safers.EnsureTarget(),
                CreatePetAttack(),
                Movement.CreateMoveToLosBehavior(),
                Movement.CreateFaceTargetBehavior( 180, false),
                new Decorator(
                    req => Me.GotTarget() && Me.CurrentTarget.Distance < SingularSettings.Instance.MeleeDismountRange,
                    CreateDismount( CompositeBuilder.CurrentBehaviorType.ToString())   // should be Pull or Combat 99% of the time
                    ),
                CreateAutoAttack()
                );

            if (CompositeBuilder.CurrentBehaviorType == BehaviorType.Pull)
            {
                prio.AddChild(
                    new PrioritySelector(
                        ctx => Me.GotTarget() && Me.CurrentTarget.IsAboveTheGround(),
                        new Decorator(
                            req => (bool)req,
                            new PrioritySelector(
                                Movement.CreateMoveToUnitBehavior(on => Me.CurrentTarget, 27, 22),
                                Movement.CreateEnsureMovementStoppedBehavior(22)
                                )
                            ),
                        new Decorator(
                            req => !(bool)req,
                            new PrioritySelector(
                                Movement.CreateMoveToMeleeBehavior(true),
                                Movement.CreateEnsureMovementStoppedWithinMelee()
                                )
                            )
                        )
                    );
            }
            else
            {
                prio.AddChild( Movement.CreateMoveToMeleeBehavior(true));
                prio.AddChild(Movement.CreateEnsureMovementStoppedWithinMelee());
            }

            return prio;
        }

        public static Composite EnsureReadyToAttackFromMediumRange( )
        {
            return new PrioritySelector(
                // Movement.CreatePositionMobsInFront(),

                Safers.EnsureTarget(),
                CreatePetAttack(),
                Movement.CreateMoveToLosBehavior(),
                Movement.CreateFaceTargetBehavior(),
                CreateDismount(CompositeBuilder.CurrentBehaviorType.ToString()),   // should be Pull or Combat 99% of the time
                Movement.CreateMoveToUnitBehavior(on => Me.CurrentTarget, 30, 25),
                CreateAutoAttack(),
                Movement.CreateEnsureMovementStoppedBehavior(25f)
                );
        }

        public static Composite EnsureReadyToAttackFromLongRange()
        {
            return new PrioritySelector(
                // Movement.CreatePositionMobsInFront(),

                Safers.EnsureTarget(),
                CreatePetAttack(),
                Movement.CreateMoveToLosBehavior(),
                Movement.CreateFaceTargetBehavior(),
                CreateDismount(CompositeBuilder.CurrentBehaviorType.ToString()),   // should be Pull or Combat 99% of the time
                Movement.CreateMoveToUnitBehavior(on => Me.CurrentTarget, 40, 36),
                CreateAutoAttack(),
                Movement.CreateEnsureMovementStoppedBehavior(36f)
                );
        }
    }
}
