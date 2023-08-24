# Fixes

This is a collection of bug fixes and other corrections for [Phantom Brigade](https://braceyourselfgames.com/phantom-brigade/). This is not a mod in the traditional sense of an extension to the game that adds a feature or brings in new content. Instead, this is a collection of spot fixes to small bugs I've found in the code as I've been researching and developing other mods.

These fixes are for release version **1.1.2**.

Each fix is its own project so that you can compile and install just that fix separate from all the others.

List of fixes:

- [ActionUtility.OnMeleeImpact](#actionutilityonmeleeimpact)
- [CIViewOverworldEvent.FadeOutEnd](#civiewoverworldeventfadeoutend)
- [CombatUnitDamageEvent.Run](#combatunitdamageeventrun)
- [DataContainerPartPreset.SortGenSteps](#datacontainerpartpresetsortgensteps)
- [DataManagerSave.SaveAIData](#datamanagersavesaveaidata)
- [OverlapUtility.OnAreaOfEffectAgainstUnits](#overlaputilityonareaofeffectagainstunits)
- [ProjectileProximityFuseSystem](#projectileproximityfusesystem)

## ActionUtility.OnMeleeImpact

When collecting the damage stats for the weapon, the function uses the rounded property of `wpn_concussion` to round damage for `wpn_stagger`.

## CIViewOverworldEvent.FadeOutEnd

The FadeOutEnd() method was setting FadeInEnd as an on-complete action which would reset some of the button states in the event dialog so that they would appear even if they shouldn't.

## CombatUnitDamageEvent.Run

Inflicted heat and stagger damage was getting assigned to inflicted concussion damage. The fix puts the right amount in each of the damage categories.

## DataContainerPartPreset.SortGenSteps

Remove nulls from GenSteps list after sorting. This also changes the sorting comparison function (`CompareGenStepsForSorting`) so that nulls sort to the end of the list. This way removing the nulls is a quick operation because it's just removing elements from the end of the list. That avoids the copying that's done when removing an element from the front.

## DataManagerSave.SaveAIData

Empty names were being serialized out for overworld entities which could cause trouble when the saved game was loaded again. Best to skip these nameless overworld entities.

## OverlapUtility.OnAreaOfEffectAgainstUnits

Inflicted heat and stagger damage was getting assigned to inflicted concussion damage. The fix puts the right amount in each of the damage categories.

## ProjectileProximityFuseSystem

Loop breaks instead of continues in a couple of places. Replace `break` with `continue` to process remaining projectiles.
