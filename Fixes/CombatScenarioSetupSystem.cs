using System.Collections.Generic;

using HarmonyLib;

using PhantomBrigade;
using PhantomBrigade.Data;
using PhantomBrigade.Game;

using UnityEngine;

namespace EchKode.PBMods.Fixes
{
	using PBCombatScenarioSetupSystem = PhantomBrigade.Combat.Systems.CombatScenarioSetupSystem;

	internal static class CombatScenarioSetupSystem
	{
		private delegate DataBlockAreaPoint GetAndRegisterSpawnPoint(
			DataBlockAreaSpawnGroup spawnGroup,
			int indexRequired = -1,
			bool log = false);
		private delegate DataBlockAreaSpawnGroup GetNextNearestSpawnGroup(
			DataContainerCombatArea area,
			DataBlockAreaSpawnGroup spawnGroupCurrent,
			CombatSetupData combatSetupData);
		private delegate void RefuelUnitBarrierCharges(
			List<PersistentEntity> units,
			PersistentEntity repairResourceProvider = null);

		private static bool initialized = false;

		private static GetAndRegisterSpawnPoint getAndRegisterSpawnPoint;
		private static GetNextNearestSpawnGroup getNextNearestSpawnGroup;
		private static RefuelUnitBarrierCharges refuelUnitBarrierCharges;

		private static void Initialize(PBCombatScenarioSetupSystem inst)
		{
			if (initialized)
			{
				return;
			}

			var mi = AccessTools.DeclaredMethod(typeof(PBCombatScenarioSetupSystem), "GetAndRegisterSpawnPoint");
			if (mi == null)
			{
				Debug.LogWarningFormat(
					"Mod {0} ({1}) Unable to initialize CombatScenarioSetupSystem | reflection failed to find method GetAndRegisterSpawnPoint",
					ModLink.modIndex,
					ModLink.modId);
				return;
			}
			getAndRegisterSpawnPoint = (GetAndRegisterSpawnPoint)mi.CreateDelegate(typeof(GetAndRegisterSpawnPoint), inst);

			mi = AccessTools.DeclaredMethod(typeof(PBCombatScenarioSetupSystem), "GetNextNearestSpawnGroup");
			if (mi == null)
			{
				Debug.LogWarningFormat(
					"Mod {0} ({1}) Unable to initialize CombatScenarioSetupSystem | reflection failed to find method GetNextNearestSpawnGroup",
					ModLink.modIndex,
					ModLink.modId);
				return;
			}
			getNextNearestSpawnGroup = (GetNextNearestSpawnGroup)mi.CreateDelegate(typeof(GetNextNearestSpawnGroup), inst);

			mi = AccessTools.DeclaredMethod(typeof(PBCombatScenarioSetupSystem), "RefuelUnitBarrierCharges");
			if (mi == null)
			{
				Debug.LogWarningFormat(
					"Mod {0} ({1}) Unable to initialize CombatScenarioSetupSystem | reflection failed to find method RefuelUnitBarrierCharges",
					ModLink.modIndex,
					ModLink.modId);
				return;
			}
			refuelUnitBarrierCharges = (RefuelUnitBarrierCharges)mi.CreateDelegate(typeof(RefuelUnitBarrierCharges), inst);

			initialized = true;
		}

		internal static bool DeployUnitsForPlayer(
			PBCombatScenarioSetupSystem inst,
			DataContainerScenario scenario,
			DataContainerCombatArea area,
			PersistentEntity unitHostPersistent)
		{
			if (!initialized)
			{
				Initialize(inst);
				if (!initialized)
				{
					return true;
				}
			}

			if (!scenario.squadUsed || IDUtility.playerBasePersistent == null)
			{
				Debug.LogWarningFormat("Skipping player unit spawning due to missing player base entity or scenario {0} not permitting deployment", scenario.key);
				return false;
			}

			var combatSetupData = Contexts.sharedInstance.persistent.combatSetupData;
			var spawnKeyPlayer = combatSetupData.spawnKeyPlayer;
			if (string.IsNullOrEmpty(spawnKeyPlayer) || !area.spawnGroups.ContainsKey(spawnKeyPlayer))
			{
				Debug.LogWarningFormat("Failed to deploy player squad using spawn group key {0}", spawnKeyPlayer);
				return false;
			}

			EquipmentUtility.RefreshUnitPartsFromRepair();

			var blockAreaSpawnGroup = area.spawnGroups[spawnKeyPlayer];
			var num = 0;
			var units = new List<PersistentEntity>();
			var flag = false;
			var slots = Contexts.sharedInstance.persistent.squadComposition.slots;
			var indexRequired = 0;
			for (var index = 0; index < slots.Count; index += 1)
			{
				if (blockAreaSpawnGroup == null)
				{
					Debug.LogWarningFormat("Skipping player unit {0} due to missing spawn group", index);
					continue;
				}

				var squadSlot = slots[index];
				if (string.IsNullOrEmpty(squadSlot.unitNameInternal))
				{
					continue;
				}

				var persistentEntity1 = IDUtility.GetPersistentEntity(squadSlot.unitNameInternal);
				if (persistentEntity1 == null
					|| persistentEntity1.isDestroyed
					|| !persistentEntity1.isUnitTag
					|| persistentEntity1.isWrecked)
				{
					Debug.LogWarningFormat(
						"Encountered invalid unit under name {0} at squad index {1} | {2}",
						squadSlot.unitNameInternal,
						index,
						persistentEntity1.ToLog());
					continue;
				}

				if (persistentEntity1.hasRepairStatus)
				{
					persistentEntity1.RemoveRepairStatus();
				}
				var persistentEntity2 = IDUtility.GetPersistentEntity(squadSlot.pilotNameInternal);
				if (persistentEntity2 == null
					|| persistentEntity2.isDestroyed
					|| !persistentEntity2.isPilotTag
					|| persistentEntity2.isDeceased)
				{
					Debug.LogWarningFormat(
						"Encountered invalid pilot under name {0} at squad index {1} | {2}",
						squadSlot.pilotNameInternal,
						index,
						persistentEntity2.ToLog());
					continue;
				}

				if (persistentEntity2.hasEntityLinkPersistentParent)
				{
					var persistentParent = IDUtility.GetPersistentParent(persistentEntity2);
					if (persistentParent != null && persistentParent.hasEntityLinkPilot)
					{
						persistentParent.RemoveEntityLinkPilot();
						Debug.LogFormat(
							"Removing link to pilot {0} from unit " + persistentParent.ToLog() + " to allow assignment to unit {1}",
							persistentEntity2.ToLog(),
							persistentEntity1.ToLog());
					}
				}
				persistentEntity1.ReplaceEntityLinkPilot(persistentEntity2.id.id);
				persistentEntity2.ReplaceEntityLinkPersistentParent(persistentEntity1.id.id);
				UnitUtilities.UpdatePilotConcussionInputs(persistentEntity2);
				var registerSpawnPoint = getAndRegisterSpawnPoint(blockAreaSpawnGroup, indexRequired);
				if (registerSpawnPoint == null)
				{
					indexRequired = -1;
					blockAreaSpawnGroup = getNextNearestSpawnGroup(area, blockAreaSpawnGroup, combatSetupData);
					if (blockAreaSpawnGroup == null)
					{
						continue;
					}
					registerSpawnPoint = getAndRegisterSpawnPoint(blockAreaSpawnGroup, indexRequired);
					if (registerSpawnPoint == null)
					{
						continue;
					}
				}
				Debug.LogFormat(
					"Friendly unit {0} is cleared to spawn at group {1} (squad slot {2}/{3})",
					persistentEntity1.ToLog(),
					blockAreaSpawnGroup.key,
					num + 1, 
					scenario.squadSize);
				var point = registerSpawnPoint.point;
				var spawnRotation = Quaternion.Euler(registerSpawnPoint.rotation);
				var combatUnit = UnitUtilities.CreateCombatUnit(persistentEntity1, point, spawnRotation, true, false, true);
				persistentEntity1.isUnitDeployed = true;
				UnitUtilities.RegisterUnitToCombat(persistentEntity1);
				units.Add(persistentEntity1);
				num += 1;
				// original: did not increment required index
				if (indexRequired != -1)
				{
					indexRequired += 1;
				}
				CIHelperOverlays.OnUnitEligibilityChange(persistentEntity1);
				if (!flag)
				{
					GameCameraSystem.MoveToLocation(combatUnit.position.v, true);
					GameCameraSystem.ForceSync();
					Contexts.sharedInstance.combat.ReplaceUnitSelected(combatUnit.id.id);
					flag = true;
				}
			}
			refuelUnitBarrierCharges(units, IDUtility.playerBasePersistent);
			Debug.LogFormat(
				"{0} player units fielded to spawn group {1}",
				num,
				blockAreaSpawnGroup.key);
			var linkedOverworldEntity = IDUtility.GetLinkedOverworldEntity(unitHostPersistent);
			var key1 = scenario.key;
			var key2 = area.key;
			AnalyticsWrapper.CombatStart(unitHostPersistent, linkedOverworldEntity, key1, key2);
			AnalyticsWrapper.CombatUnitDeployed(slots);

			return false;
		}
	}
}
