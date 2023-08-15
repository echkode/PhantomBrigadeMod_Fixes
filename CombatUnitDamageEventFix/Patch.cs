// Copyright (c) 2023 EchKode
// SPDX-License-Identifier: BSD-3-Clause

using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;

using HarmonyLib;

using PBCombatUnitDamageEvent = PhantomBrigade.Functions.CombatUnitDamageEvent;

namespace EchKode.PBMods.CombatUnitDamageEventFix
{
	[HarmonyPatch]
	static class Patch
	{
		private static MethodInfo ce_AddInflictedConcussion = AccessTools.DeclaredMethod(typeof(CombatEntity), nameof(CombatEntity.AddInflictedConcussion));
		private static MethodInfo ce_AddInflictedHeat = AccessTools.DeclaredMethod(typeof(CombatEntity), nameof(CombatEntity.AddInflictedHeat));
		private static MethodInfo ce_AddInflictedStagger = AccessTools.DeclaredMethod(typeof(CombatEntity), nameof(CombatEntity.AddInflictedStagger));

		[HarmonyPatch(typeof(PBCombatUnitDamageEvent), "Run", new System.Type[] { typeof(PersistentEntity) })]
		[HarmonyTranspiler]
		static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
		{
			var callCount = 0;

			foreach (var instruction in instructions)
			{
				if (!instruction.Calls(ce_AddInflictedConcussion))
				{
					yield return instruction;
					continue;
				}

				callCount += 1;

				if (callCount == 1 || callCount > 3)
				{
					yield return instruction;
					continue;
				}

				if (callCount == 2)
				{
					yield return new CodeInstruction(OpCodes.Callvirt, ce_AddInflictedHeat);
					continue;
				}

				yield return new CodeInstruction(OpCodes.Callvirt, ce_AddInflictedStagger);
			}
		}
	}
}
