// Copyright (c) 2024 EchKode
// SPDX-License-Identifier: BSD-3-Clause

using System.Collections.Generic;
using System.Reflection.Emit;

using HarmonyLib;

using PhantomBrigade;
using PhantomBrigade.Combat.Systems;

namespace EchKode.PBMods.CombatDamageSystemFix
{
	[HarmonyPatch]
	static class Patch
	{
		[HarmonyPatch(typeof(CombatDamageSystem), "Execute", new System.Type[] { typeof(List<CombatEntity>) })]
		[HarmonyTranspiler]
		static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions, ILGenerator generator)
		{
			// Add checks on pilot (isDeceased, isKnockedOut) before entering block to assess concussion damage.

			var cm = new CodeMatcher(instructions, generator);
			var getLinkedPilotMethodInfo = AccessTools.DeclaredMethod(typeof(IDUtility), nameof(IDUtility.GetLinkedPilot));
			var getIsDeceasedMethodInfo = AccessTools.DeclaredPropertyGetter(typeof(PersistentEntity), nameof(PersistentEntity.isDeceased));
			var getIsKnockedOutMethodInfo = AccessTools.DeclaredPropertyGetter(typeof(PersistentEntity), nameof(PersistentEntity.isKnockedOut));
			var getLinkedPilotMatch = new CodeMatch(OpCodes.Call, getLinkedPilotMethodInfo);
			var branchMatch = new CodeMatch(OpCodes.Brfalse);
			var isDeceased = new CodeInstruction(OpCodes.Callvirt, getIsDeceasedMethodInfo);
			var isKnockedOut = new CodeInstruction(OpCodes.Callvirt, getIsKnockedOutMethodInfo);

			cm.MatchEndForward(getLinkedPilotMatch)
				.MatchStartForward(branchMatch);
			var skipBlock = new CodeInstruction(OpCodes.Brtrue_S, cm.Operand);

			cm.Advance(1);
			var loadPilot = cm.Instruction.Clone();

			cm.InsertAndAdvance(loadPilot)
				.InsertAndAdvance(isDeceased)
				.InsertAndAdvance(skipBlock)
				.InsertAndAdvance(loadPilot)
				.InsertAndAdvance(isKnockedOut)
				.InsertAndAdvance(skipBlock);

			return cm.InstructionEnumeration();
		}
	}
}
