# Fixes

This is a collection of bug fixes and other corrections for [Phantom Brigade](https://braceyourselfgames.com/phantom-brigade/). This is not a mod in the traditional sense of an extension to the game that adds a feature or brings in new content. Instead, this is a collection of spot fixes to small bugs I've found in the code as I've been researching and developing other mods.

These fixes are for release version **1.1.2**.

Each fix is its own project so that you can compile and install just that fix separate from all the others.

List of fixes:

- [CIViewOverworldEvent.FadeOutEnd](#civiewoverworldeventfadeoutend)
- [CombatUnitDamageEvent.Run](#combatunitdamageeventrun)
- [DataManagerSave.SaveAIData](#datamanagersavesaveaidata)
- [OverlapUtility.OnAreaOfEffectAgainstUnits](#overlaputilityonareaofeffectagainstunits)
- [ProjectileProximityFuseSystem](#projectileproximityfusesystem)

## CIViewOverworldEvent.FadeOutEnd

The FadeOutEnd() method was setting FadeInEnd as an on-complete action which would reset some of the button states in the event dialog so that they would appear even if they shouldn't.

## CombatUnitDamageEvent.Run

Inflicted heat and stagger damage was getting assigned to inflicted concussion damage. The fix puts the right amount in each of the damage categories.

## DataManagerSave.SaveAIData

Empty names were being serialized out for overworld entities which could cause trouble when the saved game was loaded again. Best to skip these nameless overworld entities.

## OverlapUtility.OnAreaOfEffectAgainstUnits

Inflicted heat and stagger damage was getting assigned to inflicted concussion damage. The fix puts the right amount in each of the damage categories.

## ProjectileProximityFuseSystem

Loop breaks instead of continues in a couple of places. Replace `break` with `continue` to process remaining projectiles.
