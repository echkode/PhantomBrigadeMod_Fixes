// Copyright (c) 2024 EchKode
// SPDX-License-Identifier: BSD-3-Clause

using System.Collections.Generic;
using System.Reflection.Emit;

using HarmonyLib;

using PhantomBrigade.Data;

namespace EchKode.PBMods.DataMultiLinkerUnitCompositeFix
{
	[HarmonyPatch]
	public static class Patch
	{
		[HarmonyPatch(typeof(DataMultiLinkerUnitComposite), "ProcessRecursive")]
		[HarmonyTranspiler]
		static IEnumerable<CodeInstruction> Dmluc_ProcessRecursiveTranspiler(IEnumerable<CodeInstruction> instructions, ILGenerator generator)
		{
			// Merge director booting function lists from parents.

			var cm = new CodeMatcher(instructions, generator);
			var bootingFieldInfo = AccessTools.DeclaredField(typeof(DataBlockUnitCompositeDirector), nameof(DataBlockUnitCompositeDirector.booting));
			var bootingMatch = new CodeMatch(OpCodes.Stfld, bootingFieldInfo);
			var branchMatch = new CodeMatch(OpCodes.Brtrue_S);
			var loadRoot = new CodeInstruction(OpCodes.Ldarg_1);
			var loadCurrent = new CodeInstruction(OpCodes.Ldarg_0);
			var loadDirector = CodeInstruction.LoadField(typeof(DataContainerUnitComposite), nameof(DataContainerUnitComposite.director));
			var loadBooting = CodeInstruction.LoadField(typeof(DataBlockUnitCompositeDirector), nameof(DataBlockUnitCompositeDirector.booting));
			var mergeLists = CodeInstruction.Call(typeof(Patch), nameof(MergeFunctionLists));


			cm.MatchEndForward(bootingMatch)
				.Advance(1);
			var skipMerge = new CodeInstruction(OpCodes.Br_S, cm.Labels[0]);

			cm.InsertAndAdvance(skipMerge)
				.Insert(loadRoot.Clone())
				.CreateLabel(out var mergeLabel)
				.Advance(1)
				.InsertAndAdvance(loadDirector)
				.InsertAndAdvance(loadBooting)
				.InsertAndAdvance(loadCurrent)
				.InsertAndAdvance(loadDirector)
				.InsertAndAdvance(loadBooting)
				.InsertAndAdvance(mergeLists);

			cm.MatchStartBackwards(branchMatch)
				.SetOperandAndAdvance(mergeLabel);

			return cm.InstructionEnumeration();
		}

		public static void MergeFunctionLists(DataBlockUnitDirectorBooting processed, DataBlockUnitDirectorBooting current)
		{
			processed.functions.AddRange(current.functions);
			processed.functionsPerChild.AddRange(current.functionsPerChild);
		}
	}
}
