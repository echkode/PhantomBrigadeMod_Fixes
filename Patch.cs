// Copyright (c) 2022 EchKode
// SPDX-License-Identifier: BSD-3-Clause

using HarmonyLib;

using PhantomBrigade.AIOverworld.BT;
using PhantomBrigade.Data;
using PBCIViewCombatMode = CIViewCombatMode;
using PBCIViewOverworldEvent = CIViewOverworldEvent;
using PBBTAction_MoveToEntity = PhantomBrigade.AIOverworld.BT.Nodes.BTAction_MoveToEntity;
using PBDataManagerSave = PhantomBrigade.Data.DataManagerSave;
using PBModManager = PhantomBrigade.Mods.ModManager;

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

		[HarmonyPatch(typeof(PBModManager), "ProcessFieldEdit")]
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

		[HarmonyPatch(typeof(PhantomBrigade.Combat.CombatActionEvent), "OnEjection")]
		[HarmonyPrefix]
		static void Cae_OnEjectionPrefix(CombatEntity unitCombat, out CombatActionEvent.UnitPilotPair __state)
		{
			CombatActionEvent.OnEjectionPrologue(unitCombat, out __state);
		}

		[HarmonyPatch(typeof(PhantomBrigade.Combat.CombatActionEvent), "OnEjection")]
		[HarmonyPostfix]
		static void Cae_OnEjectionPostfix(CombatActionEvent.UnitPilotPair __state)
		{
			CombatActionEvent.OnEjectionEpilogue(__state);

			// Occasionally pilotless units are left in the tab order. I suspect it happens when an enemy pilot
			// ejects the turn after being damaged to the point that the AI will trigger an ejection.
			//CIViewCombatMode.ins.RedrawUnitTabs();
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

		[HarmonyPatch(typeof(PBBTAction_MoveToEntity), "OnUpdate")]
		[HarmonyPrefix]
		static bool Btamte_OnUpdatePrefix(ref BTStatus __result, PBBTAction_MoveToEntity __instance, OverworldEntity self)
		{
			__result = BTAction_MoveToEntity.OnUpdate(__instance, self);
			return false;
		}

		[HarmonyPatch(typeof(PBCIViewCombatMode), "RedrawUnitTabs")]
		[HarmonyPrefix]
		static bool Civcm_RedrawUnitTabsPrefix()
		{
			CIViewCombatMode.RedrawUnitTabs();
			return false;
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
