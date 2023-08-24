// Copyright (c) 2023 EchKode
// SPDX-License-Identifier: BSD-3-Clause

using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;

using HarmonyLib;

using PhantomBrigade.Functions.Equipment;
using PBDataContainerPartPreset = PhantomBrigade.Data.DataContainerPartPreset;

namespace EchKode.PBMods.DataContainerPartPresetFix
{
	[HarmonyPatch]
	static class Patch
	{
		[HarmonyPatch(typeof(PBDataContainerPartPreset), "CompareGenStepsForSorting", new System.Type[] { typeof(IPartGenStep), typeof(IPartGenStep) })]
		[HarmonyTranspiler]
		static IEnumerable<CodeInstruction> CompareGenStepsForSorting_Transpiler(IEnumerable<CodeInstruction> instructions, ILGenerator generator)
		{
			var cm = new CodeMatcher(instructions, generator);
			var negOneMatch = new CodeMatch(OpCodes.Ldc_I4_M1);
			var oneMatch = new CodeMatch(OpCodes.Ldc_I4_1);

			cm.MatchEndForward(negOneMatch);
			cm.Opcode = OpCodes.Ldc_I4_1;
			cm.Advance(1).MatchEndForward(oneMatch);
			cm.Opcode = OpCodes.Ldc_I4_M1;

			return cm.InstructionEnumeration();
		}

		static readonly MethodInfo listCountGetter = AccessTools.DeclaredPropertyGetter(typeof(List<IPartGenStep>), nameof(List<IPartGenStep>.Count));
		static readonly MethodInfo listRemoveAt = AccessTools.DeclaredMethod(typeof(List<IPartGenStep>), nameof(List<IPartGenStep>.RemoveAt));
		static readonly MethodInfo listItemGetter = AccessTools.DeclaredPropertyGetter(typeof(List<IPartGenStep>), "Item");

		[HarmonyPatch(typeof(PBDataContainerPartPreset), "SortGenSteps", new System.Type[] { typeof(List<IPartGenStep>) })]
		[HarmonyTranspiler]
		static IEnumerable<CodeInstruction> SortGenSteps_Transpiler(IEnumerable<CodeInstruction> instructions, ILGenerator generator)
		{
			var cm = new CodeMatcher(instructions, generator);
			var sortLabel = generator.DefineLabel();
			var loopStart = generator.DefineLabel();
			var loopCondition = generator.DefineLabel();
			var retLabel = generator.DefineLabel();
			var retMatch = new CodeMatch(OpCodes.Ret);
			var lastIndex = new[]
			{
				new CodeInstruction(OpCodes.Ldarg_1),
				new CodeInstruction(OpCodes.Ldarg_1),
				new CodeInstruction(OpCodes.Callvirt, listCountGetter),
				new CodeInstruction(OpCodes.Ldc_I4_1),
				new CodeInstruction(OpCodes.Sub),
			};

			// Patch up existing labels to keep things tidy.
			cm.MatchStartForward(new CodeMatch(OpCodes.Brfalse_S));
			cm.Operand = retLabel;
			cm.MatchStartForward(new CodeMatch(OpCodes.Bgt_S));
			cm.Operand = sortLabel;
			cm.MatchStartForward(retMatch);
			cm.Labels.Clear();
			cm.SetAndAdvance(OpCodes.Br, retLabel);
			cm.Labels.Clear();
			cm.Labels.Add(sortLabel);
			cm.MatchStartForward(retMatch);
			cm.Labels.Add(retLabel);

			cm.InsertAndAdvance(new CodeInstruction(OpCodes.Br_S, loopCondition));
			cm.Insert(lastIndex);
			// Clone the first instruction to avoid duplicating labels when lastIndex is used below.
			cm.SetInstruction(cm.Instruction.Clone());
			cm.Labels.Add(loopStart);
			cm.Advance(lastIndex.Length);
			cm.InsertAndAdvance(new CodeInstruction(OpCodes.Callvirt, listRemoveAt));

			var emptyCheck = new[]
			{
				new CodeInstruction(OpCodes.Ldarg_1),
				new CodeInstruction(OpCodes.Callvirt, listCountGetter),
				new CodeInstruction(OpCodes.Brfalse_S, retLabel),
			};
			cm.Insert(emptyCheck);
			cm.Labels.Add(loopCondition);
			cm.Advance(emptyCheck.Length);
			cm.InsertAndAdvance(lastIndex);
			cm.InsertAndAdvance(new CodeInstruction(OpCodes.Callvirt, listItemGetter));
			cm.Insert(new CodeInstruction(OpCodes.Brfalse_S, loopStart));

			return cm.InstructionEnumeration();
		}
	}
}
