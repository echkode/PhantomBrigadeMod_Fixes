# Fixes

This is a collection of bug fixes and other corrections for [Phantom Brigade](https://braceyourselfgames.com/phantom-brigade/). This is not a mod in the traditional sense of an extension to the game that adds a feature or brings in new content. Instead, this is a collection of spot fixes to small bugs I've found in the code as I've been researching and developing other mods.

These fixes are for release version **1.1.3**.

Each fix is its own project so that you can compile and install just that fix separate from all the others.

List of fixes:

- [ActionUtility.CreatePathAction](#actionutilitycreatepathaction)
- [ActionUtility.GetScatterAngleAtTime](#actionutilitygetscatterangleattime)
- [CIViewCombatScenarioStatusFix.Refresh](#civiewcombatscenariostatusfixrefresh)
- [CIViewCombatTimeline.AdjustTimelineRegions](#civiewcombattimelineadjusttimelineregions)
- [CombatExecutionEndLateSystem.Execute](#combatexecutionendlatesystemexecute)
- [CombatUILinkTimeline.Execute](#combatuilinktimelineexecute)
- [CombatUtilities.ClampTimeInCurrentTurn](#combatutilitiesclamptimeincurrentturn)
- [DataContainerPartPreset.SortGenSteps](#datacontainerpartpresetsortgensteps)
- [InputCombatMeleeUtility.AttemptTargeting](#inputcombatmeleeutilityattempttargeting)
- [InputCombatWaitDrawingUtility.AttemptFinish](#inputcombatwaitdrawingutilityattemptfinish)
- [PathUtility.TrimPastMovement](#pathutilitytrimpastmovement)
- [ScenarioUtility.FreeOrDestroyCombatParticipants](#scenarioutilityfreeordestroycombatparticipants)

## ActionUtility.CreatePathAction

Prevent placing actions after the max time placement in a turn. This constraint is necessary to avoid odd behavior and/or unexpected exits when dragging actions in the timeline.

## ActionUtility.GetScatterAngleAtTime

Prevent scatter angle from going negative by clamping lower bound to zero. Not every place using scatter angle at time is clamping the value and nowhere is it expecting the value to be negative.

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

## CombatExecutionEndLateSystem.Execute

Long wait actions that go into the next turn are split on the turn boundary when the planning phase for the next turn begins. This matches what happens with run actions. Wait actions in the new turn that are less than 0.5s in duration are kept as continuations of the wait action from the previous turn to avoid creating runt waits. This constant was found in `InputCombatWaitDrawingUtility.AttemptFinish()` where it is used to prevent creating wait actions with durations less than that time.

## CombatUILinkTimeline.Execute

A small run action is created at the end of the turn if the last run action for a unit ends near the end of the turn. This autogenerated action is marked with `isMovementExtrapolated` and there are a number of guards which check that component and avoid doing any work if it is present on the action entity. One of the methods that has this guard is `CIViewCombatTimeline.ConfigureActionPlanned()` and the guard prevents a timeline action from being created. This tells me these autogenerated actions should not appear in the timeline.

One other place that creates timeline actions is `CIViewCombatTimeline.OnPlannedActionChange()` which is called from `CombatUILinkTimeline.Execute()`. This fix adds the guard to the `Execute()` method to prevent it from calling into `OnPlannedActionChange()` for autogenerated run actions.

## CombatUtilities.ClampTimeInCurrentTurn

Use the turn length as set by the simulation configuration file rather than a hard-coded constant when calculating the end time of the turn. There are a few places scattered throughout the code that use a hard-coded turn length of 5 seconds instead of reading the value from the simulation configuration. This is one of them.

## DataContainerPartPreset.SortGenSteps

Remove nulls from GenSteps list after sorting. This also changes the sorting comparison function (`CompareGenStepsForSorting`) so that nulls sort to the end of the list. This way removing the nulls is a quick operation because it's just removing elements from the end of the list. That avoids the copying that's done when removing an element from the front.

## InputCombatMeleeUtility.AttemptTargeting

Melee actions can be placed after the max placement time in a turn and that can cause the game to exit unexpectedly if the player drags an action. Here's a short video demonstrating the melee action being placed far enough into the next turn that only a sliver of it is visible in the timeline.

<video controls src="https://github.com/echkode/PhantomBrigadeMod_Fixes/assets/48565771/0eedd238-75d6-491f-98da-a5f712cbf1a5">
  <p>melee action placed in next turn with only a sliver visible in timeline</p>
</video>

Melee actions are double-track actions and there's an algorithm that tries to place them on the timeline without overlapping existing actions in either track. The algorithm first finds the end time of the last action on the primary track (run/wait) and then scans forward from that point on the secondary track (attack/shield). If it finds that the melee action overlaps an action on the secondary track, it will jump ahead to the end time of the overlapped action and continue its scan. Here's a screenshot of the algorithm working correctly with a couple of gapped attack actions.

![Melee action placed after last attack action](https://github.com/echkode/PhantomBrigadeMod_Fixes/assets/48565771/e6fc98f0-fdcf-4d98-8431-d59435470ad6)

This algorithm appears in several other places that deal with double-track actions and only in one case does it correctly check that the action is not placed after the max placement time. Here's the code for the algorithm as it appears in this method (remember, this has been translated from IL by a dissambler so the original code probably looks a bit different).
```
float num = ActionUtility.GetLastActionTime(selectedCombatEntity, true);
DataContainerAction entry = DataMultiLinker<DataContainerAction>.GetEntry(input.selectedAction.lookupKey);
float placementDuration = CombatUIUtility.GetPaintedTimePlacementDuration();
if (CombatUIUtility.IsIntervalOverlapped(selectedCombatEntity.id.id, entry, num + 1f / 1000f, placementDuration, out int _, skipSecondaryTrack: false))
{
    for (int index = 0; index < 4; ++index)
    {
        int actionIDIntersected;
        if (CombatUIUtility.IsIntervalOverlapped(selectedCombatEntity.id.id, entry, num + 1f / 1000f, placementDuration, out actionIDIntersected, skipSecondaryTrack: false))
        {
            ActionEntity actionEntity = IDUtility.GetActionEntity(actionIDIntersected);
            if (actionEntity != null)
            {
                num = actionEntity.startTime.f + actionEntity.duration.f;
                if (index == 3)
                {
                    num = ActionUtility.GetLastActionTime(selectedCombatEntity, false);
                    Contexts.sharedInstance.combat.ReplacePredictionTimeTarget(num);
                }
            }
        }
        else
        {
            Contexts.sharedInstance.combat.ReplacePredictionTimeTarget(num);
            break;
        }
    }
}
PaintedPath paintedPath = input.paintedPath;
```
What's interesting is the for loop which has a fixed iteration count. Normally it's a good practice in game code to iterate over known quantities so you can get a rough estimate about how much time your loops will take. However, this situation appears to be a small hack to avoid recursion. Each time you find an overlapped action, you have to change the start time (`num`) and try again.

The problem with this algorithm is its use of `CombatUIUtility.IsIntervalOverlapped()`. That function is used in a number of places to detect overlapping actions. However, it's intended to be a one-off check so it is not suitable to be used in a loop like this where you're walking forward through the timeline. A better way to write this algorithm would be to get the list of actions, sort them by start time and then walk the list checking for overlap.

To fix this bug I would normally add the check for max placement time right before the last line of the code snippet above and be done with it. That's what appears to have happened with dash actions in `InputCombatDashUtilityAttemptTargeting()`. Since this is the second time that the same fix has to be applied to the algorithm in a different area of the code, the proper fix is to put the algorithm into a function and replace all the places where it's used in the code with a call to the new function. I made the new function for the algorithm but I only patched the code in `InputCombatMeleeUtility.AttemptTargeting()` because I'm doing it in IL which is difficult enough to understand without a lot of extra noise. Here's what the patch looks like in C#:
```
float num = ActionUtility.GetLastActionTime(selectedCombatEntity, true);
DataContainerAction entry = DataMultiLinker<DataContainerAction>.GetEntry(input.selectedAction.lookupKey);
float placementDuration = CombatUIUtility.GetPaintedTimePlacementDuration();
(bool ok, float startTime) = CombatUIUtility.TryPlaceAction(selectedCombatEntity.id.id, entry, num + 1f / 1000f, placementDuration, CombatUIUtility.ActionOverlapCheck.SecondaryTrack);
if (!ok)
{
    return;
}
Contexts.sharedInstance.combat.ReplacePredictionTimeTarget(startTime);
PaintedPath paintedPath = input.paintedPath;
```
I folded the max placement time check into the new function (`CombatUIUtility.TryPlaceAction()`) to centralize this algorithm and prevent the bug from reoccurring if the algorithm is needed somewhere else in the future. It makes the intention at the call site clearer as well.

If I were working on the code base directly, I would also make this change in the following places:

- InputUILinkMeleePainting.Redraw()
- InputUILinkDashPainting.Execute()
- InputCombatDashUtility.AttemptTargeting() -- max placement checked correctly here

## InputCombatWaitDrawingUtility.AttemptFinish

Wait actions have the same issue as run actions in that they can be placed after the max time placement in a round. Similar to run actions, this has the potential to cause the game to exit unexpectedly if such a wait action is placed and prior actions are dragged.

Wait actions also have one more trick. If a wait action spans the turn boundary (that is, starts in one turn and finishes in the next), the action is split into two actions with the first action lasting up to the turn boundary and the second action starting on the turn boundary. This creates two problems, one of which is the same as above with an action being created after the max time placement. The second is that runt wait actions can be created with the same issues as runt run actions. This fix prevents splitting a wait action when it spans turns.

## PathUtility.TrimPastMovement

Prevent runt runs from being created. Runt runs are small run actions that appear at the start of a new turn when there is a prior run action that spills over into the new turn.

![Small runt run action at beginning of the turn](https://github.com/echkode/PhantomBrigadeMod_Fixes/assets/48565771/4b4ad7bd-b3ce-4892-9345-c50aba30031c)

There are several problems with these runts. When a runt is selected, the selection is shown offset from the action.

![Offset selection of a runt run](https://github.com/echkode/PhantomBrigadeMod_Fixes/assets/48565771/c7b10695-7d39-4525-8447-5e338f4f5f33)

Subsquent run actions overlap these runts.

![Overlap of run by a second run action](https://github.com/echkode/PhantomBrigadeMod_Fixes/assets/48565771/e7a80191-6dae-44bb-8128-8a6967c9dd44)

The overlap is hard to see because the runts are so small they don't contain the action name so here's the same sequence with the second run moved a bit so you can compare the two.

![Same sequence of runs moved so the overlapped image can be compared to this one](https://github.com/echkode/PhantomBrigadeMod_Fixes/assets/48565771/d94c68b0-dffb-4ff7-a47a-87456a3cda30)

The runts don't prevent other run actions from working correctly. That is, only the runt gets overlapped, the rest don't.

![Third run action doesn't overlap the second one](https://github.com/echkode/PhantomBrigadeMod_Fixes/assets/48565771/9e1c32c1-3a21-49a2-9e09-6fd6309d4ab3)

Selection is still wonky, however.

![Three selected actions with runt selection offset](https://github.com/echkode/PhantomBrigadeMod_Fixes/assets/48565771/d125084e-ed3a-4dbf-b370-bb7373b6fabf)

The fix looks at the duration of the run action in the new turn and leaves it as a continuation of the action from the previous turn if the duration is less than 0.25s. This constant was found in `ActionUtility.CreatePathAction()` which logs a warning and doesn't create the action when its duration is less than that constant.

## ScenarioUtility.FreeOrDestroyCombatParticipants

There's an early return out of a loop over all the combat participants that should be a `continue`. It's unlikely this is common since it requires `PersistentEntity.isSalvageUnitFrame` to be `true` on a combat participant. The player has to change a difficulty setting to permit frame salvage and then salvage a frame to expose this execution path.
