// Copyright (c) 2024 EchKode
// SPDX-License-Identifier: BSD-3-Clause

using System.Collections.Generic;
using System.Reflection.Emit;

using HarmonyLib;

namespace EchKode.PBMods.CIViewCombatEndFix
{
	[HarmonyPatch]
	static class Patch
	{
		[HarmonyPatch(typeof(CIViewCombatEnd), "UpdateActive")]
		[HarmonyTranspiler]
		static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions, ILGenerator generator)
		{
			// Use Rewired input manager instead grabbing the return key directly.

			var cm = new CodeMatcher(instructions, generator);
			var loadConfirm = CodeInstruction.LoadField(typeof(InputAction), nameof(InputAction.Confirm));
			var loadNull = new CodeInstruction(OpCodes.Ldnull);
			var load1 = new CodeInstruction(OpCodes.Ldc_I4_1);
			var checkAndConsume = CodeInstruction.Call(typeof(InputHelper), nameof(InputHelper.CheckAndConsumeAction));

			cm.Start()
				.RemoveInstructions(2)
				.InsertAndAdvance(loadConfirm)
				.InsertAndAdvance(loadNull)
				.InsertAndAdvance(load1)
				.InsertAndAdvance(checkAndConsume);

			return cm.InstructionEnumeration();
		}
	}
}
