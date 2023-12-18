// Copyright (c) 2023 EchKode
// SPDX-License-Identifier: BSD-3-Clause

using System.Collections.Generic;
using System.Reflection.Emit;

using HarmonyLib;

using PhantomBrigade;
using PBCombatUIUtility = PhantomBrigade.CombatUIUtility;
using PBInputCombatMeleeUtility = PhantomBrigade.Combat.Systems.InputCombatMeleeUtility;

namespace EchKode.PBMods.InputCombatMeleeUtilityFix
{
	using OkFloat = System.ValueTuple<bool, float>;

	[HarmonyPatch]
	static class Patch
	{
		[HarmonyPatch(typeof(PBInputCombatMeleeUtility), nameof(PBInputCombatMeleeUtility.AttemptTargeting))]
		[HarmonyTranspiler]
		static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions, ILGenerator generator)
		{
			// Check that the melee action can be placed before max placement time. If not, cancel placement.

			var cm = new CodeMatcher(instructions, generator);
			var okLocal = generator.DeclareLocal(typeof(bool));
			var getLastActionEndTimeMethodInfo = AccessTools.DeclaredMethod(typeof(ActionUtility), nameof(ActionUtility.GetLastActionTime));
			var getDurationMethodInfo = AccessTools.DeclaredMethod(typeof(PBCombatUIUtility), nameof(PBCombatUIUtility.GetPaintedTimePlacementDuration));
			var getLastActionEndTimeMatch = new CodeMatch(OpCodes.Call, getLastActionEndTimeMethodInfo);
			var getDurationMatch = new CodeMatch(OpCodes.Call, getDurationMethodInfo);
			var loadAddressMatch = new CodeMatch(OpCodes.Ldloca_S);
			var branchLessThanMatch = new CodeMatch(OpCodes.Blt);
			var branchMatch = new CodeMatch(OpCodes.Br_S);
			var loadConst2 = new CodeInstruction(OpCodes.Ldc_I4_2);
			var callTryPlaceAction = CodeInstruction.Call(typeof(CombatUIUtility), nameof(CombatUIUtility.TryPlaceAction));
			var dupe = new CodeInstruction(OpCodes.Dup);
			var loadOkField = CodeInstruction.LoadField(typeof(OkFloat), nameof(OkFloat.Item1));
			var loadStartTimeField = CodeInstruction.LoadField(typeof(OkFloat), nameof(OkFloat.Item2));
			var storeOk = new CodeInstruction(OpCodes.Stloc_S, okLocal);
			var loadOk = new CodeInstruction(OpCodes.Ldloc_S, okLocal);
			var ret = new CodeInstruction(OpCodes.Ret);

			cm.MatchEndForward(getLastActionEndTimeMatch)
				.Advance(1);
			var startTimeLocal = cm.Operand;
			var storeStartTime = new CodeInstruction(OpCodes.Stloc_S, startTimeLocal);

			cm.MatchEndForward(getDurationMatch)
				.MatchStartForward(loadAddressMatch);
			cm.SetInstructionAndAdvance(loadConst2)  // CombatUIUtility.ActionOverlapCheck.SecondaryTrack
				.RemoveInstructions(3)
				.InsertAndAdvance(callTryPlaceAction)
				.InsertAndAdvance(dupe)
				.InsertAndAdvance(loadOkField)
				.InsertAndAdvance(storeOk)
				.InsertAndAdvance(loadStartTimeField)
				.InsertAndAdvance(storeStartTime)
				.InsertAndAdvance(loadOk);

			var deleteStart = cm.Pos;
			cm.MatchStartForward(branchLessThanMatch)
				.MatchStartBackwards(branchMatch)
				.Advance(-1)
				.MatchStartBackwards(branchMatch);
			var offset = deleteStart - cm.Pos;
			cm.RemoveInstructionsWithOffsets(offset, 0)
				.Advance(offset);

			cm.CreateLabel(out var skipRetLabel);
			var skipRet = new CodeInstruction(OpCodes.Brtrue_S, skipRetLabel);
			cm.InsertAndAdvance(skipRet)
				.InsertAndAdvance(ret)
				.Advance(4);

			deleteStart = cm.Pos;
			cm.MatchStartForward(branchLessThanMatch);
			offset = deleteStart - cm.Pos;
			cm.RemoveInstructionsWithOffsets(offset, 0);

			return cm.InstructionEnumeration();
		}
	}
}
