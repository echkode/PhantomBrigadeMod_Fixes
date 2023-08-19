// Copyright (c) 2023 EchKode
// SPDX-License-Identifier: BSD-3-Clause

using System.Collections.Generic;

using HarmonyLib;

using PhantomBrigade.Data;
using PBActionUtility = PhantomBrigade.ActionUtility;

using UnityEngine;

namespace EchKode.PBMods.ActionUtilityFix
{
	[HarmonyPatch]
	static class Patch
	{
		[HarmonyPatch(typeof(PBActionUtility), "OnMeleeImpact", new System.Type[] {
			typeof(ActionEntity),
			typeof(CombatEntity),
			typeof(CombatEntity),
			typeof(Vector3),
			typeof(Vector3) })]
		[HarmonyTranspiler]
		static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
		{
			var loadStrCount = 0;

			foreach (var instruction in instructions)
			{
				if (loadStrCount == 0)
				{
					if (instruction.LoadsConstant(UnitStats.weaponStagger))
					{
						loadStrCount += 1;
					}
					yield return instruction;
					continue;
				}

				if (loadStrCount == 1)
				{
					if (instruction.LoadsConstant(UnitStats.weaponConcussion))
					{
						loadStrCount += 1;
						instruction.operand = UnitStats.weaponStagger;
					}
				}

				yield return instruction;
			}
		}
	}
}
