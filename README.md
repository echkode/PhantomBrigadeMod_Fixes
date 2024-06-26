# Fixes

This is a collection of bug fixes and other corrections for [Phantom Brigade](https://braceyourselfgames.com/phantom-brigade/). This is not a mod in the traditional sense of an extension to the game that adds a feature or brings in new content. Instead, this is a collection of spot fixes to small bugs I've found in the code as I've been researching and developing other mods.

These fixes are for release version **1.3.3**.

Each fix is its own project so that you can compile and install just that fix separate from all the others.

List of fixes:

- [CIViewCombatScenarioStatusFix](#civiewcombatscenariostatusfix)
- [CombatDamageSystemFix](#combatdamagesystemfix)
- [CIViewCombatEndFix](#CIViewCombatEndFix)
- [CombatActionEventFix](#combatactioneventfix)
- [EquipmentUtilityFix](#equipmentutilityfix)
- [AIEjectFix](#aiejectfix)

## CIViewCombatScenarioStatusFix

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

## CombatDamageSystemFix

When a unit is destroyed with a pilot still in it, the pilot is marked as deceased but the health stats for the pilot are left unchanged. When the destroyed unit is hit with shots from weapons that have concussion damage, the concussion damage popup gets displayed even though the pilot is dead. This spot fix checks that the pilot isn't deceased or knocked out before entering the block to assess concussion damage.

## CIViewCombatEndFix

The code is bypassing Rewired and directly looking for the return key from the input system. This means it can't be reconfigured in the keybinding screen.

## CombatActionEventFix

Adds unit info to the line that's logged when a pilot ejects. This helps identify which unit had its pilot eject if you're trying to troubleshoot ejections.

## EquipmentUtilityFix

The wrong pilot is recorded in the log with a kill or takedown in `OnPartDestruction` which makes it hard to follow what happened in the battle.

## AIEjectFix

Enemy units who lose all their weapons won't eject. This is caused by an early exit in `CombatAIBehaviorInvokeSystem.CollapseEquipmentUse()` because the AI treats an eject action as an equipment use action but the eject combat action has a null `dataEquipment` section.
