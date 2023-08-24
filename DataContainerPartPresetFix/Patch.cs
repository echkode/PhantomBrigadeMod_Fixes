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
		[HarmonyPatch(typeof(PBDataContainerPartPreset), "CompareGenStepsForSorting", new System.Type[] { typeof(IPartGenStep), typeof(IPartGenStep) })]
		[HarmonyTranspiler]
		static IEnumerable<CodeInstruction> CompareGenStepsForSorting_Transpiler(IEnumerable<CodeInstruction> instructions, ILGenerator generator)
		{
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
		[HarmonyPostfix]
		static void Dcpp_SortGenStepsPostfix(List<IPartGenStep> steps)
		{
			if (steps == null)
			{
				return;
			}
			while (steps.Count != 0 && steps[steps.Count - 1] == null)
			{
				steps.RemoveAt(steps.Count - 1);
			}
		}
	}
}
