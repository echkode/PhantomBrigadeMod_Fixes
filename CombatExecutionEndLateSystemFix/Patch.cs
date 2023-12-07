// Copyright (c) 2023 EchKode
// SPDX-License-Identifier: BSD-3-Clause

using System.Collections.Generic;
using System.Reflection.Emit;

using HarmonyLib;

using PBCombatExecutionEndLateSystem = PhantomBrigade.Combat.Systems.CombatExecutionEndLateSystem;

namespace EchKode.PBMods.CombatExecutionEndLateSystemFix
{
	[HarmonyPatch]
	static class Patch
	{
		[HarmonyPatch(typeof(PBCombatExecutionEndLateSystem), "Execute")]
		[HarmonyTranspiler]
		static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions, ILGenerator generator)
		{
			// Set up call to split long wait actions that cross into new turn on turn start boundary.

			var cm = new CodeMatcher(instructions, generator);
			var getStartTimeMethodInfo = AccessTools.DeclaredPropertyGetter(typeof(ActionEntity), nameof(ActionEntity.startTime));
			var setDisposedMethodInfo = AccessTools.DeclaredPropertySetter(typeof(ActionEntity), nameof(ActionEntity.isDisposed));
			var getStartTimeMatch = new CodeMatch(OpCodes.Callvirt, getStartTimeMethodInfo);
			var setDisposedMatch = new CodeMatch(OpCodes.Callvirt, setDisposedMethodInfo);
			var convertMatch = new CodeMatch(OpCodes.Conv_R4);
			var addMatch = new CodeMatch(OpCodes.Add);
			var loadTurnStart = new CodeInstruction(OpCodes.Ldloc_2);
			var splitCall = CodeInstruction.Call(typeof(CombatExecutionEndLateSystemFix), nameof(CombatExecutionEndLateSystemFix.SplitWaitAction));

			cm.MatchStartForward(getStartTimeMatch)
				.Advance(-1);
			var actionEntityLocal = cm.Operand;
			var loadActionEntity = new CodeInstruction(OpCodes.Ldloc_S, actionEntityLocal);

			cm.MatchStartForward(addMatch)
				.Advance(1);
			var actionEndTimeLocal = cm.Operand;
			var loadActionEndTime = new CodeInstruction(OpCodes.Ldloc_S, actionEndTimeLocal);

			cm.MatchEndForward(setDisposedMatch)
				.MatchEndForward(addMatch)
				.Advance(2);
			var oldJumpTarget = new List<Label>(cm.Labels);

			cm.Labels.Clear();
			cm.CreateLabel(out var newJumpTarget);

			var branchIncrement = new CodeInstruction(OpCodes.Br_S, newJumpTarget);
			cm.InsertAndAdvance(branchIncrement)
				.Insert(loadActionEntity)
				.AddLabels(oldJumpTarget)
				.Advance(1)
				.InsertAndAdvance(loadActionEndTime)
				.InsertAndAdvance(loadTurnStart)
				.InsertAndAdvance(splitCall);

			return cm.InstructionEnumeration();
		}
	}
}
