// Copyright (c) 2023 EchKode
// SPDX-License-Identifier: BSD-3-Clause

using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;

using HarmonyLib;

using PhantomBrigade.Data;
using PBIDUtility = PhantomBrigade.IDUtility;
using PBDataManagerSave = PhantomBrigade.Data.DataManagerSave;

namespace EchKode.PBMods.DataManagerSaveFix
{
	[HarmonyPatch]
	static class Patch
	{
		private static MethodInfo oe_getAgentEntityBlackboard = AccessTools.DeclaredPropertyGetter(typeof(OverworldEntity), nameof(OverworldEntity.agentEntityBlackboard));
		private static MethodInfo idu_GetOverworldEntity = AccessTools.DeclaredMethod(typeof(PBIDUtility), nameof(PBIDUtility.GetOverworldEntity), new System.Type[] { typeof(int) });

		[HarmonyPatch(typeof(PBDataManagerSave), "SaveAIData", new System.Type[] { typeof(OverworldEntity), typeof(DataContainerSavedOverworldEntity) })]
		[HarmonyTranspiler]
		static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
		{
			var foundForEachLoop = false;
			var foundLoopEndLabel = false;
			var foundCall = false;
			var branchCount = 0;
			Label? loopEnd = null;

			foreach (var instruction in instructions)
			{
				if (branchCount >= 2)
				{
					yield return instruction;
					continue;
				}

				if (instruction.Calls(oe_getAgentEntityBlackboard))
				{
					foundForEachLoop = true;
					yield return instruction;
					continue;
				}

				if (!foundForEachLoop)
				{
					yield return instruction;
					continue;
				}

				if (!foundLoopEndLabel)
				{
					foundLoopEndLabel = instruction.Branches(out loopEnd);
					yield return instruction;
					continue;
				}

				if (instruction.Calls(idu_GetOverworldEntity))
				{
					foundCall = true;
					yield return instruction;
					continue;
				}

				if (!foundCall)
				{
					yield return instruction;
					continue;
				}

				if (instruction.Branches(out var _))
				{
					if (branchCount == 0)
					{
						instruction.operand = loopEnd;
						branchCount += 1;
					}
					else if (branchCount == 1)
					{
						instruction.opcode = OpCodes.Brfalse_S;
						instruction.operand = loopEnd;
						branchCount += 1;
					}
				}

				yield return instruction;
			}
		}
	}
}
