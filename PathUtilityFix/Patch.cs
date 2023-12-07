// Copyright (c) 2023 EchKode
// SPDX-License-Identifier: BSD-3-Clause

using System.Collections.Generic;
using System.Reflection.Emit;

using HarmonyLib;

using PBPathUtility = PhantomBrigade.PathUtility;

namespace EchKode.PBMods.PathUtilityFix
{
	[HarmonyPatch]
	static class Patch
	{
		[HarmonyPatch(typeof(PBPathUtility), "TrimPastMovement")]
		[HarmonyTranspiler]
		static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions, ILGenerator generator)
		{
			var cm = new CodeMatcher(instructions, generator);
			var durationPropInfo = AccessTools.DeclaredPropertyGetter(typeof(ActionEntity), nameof(ActionEntity.duration));
			var durationMatch = new CodeMatch(OpCodes.Callvirt, durationPropInfo);
			var addMatch = new CodeMatch(OpCodes.Add);
			var subtract = new CodeInstruction(OpCodes.Sub);

			cm.MatchEndForward(durationMatch)
				.MatchEndForward(addMatch)
				.Advance(2)
				.InsertAndAdvance(subtract)
				.RemoveInstructions(2)
				.SetOperandAndAdvance(0.25f)
				.SetOpcodeAndAdvance(OpCodes.Blt);

			return cm.InstructionEnumeration();
		}
	}
}
