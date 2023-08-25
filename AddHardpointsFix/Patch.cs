// Copyright (c) 2023 EchKode
// SPDX-License-Identifier: BSD-3-Clause

using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;

using HarmonyLib;

using PhantomBrigade.Data;
using PhantomBrigade.Functions.Equipment;
using PBAddHardpoints = PhantomBrigade.Functions.Equipment.AddHardpoints;

namespace EchKode.PBMods.AddHardpointsFix
{
	[HarmonyPatch]
	static class Patch
	{
		private readonly static MethodInfo objectPoolGet = AccessTools.DeclaredMethod(typeof(ObjectPoolProvider<GeneratedHardpoint>), nameof(ObjectPoolProvider<GeneratedHardpoint>.Get));
		private readonly static MethodInfo dictionaryAdd = AccessTools.DeclaredMethod(typeof(Dictionary<string, GeneratedHardpoint>), nameof(Dictionary<string, GeneratedHardpoint>.Add));

		[HarmonyPatch(typeof(PBAddHardpoints), "Run", new[] {
			typeof(DataContainerPartPreset),
			typeof(Dictionary<string, GeneratedHardpoint>),
			typeof(int),
			typeof(string),
			typeof(bool),
		})]
		[HarmonyTranspiler]
		static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions, ILGenerator generator)
		{
			var cm = new CodeMatcher(instructions, generator);
			var retLabel = generator.DefineLabel();
			var getMatch = new CodeMatch(OpCodes.Callvirt, objectPoolGet);
			var brMatch = new CodeMatch(OpCodes.Brfalse);
			var addMatch = new CodeMatch(OpCodes.Callvirt, dictionaryAdd);

			cm.MatchEndForward(getMatch)
				.MatchStartForward(brMatch)
				.Operand = retLabel;
			cm.MatchStartForward(addMatch)
				.Advance(1)
				.Labels
				.Add(retLabel);

			return cm.InstructionEnumeration();
		}
	}
}
