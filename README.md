# Fixes

This is a collection of bug fixes and other corrections for [Phantom Brigade](https://braceyourselfgames.com/phantom-brigade/). This is not a mod in the traditional sense of an extension to the game that adds a feature or brings in new content. Instead, this is a collection of spot fixes to small bugs I've found in the code as I've been researching and developing other mods.

These fixes are for release version **1.1.2**.

Each fix is its own project so that you can compile and install just that fix separate from all the others.

List of fixes:

- [ActionUtility.GetScatterAngleAtTime](#actionutilitygetscatterangleattime)
- [ActionUtility.OnMeleeImpact](#actionutilityonmeleeimpact)
- [AddHardpointsFix.Run](#addhardpointsfixrun)
- [CIViewOverworldEvent.FadeOutEnd](#civiewoverworldeventfadeoutend)
- [DataContainerPartPreset.SortGenSteps](#datacontainerpartpresetsortgensteps)

## ActionUtility.GetScatterAngleAtTime

Prevent scatter angle from going negative by clamping lower bound to zero. Not every place using scatter angle at time is clamping the value and nowhere is it expecting the value to be negative.

## ActionUtility.OnMeleeImpact

When collecting the damage stats for the weapon, the function uses the rounded property of `wpn_concussion` to round damage for `wpn_stagger`.

## AddHardpointsFix.Run

Prevent adding null hardpoints to layout. There is a hard limit to the generated layout pool capacity and if it is exceeded, the pool will return null. Other parts of the code aren't expecting generated hardpoints to be null.

## CIViewOverworldEvent.FadeOutEnd

The FadeOutEnd() method was setting FadeInEnd as an on-complete action which would reset some of the button states in the event dialog so that they would appear even if they shouldn't.

## DataContainerPartPreset.SortGenSteps

Remove nulls from GenSteps list after sorting. This also changes the sorting comparison function (`CompareGenStepsForSorting`) so that nulls sort to the end of the list. This way removing the nulls is a quick operation because it's just removing elements from the end of the list. That avoids the copying that's done when removing an element from the front.
