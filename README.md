# Fixes

This is a collection of bug fixes and other corrections for [Phantom Brigade (Alpha)](https://braceyourselfgames.com/phantom-brigade/). This is not a mod in the traditional sense of an extension to the game that adds a feature or brings in new content and I do not recommend using it as a mod. Rather, it's more akin to a working journal of solutions to problems I've discovered while going through the disassembly of the game.

Current fixes:

- [CombatUtilities.GetHitDirection](#combatutilitiesgethitdirection)
- [ProjectileSplashDamageSystem](#projectilesplashdamagesystem)
- [ProjectileProximityFuseSystem](#projectileproximityfusesystem)
- [CaptureWithAlpha.GetProjectPath](#capturewithalphagetprojectpath)
- [ModManager.ProcessFieldEdit](#modmanagerprocessfieldedit)
- [CombatActionEvent.OnEjection](#combatactioneventonejection)
- [CombatCollisionSystem](#combatcollisionsystem)

Obsolete fixes:

- [CombatLandingSystem](#combatlandingsystem) (patched in PB release 0.22)


## CombatUtilities.GetHitDirection

Attacking from the back left of a target sometimes returns "right". Quick fix to change the return value to its proper value of "back".

## ProjectileSplashDamageSystem

Loop breaks instead of continuing on an unprimed projectile. Replace `break` with `continue` to process remaining projectiles.

## ProjectileProximityFuseSystem

Loop breaks instead of continues in a couple of places. Replace `break` with `continue` to process remaining projectiles.

## CaptureWithAlpha.GetProjectPath

Screenshot are being saved to game install folder instead of user documents folder. Ironically, there is a helper class that has the right folder so it's just a matter of overriding a single getter method to use that path instead.

## ModManager.ProcessFieldEdit

There are a number of bugs in how the ModManager process ConfigEdit files. The biggest change is to fix add/remove operations so they operate on the path terminal instead of the top-level collection at the beginning of the path and add a null value operation to null out values.

## CombatActionEvent.OnEjection

The pilot stat `pilot_auto_combat_takedowns` records the number of enemy mechs that have been destroyed. However, I think that the idea of a mech takedown should be expanded to include forcing an enemy pilot to eject without completely destroying the mech. I do this by assigning the takedown to the mech that fired the last projectile to strike the downed mech. I added a new Entitas entity and component to track this information. The actual tracking is done in [CombatCollisionSystem](#combatcollisionsystem) and the allocation of credit is done in this method.

## CombatCollisionSystem

There are two fixes to this system. The first fix applies a scaling factor to the damage a projectile does to environment objects. This matches what is done when a projectile hits a unit. This mostly affects railgun projectiles. The second fix is tracking the unit that fires each projectile so when a unit's pilot ejects, credit for downing the unit can be assigned to the attacking pilot.

## CombatLandingSystem

_Fixed in Phantom Brigade release 0.22_<br />
UI tabs don't always appear for the new units. A simple one-line fix to trigger a redraw of the UI tabs.
