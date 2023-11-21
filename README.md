# Fixes

This is a collection of bug fixes and other corrections for [Phantom Brigade](https://braceyourselfgames.com/phantom-brigade/). This is not a mod in the traditional sense of an extension to the game that adds a feature or brings in new content. Instead, this is a collection of spot fixes to small bugs I've found in the code as I've been researching and developing other mods.

These fixes are for release version **1.1.3**.

Each fix is its own project so that you can compile and install just that fix separate from all the others.

List of fixes:

- [ActionUtility.GetScatterAngleAtTime](#actionutilitygetscatterangleattime)
- [AddHardpointsFix.Run](#addhardpointsfixrun)
- [CombatUtilities.ClampTimeInCurrentTurn](#combatutilitiesclamptimeincurrentturn)
- [DataContainerPartPreset.SortGenSteps](#datacontainerpartpresetsortgensteps)

## ActionUtility.GetScatterAngleAtTime

Prevent scatter angle from going negative by clamping lower bound to zero. Not every place using scatter angle at time is clamping the value and nowhere is it expecting the value to be negative.

## AddHardpointsFix.Run

Prevent adding null hardpoints to layout. There is a hard limit to the generated layout pool capacity and if it is exceeded, the pool will return null. Other parts of the code aren't expecting generated hardpoints to be null.

## CombatUtilities.ClampTimeInCurrentTurn

Use the turn length as set by the simulation configuration file rather than a hard-coded constant when calculating the end time of the turn. There are a few places scattered throughout the code that use a hard-coded turn length of 5 seconds instead of reading the value from the simulation configuration. This is one of them.

## DataContainerPartPreset.SortGenSteps

Remove nulls from GenSteps list after sorting. This also changes the sorting comparison function (`CompareGenStepsForSorting`) so that nulls sort to the end of the list. This way removing the nulls is a quick operation because it's just removing elements from the end of the list. That avoids the copying that's done when removing an element from the front.
