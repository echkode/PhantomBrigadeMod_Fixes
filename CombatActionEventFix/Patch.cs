// Copyright (c) 2024 EchKode
// SPDX-License-Identifier: BSD-3-Clause

using System.Collections.Generic;
using System.Reflection.Emit;

using HarmonyLib;

using PhantomBrigade;
using PhantomBrigade.Combat;

namespace EchKode.PBMods.CombatActionEventFix
{
	[HarmonyPatch]
	static class Patch
	{
		[HarmonyPatch(typeof(CombatActionEvent), nameof(CombatActionEvent.OnEjection))]
		[HarmonyTranspiler]
		static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions, ILGenerator generator)
		{
			// Add unit info to log message at beginning of OnEjection.

			var cm = new CodeMatcher(instructions, generator);
			var toLogMethodInfo = AccessTools.DeclaredMethod(typeof(IDUtility), nameof(IDUtility.ToLog), new System.Type[]
			{
				typeof(PersistentEntity),
			});
			var concatMethodInfo = AccessTools.DeclaredMethod(typeof(string), nameof(string.Concat), new System.Type[]
			{
				typeof(string),
				typeof(string),
				typeof(string),
			});
			var barSpace = new CodeInstruction(OpCodes.Ldstr, " | unit: ");
			var toLog = new CodeInstruction(OpCodes.Call, toLogMethodInfo);
			var concat = new CodeInstruction(OpCodes.Call, concatMethodInfo);

			cm.Start();
			var logInstructions = cm.Instructions(2);

			cm.RemoveInstructions(2)
				.Advance(3);
			var loadUnitPersistent = cm.Instruction.Clone();

			cm.InsertAndAdvance(logInstructions)
				.Advance(-1)
				.InsertAndAdvance(barSpace)
				.InsertAndAdvance(loadUnitPersistent)
				.InsertAndAdvance(toLog)
				.InsertAndAdvance(concat);

			return cm.InstructionEnumeration();
		}
	}
}
