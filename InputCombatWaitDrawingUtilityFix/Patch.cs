// Copyright (c) 2023 EchKode
// SPDX-License-Identifier: BSD-3-Clause

using System.Collections.Generic;
using System.Reflection.Emit;

using HarmonyLib;

using PhantomBrigade.Data;
using PBInputCombatWaitDrawingUtility = PhantomBrigade.Combat.Systems.InputCombatWaitDrawingUtility;

namespace EchKode.PBMods.InputCombatWaitDrawingUtilityFix
{
	[HarmonyPatch]
	static class Patch
	{
		[HarmonyPatch(typeof(PBInputCombatWaitDrawingUtility), "AttemptFinish")]
		[HarmonyTranspiler]
		static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions, ILGenerator generator)
		{
			var cm = new CodeMatcher(instructions, generator);
			var getTurnLengthMethodInfo = AccessTools.DeclaredPropertyGetter(typeof(CombatContext), nameof(CombatContext.turnLength));
			var getSimMethodInfo = AccessTools.DeclaredPropertyGetter(typeof(DataShortcuts), nameof(DataShortcuts.sim));
			var getTurnLengthMatch = new CodeMatch(OpCodes.Callvirt, getTurnLengthMethodInfo);
			var load1Match = new CodeMatch(OpCodes.Ldc_I4_1);
			var branchMatch = new CodeMatch(OpCodes.Ble_Un_S);
			var convertToFloat = new CodeInstruction(OpCodes.Conv_R4);
			var getSim = new CodeInstruction(OpCodes.Call, getSimMethodInfo);
			var loadMaxTimePlacement = CodeInstruction.LoadField(typeof(DataContainerSettingsSimulation), nameof(DataContainerSettingsSimulation.maxActionTimePlacement));
			var mul = new CodeInstruction(OpCodes.Mul);
			var add = new CodeInstruction(OpCodes.Add);

			cm.MatchEndForward(getTurnLengthMatch)
				.MatchStartForward(load1Match)
				.Advance(-2);
			var loadTurnNumbers = new List<CodeInstruction>(cm.Instructions(2));
			cm.MatchStartForward(branchMatch)
				.Advance(-1)
				.RemoveInstruction()  // Ldloc_S turnEnd
				.InsertAndAdvance(loadTurnNumbers)
				.InsertAndAdvance(convertToFloat)
				.InsertAndAdvance(mul)
				.InsertAndAdvance(getSim)
				.InsertAndAdvance(loadMaxTimePlacement)
				.InsertAndAdvance(add);
			return cm.InstructionEnumeration();
		}
	}
}
