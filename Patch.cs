// Copyright (c) 2022 EchKode
// SPDX-License-Identifier: BSD-3-Clause

using HarmonyLib;

using PhantomBrigade.Data;
using PBCIViewCombatMode = CIViewCombatMode;
using PBCIViewOverworldEvent = CIViewOverworldEvent;
using PBCombatScenarioSetupSystem = PhantomBrigade.Combat.Systems.CombatScenarioSetupSystem;
using PBDataManagerSave = PhantomBrigade.Data.DataManagerSave;
using PBModUtilities = PhantomBrigade.Mods.ModUtilities;

using UnityEngine;

namespace EchKode.PBMods.Fixes
{
	[HarmonyPatch]
	static class Patch
	{
		[HarmonyPatch(typeof(PhantomBrigade.CombatUtilities), "GetHitDirection")]
		[HarmonyPrefix]
		static bool Cu_GetHitDirectionPrefix(Quaternion objectRotation, Vector3 incomingDirection, ref string __result)
		{
			__result = CombatUtilities.GetHitDirection(objectRotation, incomingDirection);
			return false;
		}

		[HarmonyPatch(typeof(CaptureWithAlpha), "GetProjectPath")]
		[HarmonyPostfix]
		static void Cwa_GetProjectPathPostfix(CaptureWithAlpha __instance, ref string __result)
		{
			if (!Screenshot.Initialized)
			{
				Screenshot.Initialize(__instance);
			}
			__result = Screenshot.GetProjectPath();
		}

		[HarmonyPatch(typeof(PBModUtilities), "ProcessFieldEdit", new System.Type[] { typeof(object), typeof(string), typeof(string), typeof(string), typeof(int), typeof(string), typeof(string) })]
		[HarmonyPrefix]
		static bool Mm_ProcessFieldEditPrefix(
			object target,
			string filename,
			string fieldPath,
			string valueRaw,
			int i,
			string modID,
			string dataTypeName)
		{
			var spec = new ModManager.EditSpec()
			{
				i = i,
				modID = modID,
				filename = ModManager.FindConfigKeyIfEmpty(target, dataTypeName, filename),
				dataTypeName = dataTypeName,
				root = target,
				fieldPath = fieldPath,
				valueRaw = valueRaw,
			};
			Debug.LogWarningFormat(
				"Mod {0} ({1}) applying edit to config {2} path {3}",
				spec.i,
				spec.modID,
				spec.filename,
				spec.fieldPath);
			ModManager.ProcessFieldEdit(spec);
			return false;
		}

		[HarmonyPatch(typeof(PBCIViewOverworldEvent), "FadeOutEnd")]
		[HarmonyPrefix]
		static bool Civoe_FadeOutEndPrefix()
		{
			CIViewOverworldEvent.FadeOutEnd();
			return false;
		}

		[HarmonyPatch(typeof(PBDataManagerSave), "SaveAIData")]
		[HarmonyPrefix]
		static bool Dms_SaveAIDataPrefix(
			OverworldEntity sourceEntity,
			DataContainerSavedOverworldEntity targetData)
		{
			DataManagerSave.SaveAIData(sourceEntity, targetData);
			return false;
		}

		[HarmonyPatch(typeof(PBCIViewCombatMode), "RedrawUnitTabs")]
		[HarmonyPrefix]
		static bool Civcm_RedrawUnitTabsPrefix()
		{
			CIViewCombatMode.RedrawUnitTabs();
			return false;
		}

		[HarmonyPatch(typeof(PBCombatScenarioSetupSystem), "DeployUnitsForPlayer")]
		[HarmonyPrefix]
		static bool Csss_DeployUnitsForPlayer(
			PBCombatScenarioSetupSystem __instance,
			DataContainerScenario scenario,
			DataContainerCombatArea area,
			PersistentEntity unitHostPersistent)
		{
			return CombatScenarioSetupSystem.DeployUnitsForPlayer(
				__instance,
				scenario,
				area,
				unitHostPersistent);
		}

		[HarmonyPatch(typeof(PhantomBrigade.Heartbeat), "Start")]
		[HarmonyPrefix]
		static void Hb_StartPrefix()
		{
			ModManager.LoadText();
			Heartbeat.Start();
		}
	}
}
