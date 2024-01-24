// Copyright (c) 2023 EchKode
// SPDX-License-Identifier: BSD-3-Clause

using System.Collections.Generic;
using System.Reflection.Emit;

using HarmonyLib;

using PhantomBrigade.Functions.Equipment;
using PBDataContainerPartPreset = PhantomBrigade.Data.DataContainerPartPreset;

namespace EchKode.PBMods.DataContainerPartPresetFix
{
	[HarmonyPatch]
	static class Patch
	{
		[HarmonyPatch(typeof(PBDataContainerPartPreset), "CompareGenStepsForSorting", new System.Type[]
		{
			typeof(IPartGenStep),
			typeof(IPartGenStep)
		})]
		[HarmonyTranspiler]
		[System.Diagnostics.CodeAnalysis.SuppressMessage("CodeQuality", "IDE0051:Remove unused private members", Justification = "Used by the Harmony patch system")]
		static IEnumerable<CodeInstruction> Dcpp_CompareGenStepsForSortingTranspiler(IEnumerable<CodeInstruction> instructions, ILGenerator generator)
		{
			// Change the return values when step1 or step2 is null so that nulls sort to the end.
			// When step1 is null and step2 is non-null, the function should return 1 so that step1 is sorted after step2.
			// When step2 is null and step1 is non-null, the function should return -1 so that step1 is sorted before step2.

			var cm = new CodeMatcher(instructions, generator);
			var negOneMatch = new CodeMatch(OpCodes.Ldc_I4_M1);
			var oneMatch = new CodeMatch(OpCodes.Ldc_I4_1);

			cm.MatchEndForward(negOneMatch);
			cm.Opcode = OpCodes.Ldc_I4_1;
			cm.Advance(1).MatchEndForward(oneMatch);
			cm.Opcode = OpCodes.Ldc_I4_M1;

			return cm.InstructionEnumeration();
		}

		[HarmonyPatch(typeof(PBDataContainerPartPreset), "SortGenSteps", new System.Type[] { typeof(List<IPartGenStep>) })]
		[HarmonyTranspiler]
		[System.Diagnostics.CodeAnalysis.SuppressMessage("CodeQuality", "IDE0051:Remove unused private members", Justification = "Used by the Harmony patch system")]
		static IEnumerable<CodeInstruction> Dcpp_SortGenStepTranspiler(IEnumerable<CodeInstruction> instructions, ILGenerator generator)
		{
			// Exit the loop once we reach a non-null value. Since the list is sorted with nulls at the end, we don't
			// have to go any farther once we reach a non-null value.

			var cm = new CodeMatcher(instructions, generator);
			var branchMatch = new CodeMatch(OpCodes.Brtrue_S);

			cm.End();
			cm.CreateLabel(out var retLabel);

			cm.MatchStartBackwards(branchMatch)
				.SetOperandAndAdvance(retLabel);

			return cm.InstructionEnumeration();
		}
	}
}
