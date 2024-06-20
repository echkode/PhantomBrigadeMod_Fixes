// Copyright (c) 2024 EchKode
// SPDX-License-Identifier: BSD-3-Clause

using System.Collections.Generic;
using System.Reflection.Emit;

using HarmonyLib;

using PhantomBrigade.AI;
using PhantomBrigade.AI.Systems;
using PhantomBrigade.Data;

using UnityEngine;

namespace EchKode.PBMods.AIEjectFix
{
	[HarmonyPatch]
	public static partial class Patch
	{
		[HarmonyPatch(typeof(CombatAIBehaviorInvokeSystem), "CollapseEquipmentUse")]
		[HarmonyTranspiler]
		static IEnumerable<CodeInstruction> Caibis_Transpiler(IEnumerable<CodeInstruction> instructions, ILGenerator generator)
		{
			// The combat AI treats eject as a use equipment action but the combat action does not have a dataEquipment section.
			// We have to move the null check so that it's not a condition for an early return but instead guards the call to
			// EquipmentUtility.GetPartInUnit() where the dataEquipment section is consulted for the socket.

			var cm = new CodeMatcher(instructions, generator);
			var isPlannedActionMethodInfo = AccessTools.DeclaredMethod(typeof(AIUtility), nameof(AIUtility.IsPlannedActionUIDValid));
			var getPartMethodInfo = AccessTools.DeclaredMethod(typeof(EquipmentUtility), nameof(EquipmentUtility.GetPartInUnit));
			var logErrorMethodInfo = AccessTools.DeclaredMethod(typeof(Debug), nameof(Debug.LogError), new System.Type[]
			{
				typeof(object),
			});
			var isPlannedActionMatch = new CodeMatch(OpCodes.Call, isPlannedActionMethodInfo);
			var getPartMatch = new CodeMatch(OpCodes.Call, getPartMethodInfo);
			var logErrorMatch = new CodeMatch(OpCodes.Call, logErrorMethodInfo);
			var loadDataEquipment = CodeInstruction.LoadField(typeof(DataContainerAction), nameof(DataContainerAction.dataEquipment));
			var loadNull = new CodeInstruction(OpCodes.Ldnull);

			cm.Start()
				.MatchStartForward(isPlannedActionMatch)
				.Advance(-7);
			var loadEntry = cm.Instruction.Clone();
			cm.Advance(-1)
				.RemoveInstructions(3)
				.MatchEndForward(getPartMatch)
				.Advance(1)
				.CreateLabel(out var storeLabel);
			var jumpToStore = new CodeInstruction(OpCodes.Br_S, storeLabel);
			cm.MatchEndBackwards(logErrorMatch)
				.Advance(1);
			var labels = new List<Label>(cm.Labels);
			cm.Labels.Clear();
			cm.CreateLabel(out var getPartLabel);
			var skipToGetPart = new CodeInstruction(OpCodes.Brtrue_S, getPartLabel);
			cm.Insert(loadEntry)
				.AddLabels(labels)
				.Advance(1)
				.InsertAndAdvance(loadDataEquipment)
				.InsertAndAdvance(skipToGetPart)
				.InsertAndAdvance(loadNull)
				.InsertAndAdvance(jumpToStore);

			return cm.InstructionEnumeration();
		}
	}
}
