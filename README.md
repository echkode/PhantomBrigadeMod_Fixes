# Fixes

This is a collection of bug fixes and other corrections for [Phantom Brigade](https://braceyourselfgames.com/phantom-brigade/). This is not a mod in the traditional sense of an extension to the game that adds a feature or brings in new content. Instead, this is a collection of spot fixes to small bugs I've found in the code as I've been researching and developing other mods.

These fixes are for release version **1.2.0**.

Each fix is its own project so that you can compile and install just that fix separate from all the others.

List of fixes:

- [CIViewCombatScenarioStatusFix.Refresh](#civiewcombatscenariostatusfixrefresh)
- [DataContainerPartPreset.SortGenSteps](#datacontainerpartpresetsortgensteps)

## CIViewCombatScenarioStatusFix.Refresh

The turn display in the state entry for hostiles replanning in the mission status panel in combat shows a single segment and the wrong number of total turns when the state turn check is relative. This occurs with the generic_blitz_standard scenario. This has bugged me for a long time.

![Single segment in turn counter and wrong total turn count](https://github.com/echkode/PhantomBrigadeMod_Fixes/assets/48565771/7a4a78a2-6c06-40e1-beee-2f9b513b5bf3)

As it counts down, it finally becomes correct at the turn before the state ends.

![Total turn count finally correct on last turn before state change](https://github.com/echkode/PhantomBrigadeMod_Fixes/assets/48565771/9f35198d-bc54-416e-8584-a7802470a598)

With the fix, you get the right number of segments in the turn counter and the correct display of the turns remaining / turn total. The turn counter segments start to fill properly on subsequent turns.

![Total turn count correct on start of replanning state](https://github.com/echkode/PhantomBrigadeMod_Fixes/assets/48565771/bc133169-0ff6-4ce8-b6ba-e0071131b8ab)
![Segment filled on count down of replanning state](https://github.com/echkode/PhantomBrigadeMod_Fixes/assets/48565771/879b5ec7-5d43-41ec-890b-6da194944bc6)

The fill of the last segment sometimes spills into the border of the turn counter. This is caused by some rounding which makes the sum of the pixel counts of the individual counter segments not add up to the correct width.

![Last segment fill in black border on right of turn counter](https://github.com/echkode/PhantomBrigadeMod_Fixes/assets/48565771/e42b0a7c-8058-43d3-9102-412549914aef)

Here's how it looks after the fix.

![Black border on right side of turn counter](https://github.com/echkode/PhantomBrigadeMod_Fixes/assets/48565771/7c624955-f36f-4c94-8728-c962657a5080)

## DataContainerPartPreset.SortGenSteps

Remove nulls from GenSteps list after sorting. This also changes the sorting comparison function (`CompareGenStepsForSorting`) so that nulls sort to the end of the list. This way removing the nulls is a quick operation because it's just removing elements from the end of the list. That avoids the copying that's done when removing an element from the front.
