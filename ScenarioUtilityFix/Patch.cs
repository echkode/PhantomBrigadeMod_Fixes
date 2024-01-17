// Copyright (c) 2024 EchKode
// SPDX-License-Identifier: BSD-3-Clause

using System.Collections.Generic;
using System.Reflection.Emit;

using HarmonyLib;

using PhantomBrigade;

namespace EchKode.PBMods.ScenarioUtilityFix
{
	[HarmonyPatch]
	static class Patch
	{
		[HarmonyPatch(typeof(ScenarioUtility), nameof(ScenarioUtility.FreeOrDestroyCombatParticipants))]
		[HarmonyTranspiler]
		static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions, ILGenerator generator)
		{
			// Continue instead of return so that all the combat participants are seen.

			var cm = new CodeMatcher(instructions, generator);
			var getIsSalvageFrameMethodInfo = AccessTools.DeclaredPropertyGetter(typeof(PersistentEntity), nameof(PersistentEntity.isSalvageUnitFrame));
			var setIsSalvageFrameMethodInfo = AccessTools.DeclaredPropertySetter(typeof(PersistentEntity), nameof(PersistentEntity.isSalvageUnitFrame));
			var branchMatch = new CodeMatch(OpCodes.Br);
			var getIsSalvageFrameMatch = new CodeMatch(OpCodes.Callvirt, getIsSalvageFrameMethodInfo);
			var setIsSalvageFrameMatch = new CodeMatch(OpCodes.Callvirt, setIsSalvageFrameMethodInfo);

			cm.MatchStartForward(branchMatch);
			var branchLoopEnd = cm.Instruction.Clone();

			cm.MatchEndForward(getIsSalvageFrameMatch)
				.MatchEndForward(setIsSalvageFrameMatch)
				.Advance(1)
				.SetInstructionAndAdvance(branchLoopEnd);

			return cm.InstructionEnumeration();
		}
	}
}
