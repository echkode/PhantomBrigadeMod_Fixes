// Copyright (c) 2022 EchKode
// SPDX-License-Identifier: BSD-3-Clause

using HarmonyLib;
using UnityEngine;

namespace EchKode.PBMods.Fixes
{
	using PBModManager = PhantomBrigade.Mods.ModManager;

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
		}

		[HarmonyPatch(typeof(PhantomBrigade.Heartbeat), "Start")]
		[HarmonyPrefix]
		static void Hb_StartPrefix()
		{
			Heartbeat.Start();
		}
	}
}
