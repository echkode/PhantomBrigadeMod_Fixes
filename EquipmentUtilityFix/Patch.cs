// Copyright (c) 2024 EchKode
// SPDX-License-Identifier: BSD-3-Clause

using System.Collections.Generic;
using System.Reflection.Emit;

using HarmonyLib;

using PhantomBrigade;

namespace EchKode.PBMods.EquipmentUtilityFix
{
	[HarmonyPatch]
	static class Patch
	{
		[HarmonyPatch(typeof(EquipmentUtility), nameof(EquipmentUtility.OnPartDestruction))]
		[HarmonyTranspiler]
		static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions, ILGenerator generator)
		{
			// Use the correct PersistentEntity for the responsible pilot when logging takedowns and kills.

			var cm = new CodeMatcher(instructions, generator);
			var getHasEventSourceEntityMethodInfo = AccessTools.DeclaredPropertyGetter(typeof(CombatEntity), nameof(CombatEntity.hasEventSourceEntity));
			var getLinkedPilotMethodInfo = AccessTools.DeclaredMethod(typeof(IDUtility), nameof(IDUtility.GetLinkedPilot));
			var toLogMethodInfo = AccessTools.DeclaredMethod(typeof(IDUtility), nameof(IDUtility.ToLog), new System.Type[]
			{
				typeof(PersistentEntity),
			});
			var getHasEventSourceEntityMatch = new CodeMatch(OpCodes.Callvirt, getHasEventSourceEntityMethodInfo);
			var getLinkedPilotMatch = new CodeMatch(OpCodes.Call, getLinkedPilotMethodInfo);
			var pilotStringMatch = new CodeMatch(OpCodes.Ldstr, "Pilot ");
			var toLogPilotMatch = new CodeMatch(OpCodes.Call, toLogMethodInfo);

			cm.MatchEndForward(getHasEventSourceEntityMatch)
				.MatchEndForward(getLinkedPilotMatch)
				.Advance(1);
			var pilotLoc = cm.Operand;

			cm.MatchEndForward(pilotStringMatch)
				.MatchStartForward(toLogPilotMatch)
				.Advance(-1)
				.SetOperandAndAdvance(pilotLoc)
				.MatchEndForward(pilotStringMatch)
				.Advance(1)
				.MatchEndForward(pilotStringMatch)
				.MatchStartForward(toLogPilotMatch)
				.Advance(-1)
				.SetOperandAndAdvance(pilotLoc)
				.MatchEndForward(pilotStringMatch)
				.MatchStartForward(toLogPilotMatch)
				.Advance(-1)
				.SetOperandAndAdvance(pilotLoc);

			return cm.InstructionEnumeration();
		}
	}
}
