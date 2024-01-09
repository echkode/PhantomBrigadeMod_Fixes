// Copyright (c) 2024 EchKode
// SPDX-License-Identifier: BSD-3-Clause

using System.Collections.Generic;
using System.Reflection.Emit;

using HarmonyLib;

using PhantomBrigade.Combat.Components;
using PhantomBrigade.Data;

using UnityEngine;

namespace EchKode.PBMods.CIViewCombatScenarioStatusFix
{
	[HarmonyPatch]
	static class Patch
	{
		[HarmonyPatch(typeof(CIViewCombatScenarioStatus), nameof(CIViewCombatScenarioStatus.Refresh))]
		[HarmonyTranspiler]
		static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions, ILGenerator generator)
		{
			// Use the correct value for turn reference in a relative turn check.

			var cm = new CodeMatcher(instructions, generator);
			var entryTurnFieldInfo = AccessTools.DeclaredField(typeof(ScenarioStateScopeMetadata), nameof(ScenarioStateScopeMetadata.entryTurn));
			var turnFieldInfo = AccessTools.DeclaredField(typeof(DataBlockScenarioState), nameof(DataBlockScenarioState.turn));
			var maxMethodInfo = AccessTools.DeclaredMethod(
				typeof(Mathf),
				nameof(Mathf.Max),
				new System.Type[]
				{
					typeof(int),
					typeof(int),
				});
			var entryTurnMatch = new CodeMatch(OpCodes.Ldfld, entryTurnFieldInfo);
			var turnMatch = new CodeMatch(OpCodes.Ldfld, turnFieldInfo);
			var maxMatch = new CodeMatch(OpCodes.Call, maxMethodInfo);
			var addMatch = new CodeMatch(OpCodes.Add);
			var load1 = new CodeInstruction(OpCodes.Ldc_I4_1);
			var add = new CodeMatch(OpCodes.Add);
			var logMetadata = CodeInstruction.Call(typeof(CIViewCombatScenarioStatusFix), nameof(CIViewCombatScenarioStatusFix.LogStateScopeMetadata));

			cm.MatchStartForward(entryTurnMatch);
			var loadInstructions = cm.InstructionsWithOffsets(-2, -1);

			cm.MatchStartForward(turnMatch);
			var loadTurnField = cm.Instruction.Clone();
			cm.Advance(1);
			var loadRelativeField = cm.Instruction.Clone();
			cm.Advance(-2);
			var loadScenarioState = cm.Instruction.Clone();

			cm.MatchEndForward(maxMatch)
				.Advance(1);
			var storeTotal = cm.Instruction.Clone();
			var loadTotal = new CodeInstruction(OpCodes.Ldloc_S, cm.Operand);
			cm.Advance(-6);
			var labels = new List<Label>(cm.Labels);
			cm.Labels.Clear();
			var maxInstructions = cm.Instructions(7);

			cm.RemoveInstructions(7)
				.AddLabels(labels)
				.MatchEndBackwards(turnMatch)
				.Advance(3);
			var loadTurnValue = cm.Instruction.Clone();
			cm.CreateLabel(out var skipTotalRelativeLabel);
			cm.CreateLabel(out var storeTotalLabel);
			cm.Labels.Clear();
			var skipTotalRelative = new CodeMatch(OpCodes.Brfalse_S, skipTotalRelativeLabel);
			var jumpStoreTotal = new CodeMatch(OpCodes.Br_S, storeTotalLabel);

			cm.InsertAndAdvance(loadScenarioState)
				.InsertAndAdvance(loadTurnField)
				.InsertAndAdvance(loadRelativeField)
				.InsertAndAdvance(skipTotalRelative)
				.InsertAndAdvance(loadTurnValue)
				.InsertAndAdvance(jumpStoreTotal)
				.Insert(maxInstructions)
				.AddLabels(new[] { skipTotalRelativeLabel })
				.Advance(maxInstructions.Count - 1)
				.AddLabels(new[] { storeTotalLabel });

			cm.MatchEndForward(addMatch)
				.Advance(2)
				.InsertAndAdvance(load1)
				.InsertAndAdvance(loadTotal)
				.InsertAndAdvance(add)
				.InsertAndAdvance(storeTotal);

			cm.MatchStartBackwards(entryTurnMatch)
				.Advance(-1)
				.MatchStartBackwards(entryTurnMatch)
				.Insert(loadInstructions)
				.InsertAndAdvance(loadScenarioState)
				.InsertAndAdvance(logMetadata);

			return cm.InstructionEnumeration();
		}

		[HarmonyPatch(typeof(CIViewCombatScenarioStatus), nameof(CIViewCombatScenarioStatus.Refresh))]
		[HarmonyTranspiler]
		static IEnumerable<CodeInstruction> Transpiler2(IEnumerable<CodeInstruction> instructions, ILGenerator generator)
		{
			// Fix fill bar width to account for round-off.

			var cm = new CodeMatcher(instructions, generator);
			var widthLocal = generator.DeclareLocal(typeof(int));
			var roundToIntMethodInfo = AccessTools.DeclaredMethod(typeof(Mathf), nameof(Mathf.RoundToInt));
			var minMethodInfo = AccessTools.DeclaredMethod(
				typeof(Mathf),
				nameof(Mathf.Min),
				new System.Type[]
				{
					typeof(int),
					typeof(int),
				});
			var setWidthMethodInfo = AccessTools.DeclaredPropertySetter(typeof(UIWidget), nameof(UIWidget.width));
			var roundToIntMatch = new CodeMatch(OpCodes.Call, roundToIntMethodInfo);
			var minMatch = new CodeMatch(OpCodes.Call, minMethodInfo);
			var storeWidth = new CodeInstruction(OpCodes.Stloc, widthLocal);
			var loadWidth = new CodeInstruction(OpCodes.Ldloc, widthLocal);
			var load1 = new CodeInstruction(OpCodes.Ldc_I4_1);
			var mul = new CodeInstruction(OpCodes.Mul);
			var dupe = new CodeInstruction(OpCodes.Dup);
			var loadForegroundSprite = CodeInstruction.LoadField(typeof(CIHelperScenarioState), nameof(CIHelperScenarioState.spriteCollectionFillForeground));
			var setWidth = new CodeInstruction(OpCodes.Callvirt, setWidthMethodInfo);
			var load2 = new CodeInstruction(OpCodes.Ldc_I4_2);
			var add = new CodeInstruction(OpCodes.Add);
			var loadBackgroundSprite = CodeInstruction.LoadField(typeof(CIHelperScenarioState), nameof(CIHelperScenarioState.spriteFillBackground));

			cm.MatchEndForward(roundToIntMatch)
				.Advance(1)
				.MatchEndForward(roundToIntMatch)
				.MatchStartBackwards(minMatch)
				.Advance(-1);
			var loadTurnTotal = cm.Instruction.Clone();
			cm.Advance(3);
			var loadHelper = cm.Instruction.Clone();

			cm.MatchEndForward(roundToIntMatch)
				.Advance(2);
			var loadStepNumber = cm.Instruction.Clone();
			cm.CreateLabel(out var skipChangeWidthLabel);
			var skipChangeWidth = new CodeInstruction(OpCodes.Ble_S, skipChangeWidthLabel);

			cm.InsertAndAdvance(loadTurnTotal)
				.InsertAndAdvance(load1)
				.InsertAndAdvance(skipChangeWidth)
				.InsertAndAdvance(loadStepNumber)
				.InsertAndAdvance(loadTurnTotal)
				.InsertAndAdvance(mul)
				.InsertAndAdvance(storeWidth)
				.InsertAndAdvance(loadHelper)
				.InsertAndAdvance(dupe)
				.InsertAndAdvance(loadForegroundSprite)
				.InsertAndAdvance(loadWidth)
				.InsertAndAdvance(setWidth)
				.InsertAndAdvance(loadBackgroundSprite)
				.InsertAndAdvance(loadWidth)
				.InsertAndAdvance(load2)
				.InsertAndAdvance(add)
				.InsertAndAdvance(setWidth);

			return cm.InstructionEnumeration();
		}
	}
}
