# Fixes

This is a collection of bug fixes and other corrections for [Phantom Brigade](https://braceyourselfgames.com/phantom-brigade/). This is not a mod in the traditional sense of an extension to the game that adds a feature or brings in new content. Instead, this is a collection of spot fixes to small bugs I've found in the code as I've been researching and developing other mods.

These fixes are for release version **1.1.3**.

Each fix is its own project so that you can compile and install just that fix separate from all the others.

List of fixes:

- [ActionUtility.CreatePathAction](#actionutilitycreatepathaction)
- [ActionUtility.GetScatterAngleAtTime](#actionutilitygetscatterangleattime)
- [AddHardpointsFix.Run](#addhardpointsfixrun)
- [CIViewCombatTimeline.AdjustTimelineRegions](#civiewcombattimelineadjusttimelineregions)
- [CombatUtilities.ClampTimeInCurrentTurn](#combatutilitiesclamptimeincurrentturn)
- [DataContainerPartPreset.SortGenSteps](#datacontainerpartpresetsortgensteps)

## ActionUtility.CreatePathAction

Prevent placing actions after the max time placement in a turn. This constraint is necessary to avoid odd behavior and/or unexpected exits when dragging actions in the timeline.

## ActionUtility.GetScatterAngleAtTime

Prevent scatter angle from going negative by clamping lower bound to zero. Not every place using scatter angle at time is clamping the value and nowhere is it expecting the value to be negative.

## AddHardpointsFix.Run

Prevent adding null hardpoints to layout. There is a hard limit to the generated layout pool capacity and if it is exceeded, the pool will return null. Other parts of the code aren't expecting generated hardpoints to be null.

## CIViewCombatTimeline.AdjustTimelineRegions

The game can exit unexpectedly when dragging actions in the combat timeline. When an action is dragged, it may push around adjacent actions. This can create a cascade of actions being moved and is resolved with a recursive algorithm which would be immaterial except that there is a bug that allows a dash action to overlap with a run or wait action. Dragging an action when two others are overlapped can cause the recursive algorithm to overflow the stack and take down the game.

Here is the sequence of steps I've used to trigger the unexpected exit by exploiting the action overlap bug.

1. Set up a set of dash, run and attack actions as shown in the following screenshot.

![Two sets of dash, run, attack actions](https://github.com/echkode/PhantomBrigadeMod_Fixes/assets/48565771/2799b641-295a-4844-b676-a1b95d3be2c2)

2. Drag the second dash, run and attack actions so that the second dash overlaps the first run and creates enough space near the end of the turn to add a third dash action.

![Overlapping second dash with first run action](https://github.com/echkode/PhantomBrigadeMod_Fixes/assets/48565771/5d0439af-b2a9-4c08-be12-47f546175fe9)

3. Add a third dash action after the second run.

![Third dash action added](https://github.com/echkode/PhantomBrigadeMod_Fixes/assets/48565771/c551afd7-a6fe-460d-bbf9-8109b9f0ad68)

4. Move the first dash action toward the end of the turn. Keep moving it until the last dash action starts getting pushed into the next turn.

At this point the game will pause for a few seconds and then abruptly exit.

The recursive algorithm relies on two constraints that should always hold.

The first constraint is that the start time of an action created in the turn should not be less than the turn start time nor more than the turn start + 4.5s. Actions created in a turn should start in that turn.

The second constraint is that no actions on the same track can overlap. There are two tracks for actions in the timeline. The lower track is for primary actions like run and wait. The upper track is for attack, eject and retreat. Dash is a special action because it spans both tracks. This constraint means that attack actions can't overlap with each other but they can occur simultaneously with run actions. Since dash occupies both tracks, no other action can be executed when dashing.

The overlap bug breaks the second constraint and so when it occurs, there's no guarantee that the recursive algorithm will exit. In fact, if you look at `Player.log` after the game exits unexpectedly, you will see a number of lines at the end of the file similar to the following example:
```
Failed to complete timeline region adjustment, bailing at depth 451 | Action ID selected: 34 | Action start time: 5 | Offset selected: 0.6431942 | Offset correction allowed: True
```
The `depth` value is an argument that's incremented with each call into `AdjustTimelineRegions()`.

There is another bug in action placement that  breaks the first constraint. Normally the timeline prevents trying to place an action that starts after turn start + 4.5s. However, if there is a dash action that extends slightly past turn start + 4.5s, it is possible to place a run action after it. During the placing of the run action, it will appear to overlap the dash action. Once the run action is placed, the action is moved slightly so that it properly abuts the end of the dash action. However, this causes the start time of the action to be outside the constraint window.

This fix builds on the insight that the two constraints lets you calculate the maximum displacement of the selected action. This puts bounds on how far it can be dragged and guarantees that when it is dragged to the edge of those bounds, the actions between it and the end of the timeline that it's being dragged toward are packed as tightly as possible without any overlap. This can all be computed up front with a few simple loops in a non-recursive procedure. It also has some special logic to detect when the placement algorithm has added an action beyond turn start + 4.5s.

## CombatUtilities.ClampTimeInCurrentTurn

Use the turn length as set by the simulation configuration file rather than a hard-coded constant when calculating the end time of the turn. There are a few places scattered throughout the code that use a hard-coded turn length of 5 seconds instead of reading the value from the simulation configuration. This is one of them.

## DataContainerPartPreset.SortGenSteps

Remove nulls from GenSteps list after sorting. This also changes the sorting comparison function (`CompareGenStepsForSorting`) so that nulls sort to the end of the list. This way removing the nulls is a quick operation because it's just removing elements from the end of the list. That avoids the copying that's done when removing an element from the front.
