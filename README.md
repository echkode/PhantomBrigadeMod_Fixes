# Fixes

This is a collection of bug fixes and other corrections for [Phantom Brigade](https://braceyourselfgames.com/phantom-brigade/). This is not a mod in the traditional sense of an extension to the game that adds a feature or brings in new content. Instead, this is a collection of spot fixes to small bugs I've found in the code as I've been researching and developing other mods.

These fixes are for release version **1.1.3**.

Each fix is its own project so that you can compile and install just that fix separate from all the others.

List of fixes:

- [CIViewCombatScenarioStatusFix.Refresh](#civiewcombatscenariostatusfixrefresh)
- [DataContainerPartPreset.SortGenSteps](#datacontainerpartpresetsortgensteps)
- [DataMultiLinker.LoadData](#datamultilinkerloaddata)
- [DataMultiLinkerUnitComposite.ProcessRecursive](#datamultilinkerunitcompositeprocessrecursive)
- [ScenarioUtility.FreeOrDestroyCombatParticipants](#scenarioutilityfreeordestroycombatparticipants)

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

## DataMultiLinker.LoadData

This is a real simple fix: the `RefreshList()` call in `LoadData()` needs to be moved after the call to `ModManager.ProcessConfigModsForMultiLinker<T>()` but that's not easy to do with a patch for reasons.

Here's a more through explanation of the fault path.

Databases constructed from multiple config files use the `DataMultiLinker<>` generic class. The static method `LoadData()` will assemble the data from all the config files into a dictionary keyed by the name of the config file. The values of the dictionary are instances of a subclass of `DataContainer` which has a field named `key`. Sometimes specific instances will be accessed through the dictionary by key. Other times, code wants to iterate through all the instances in the database and that's done by calling the `GetDataList()` method. This method is backed by a separate list that's built in `LoadData()` through a call to `RefreshList()`.

The `key` field on the `DataContainer` instances in a database is assigned in a step after the config files have been read and instances created from them. `LoadData()` will iterate through the loaded instances and call `OnAfterDeserialization()` on them with the key of the instance as the argument to that method. The `DataContainer` base class method will assign the key argument to the `key` field.

ConfigOverrides replace the instances of a DataContainer in a database with a completely new instance. The method to load ConfigOverrides is `ModManager.ProcessConfigModsForMultiLinker<>()` and it's called in `DataMultiLinker<>.LoadData()`. Unfortunately it's called after `RefreshList()` so the data list has instances that won't be assigned keys in the OnAfterDeserialization step. When a function like `DataHelperAction.GetAvailableActions()` gets the list of actions from the action database, it includes these actions with a null `key` field instead of the ConfigOverrides replacements. This results in a NullReferenceException out of `DataHelperAction.IsAvailable()` which isn't expecting the `key` field on an action to be null.

The Modified Combat Actions mod from Nexus uses ConfigOverrides to change some actions. It will trigger an endless stream of this exception in the player.log file:
```
NullReferenceException: Object reference not set to an instance of an object
  at PhantomBrigade.Data.DataHelperAction.IsAvailable (PhantomBrigade.Data.DataContainerAction data, PersistentEntity unitPersistent, System.Boolean skipCombatChecks, System.Collections.Generic.HashSet`1[T] parts) [0x00110] in <003d31944a3449f787320483ebbd5a44>:0 
  at PhantomBrigade.Data.DataHelperAction.GetAvailableActions (CombatEntity unitCombat, System.Boolean unitStateValidation) [0x000b8] in <003d31944a3449f787320483ebbd5a44>:0 
  at PhantomBrigade.AI.Systems.CombatAIBehaviorInvokeSystem.CreateActionTables () [0x00054] in <003d31944a3449f787320483ebbd5a44>:0 
  at PhantomBrigade.AI.Systems.CombatAIBehaviorInvokeSystem.Execute () [0x000dc] in <003d31944a3449f787320483ebbd5a44>:0 
  at Entitas.Systems.Execute () [0x00010] in <ba8d092fe3cc43559f608b2222be040c>:0 
  at Entitas.Systems.Execute () [0x00010] in <ba8d092fe3cc43559f608b2222be040c>:0 
  at PhantomBrigade.GameController+GameControllerState.OnUpdate () [0x00015] in <003d31944a3449f787320483ebbd5a44>:0 
  at PhantomBrigade.GameController.OnUpdate () [0x00060] in <003d31944a3449f787320483ebbd5a44>:0 
  at PhantomBrigade.Heartbeat.Update () [0x00005] in <003d31944a3449f787320483ebbd5a44>:0 
```
This code can't patch `DataMultiLinker<>.LoadData()` directly because of limitations in patching static methods of generic classes. Instead, it patches `ModManager.ProcessConfigModsForMultiLinker<>()` to call `RefreshList()` after it is done loading ConfigOverrides. I added some instrumentation so you can see it doing its job in the player.log file.
```
Mod manager | Loading mod 10 (directory: DataMultiLinkerLoadDataFix, ID: com.echkode.pbmods.datamultilinkerloaddatafix) | Name: DataMultiLinkerLoadDataFix | Version: 1 | Description: Refresh list of actions after ConfigOverrides have been loaded
Loading mod .dll: DataMultiLinkerLoadDataFix.dll
Mod com.echkode.pbmods.datamultilinkerloaddatafix | Located ModLink of type ModLink, assigned metadata and singleton reference to it
Mod manager | Loading mod 11 (directory: Modified Combat Actions, ID: Modified Combat Actions) | Name: Modified Combat Actions | Version: 1.0 | Description: Created by Aerrus

<-- snip -->

Mod 11 (Modified Combat Actions) replaces config dash of type DataContainerAction
Mod 11 (Modified Combat Actions) replaces config shield of type DataContainerAction
Mod 11 (Modified Combat Actions) replaces config wait of type DataContainerAction
Mod 10 (com.echkode.pbmods.datamultilinkerloaddatafix) invoked RefreshList | multilinker type: DataContainerAction
Mod 10 (com.echkode.pbmods.datamultilinkerloaddatafix) OnAfterDeserialization | key: attack_primary | instance key: attack_primary
Mod 10 (com.echkode.pbmods.datamultilinkerloaddatafix) OnAfterDeserialization | key: attack_secondary | instance key: attack_secondary
Mod 10 (com.echkode.pbmods.datamultilinkerloaddatafix) OnAfterDeserialization | key: attack_system | instance key: attack_system
Mod 10 (com.echkode.pbmods.datamultilinkerloaddatafix) OnAfterDeserialization | key: attack_vehicle | instance key: attack_vehicle
Mod 10 (com.echkode.pbmods.datamultilinkerloaddatafix) OnAfterDeserialization | key: crash | instance key: crash
Mod 10 (com.echkode.pbmods.datamultilinkerloaddatafix) OnAfterDeserialization | key: dash | instance key: dash
Mod 10 (com.echkode.pbmods.datamultilinkerloaddatafix) OnAfterDeserialization | key: default | instance key: default
Mod 10 (com.echkode.pbmods.datamultilinkerloaddatafix) OnAfterDeserialization | key: eject | instance key: eject
Mod 10 (com.echkode.pbmods.datamultilinkerloaddatafix) OnAfterDeserialization | key: melee_fallback | instance key: melee_fallback
Mod 10 (com.echkode.pbmods.datamultilinkerloaddatafix) OnAfterDeserialization | key: melee_primary | instance key: melee_primary
Mod 10 (com.echkode.pbmods.datamultilinkerloaddatafix) OnAfterDeserialization | key: melee_secondary | instance key: melee_secondary
Mod 10 (com.echkode.pbmods.datamultilinkerloaddatafix) OnAfterDeserialization | key: move_limp | instance key: move_limp
Mod 10 (com.echkode.pbmods.datamultilinkerloaddatafix) OnAfterDeserialization | key: move_run | instance key: move_run
Mod 10 (com.echkode.pbmods.datamultilinkerloaddatafix) OnAfterDeserialization | key: move_system | instance key: move_system
Mod 10 (com.echkode.pbmods.datamultilinkerloaddatafix) OnAfterDeserialization | key: move_vehicle | instance key: move_vehicle
Mod 10 (com.echkode.pbmods.datamultilinkerloaddatafix) OnAfterDeserialization | key: reaction_attack_primary | instance key: reaction_attack_primary
Mod 10 (com.echkode.pbmods.datamultilinkerloaddatafix) OnAfterDeserialization | key: reaction_attack_secondary | instance key: reaction_attack_secondary
Mod 10 (com.echkode.pbmods.datamultilinkerloaddatafix) OnAfterDeserialization | key: reaction_attack_vehicle | instance key: reaction_attack_vehicle
Mod 10 (com.echkode.pbmods.datamultilinkerloaddatafix) OnAfterDeserialization | key: reaction_shield | instance key: reaction_shield
Mod 10 (com.echkode.pbmods.datamultilinkerloaddatafix) OnAfterDeserialization | key: reaction_system | instance key: reaction_system
Mod 10 (com.echkode.pbmods.datamultilinkerloaddatafix) OnAfterDeserialization | key: retreat | instance key: retreat
Mod 10 (com.echkode.pbmods.datamultilinkerloaddatafix) OnAfterDeserialization | key: retreat_airlift | instance key: retreat_airlift
Mod 10 (com.echkode.pbmods.datamultilinkerloaddatafix) OnAfterDeserialization | key: shield | instance key: shield
Mod 10 (com.echkode.pbmods.datamultilinkerloaddatafix) OnAfterDeserialization | key: system_equipment_core | instance key: system_equipment_core
Mod 10 (com.echkode.pbmods.datamultilinkerloaddatafix) OnAfterDeserialization | key: wait | instance key: wait
```

## DataMultiLinkerUnitComposite.ProcessRecursive

Composite units are a new addition with release 1.2.0. There's a new database, UnitComposite, that's used to construct these units. Like many other databases, UnitComposite can inherit properties from parent objects. For example, parent objects can add items the `nodes` field on the `DataBlockUnitCompositeDirector` class. However, that doesn't happen for the `booting` field on the same class. Instead, the code initializes the field from the first non-null value it sees. This means that parent objects cannot add `booting` functions to a composite unit. This patch merges all the `booting` functions in the inheritance hierarchy.

## ScenarioUtility.FreeOrDestroyCombatParticipants

There's an early return out of a loop over all the combat participants that should be a `continue`. It's unlikely this is common since it requires `PersistentEntity.isSalvageUnitFrame` to be `true` on a combat participant. The player has to change a difficulty setting to permit frame salvage and then salvage a frame to expose this execution path.
