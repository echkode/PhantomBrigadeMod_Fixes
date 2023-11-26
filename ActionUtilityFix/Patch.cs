// Copyright (c) 2023 EchKode
// SPDX-License-Identifier: BSD-3-Clause

using System.Collections.Generic;
using System.Reflection.Emit;

using HarmonyLib;

using PhantomBrigade.Data;
using PBActionUtility = PhantomBrigade.ActionUtility;

using UnityEngine;

namespace EchKode.PBMods.ActionUtilityFix
{
	[HarmonyPatch]
	static class Patch
	{
		[HarmonyPatch(typeof(PBActionUtility), "GetScatterAngleAtTime")]
		[HarmonyPostfix]
		static void Au_GetScatterAngleAtTimePostfix(ref float __result)
		{
			__result = Mathf.Max(0f, __result);
		}

		[HarmonyPatch(typeof(PBActionUtility), "CreatePathAction")]
		[HarmonyTranspiler]
		static IEnumerable<CodeInstruction> Au_CreatePathActionTranspiler(IEnumerable<CodeInstruction> instructions, ILGenerator generator)
		{
			var cm = new CodeMatcher(instructions, generator);
			var lastActionTimeMethodInfo = AccessTools.DeclaredMethod(typeof(PBActionUtility), nameof(PBActionUtility.GetLastActionTime));
			var getSimMethodInfo = AccessTools.DeclaredPropertyGetter(typeof(DataShortcuts), nameof(DataShortcuts.sim));
			var minMethodInfo = AccessTools.DeclaredMethod(typeof(Mathf), nameof(Mathf.Min), new System.Type[] { typeof(float), typeof(float) });
			var lastActionTimeMatch = new CodeMatch(OpCodes.Call, lastActionTimeMethodInfo);
			var load1Match = new CodeMatch(OpCodes.Ldc_I4_1);
			var minMatch = new CodeMatch(OpCodes.Call, minMethodInfo);
			var getSim = new CodeInstruction(OpCodes.Call, getSimMethodInfo);
			var loadMaxTimePlacement = CodeInstruction.LoadField(typeof(DataContainerSettingsSimulation), nameof(DataContainerSettingsSimulation.maxActionTimePlacement));
			var add = new CodeInstruction(OpCodes.Add);
			var turnEnd = cm.MatchEndForward(lastActionTimeMatch)
				.MatchStartForward(load1Match)
				.Advance(-2)
				.InstructionsInRange(cm.Pos, cm.Pos + 5);

			cm.Advance(2)
				.RemoveInstruction()  // OpCodes.Ldc_I4_1
				.RemoveInstruction()  // OpCodes.Add
				.Advance(2)
				.InsertAndAdvance(getSim)
				.InsertAndAdvance(loadMaxTimePlacement)
				.Insert(add)
				.MatchStartForward(minMatch)
				.Advance(-3)
				.RemoveInstruction();  // OpCodes.Ldloc_S 11
			foreach (var ci in turnEnd)
			{
				cm.InsertAndAdvance(ci);
			}

			return cm.InstructionEnumeration();
		}
	}
}
