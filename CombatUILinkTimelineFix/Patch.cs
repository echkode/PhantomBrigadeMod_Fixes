// Copyright (c) 2023 EchKode
// SPDX-License-Identifier: BSD-3-Clause

using System.Collections.Generic;
using System.Reflection.Emit;

using HarmonyLib;

using PBCombatUILinkTimeline = PhantomBrigade.Combat.Systems.CombatUILinkTimeline;

namespace EchKode.PBMods.CombatUILinkTimelineFix
{
	[HarmonyPatch]
	static class Patch
	{
		[HarmonyPatch(typeof(PBCombatUILinkTimeline), "Execute")]
		[HarmonyTranspiler]
		static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions, ILGenerator generator)
		{
			var cm = new CodeMatcher(instructions, generator);
			var hasDurationPropInfo = AccessTools.DeclaredPropertyGetter(typeof(ActionEntity), nameof(ActionEntity.hasDuration));
			var isExtrapolatedPropInfo = AccessTools.DeclaredPropertyGetter(typeof(ActionEntity), nameof(ActionEntity.isMovementExtrapolated));
			var hasDurationMatch = new CodeMatch(OpCodes.Callvirt, hasDurationPropInfo);
			var isExtrapolated = new CodeInstruction(OpCodes.Callvirt, isExtrapolatedPropInfo);

			cm.MatchStartForward(hasDurationMatch)
				.Advance(-1);
			var loadEntity = new CodeInstruction(cm.Instruction);
			cm.Advance(2);
			var branch = new CodeInstruction(OpCodes.Brtrue, cm.Operand);
			cm.Advance(1)
				.InsertAndAdvance(loadEntity)
				.InsertAndAdvance(isExtrapolated)
				.InsertAndAdvance(branch);


			return cm.InstructionEnumeration();
		}
	}
}
