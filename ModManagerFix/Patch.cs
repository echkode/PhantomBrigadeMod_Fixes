// Copyright (c) 2024 EchKode
// SPDX-License-Identifier: BSD-3-Clause

using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;

using HarmonyLib;

using PhantomBrigade.Mods;
using PBModLink = PhantomBrigade.Mods.ModLink;

using UnityEngine;

namespace EchKode.PBMods.ModManagerFix
{
	[HarmonyPatch]
	public static class Patch
	{
		[HarmonyPatch(typeof(ModManager), nameof(ModManager.LoadMod))]
		[HarmonyTranspiler]
		static IEnumerable<CodeInstruction> Mm_LoadModTranspiler(IEnumerable<CodeInstruction> instructions, ILGenerator generator)
		{
			// Move loading libraries earlier in the sequence so YAML tag mappings get registered before trying to
			// read any ConfigEdits/ConfigOverrides files.

			var cm = new CodeMatcher(instructions, generator);
			var modLoadedDataConstructorInfo = AccessTools.Constructor(typeof(ModLoadedData), new System.Type[] { });
			var tryLoadingConfigTreesMethodInfo = AccessTools.DeclaredMethod(typeof(ModManager), nameof(ModManager.TryLoadingConfigTrees));
			var logWarningMethodInfo = AccessTools.DeclaredMethod(typeof(Debug), nameof(Debug.LogWarning), new System.Type[] { typeof(object) });
			var logMethodInfo = AccessTools.DeclaredMethod(typeof(Debug), nameof(Debug.Log), new System.Type[] { typeof(object) });
			var modLoadedDataMatch = new CodeMatch(OpCodes.Newobj, modLoadedDataConstructorInfo);
			var tryLoadingConfigTreesMatch = new CodeMatch(OpCodes.Call, tryLoadingConfigTreesMethodInfo);
			var logWarningMatch = new CodeMatch(OpCodes.Call, logWarningMethodInfo);
			var logMatch = new CodeMatch(OpCodes.Call, logMethodInfo);

			cm.MatchEndForward(modLoadedDataMatch)
				.MatchEndForward(tryLoadingConfigTreesMatch)
				.Advance(1);
			var deleteStart = cm.Pos;
			var labels = new List<Label>(cm.Labels);
			cm.Labels.Clear();

			cm.MatchEndForward(logWarningMatch);
			var offset = deleteStart - cm.Pos;

			cm.Advance(1);
			var swapLabels = labels;
			labels = new List<Label>(cm.Labels);
			cm.Labels.Clear();
			cm.AddLabels(swapLabels)
				.Advance(-1);
			var loadLibraries = cm.InstructionsWithOffsets(offset, 0);

			cm.RemoveInstructionsWithOffsets(offset, 0)
				.Advance(offset)
				.MatchEndBackwards(logMatch)
				.Advance(1)
				.AddLabels(labels)
				.InsertAndAdvance(loadLibraries);

			return cm.InstructionEnumeration();
		}

		[HarmonyPatch(typeof(ModManager), nameof(ModManager.TryLoadingLibraries))]
		[HarmonyTranspiler]
		static IEnumerable<CodeInstruction> Mm_TryLoadingLibrariesTranspiler(IEnumerable<CodeInstruction> instructions, ILGenerator generator)
		{
			// Make call to register tag mappings in the loaded mod assemblies. Do after OnLoad() is called
			// on the mod to make sure it loads and patches successfully.

			var cm = new CodeMatcher(instructions, generator);
			var onLoadMethodInfo = AccessTools.DeclaredMethod(typeof(PBModLink), nameof(PBModLink.OnLoad));
			var onLoadMatch = new CodeMatch(OpCodes.Callvirt, onLoadMethodInfo);
			var loadedData = new CodeInstruction(OpCodes.Ldarg_2);
			var registerModTagMappings = CodeInstruction.Call(typeof(Patch), nameof(RegisterModTagMappings));

			cm.End()
				.MatchEndBackwards(onLoadMatch)
				.Advance(1)
				.InsertAndAdvance(loadedData)
				.InsertAndAdvance(registerModTagMappings);

			return cm.InstructionEnumeration();
		}

		[HarmonyPatch(typeof(UtilitiesYAML), nameof(UtilitiesYAML.LoadTagMappings))]
		[HarmonyTranspiler]
		static IEnumerable<CodeInstruction> Uy_LoadTagMappingsTranspiler(IEnumerable<CodeInstruction> instructions, ILGenerator generator)
		{
			// Add a guard against clobbering an existing tag map. This is so that SetupYAMLReader() can be call repeatedly
			// by the ModManager. SetupYAMLReader() internally calls LoadTagMappings().

			var cm = new CodeMatcher(instructions, generator);
			var loadTagMappings = CodeInstruction.LoadField(typeof(UtilitiesYAML), "tagMappings");
			var ret = new CodeInstruction(OpCodes.Ret);

			cm.Start();
			cm.CreateLabel(out var skipRetLabel);
			var skipRet = new CodeInstruction(OpCodes.Brfalse_S, skipRetLabel);

			cm.InsertAndAdvance(loadTagMappings)
				.InsertAndAdvance(skipRet)
				.InsertAndAdvance(ret);

			return cm.InstructionEnumeration();
		}

		[HarmonyPatch(typeof(UtilitiesYAML), nameof(UtilitiesYAML.LoadTagMappings))]
		[HarmonyReversePatch]
		public static void RegisterTagMappings(Assembly assembly)
		{
			// Avoid reinventing the wheel and extract the foreach loop that gets any types that have
			// TypeHintedAttribute or implement an interface with that attribute and loads them
			// into the tag map.

			IEnumerable<CodeInstruction>  Transpiler(IEnumerable < CodeInstruction > instructions, ILGenerator generator)
			{
				var cm = new CodeMatcher(instructions, generator);
				var getDefinedTypesMethodInfo = AccessTools.DeclaredPropertyGetter(typeof(Assembly), nameof(Assembly.DefinedTypes));
				var getDefinedTypesMatch = new CodeMatch(OpCodes.Callvirt, getDefinedTypesMethodInfo);
				var loadAssembly = new CodeInstruction(OpCodes.Ldarg_0);

				cm.MatchStartForward(getDefinedTypesMatch)
					.Advance(-1);
				var offset = 0 - cm.Pos;
				cm.RemoveInstructionsWithOffsets(offset, 0)
					.Advance(offset)
					.InsertAndAdvance(loadAssembly);

				return cm.InstructionEnumeration();
			}

			_ = Transpiler(null, null);
		}

		[HarmonyPatch(typeof(UtilitiesYAML), "SetupReader")]
		[HarmonyReversePatch]
		public static void SetupYAMLReader()
		{
			// This gives me direct access to the method without having to go through reflection.

			throw new System.NotImplementedException("Harmony reverse patch");
		}

		public static void RegisterModTagMappings(ModLoadedData loadedData)
		{
			var tagCount = UtilitiesYAML.GetTagMappings().Count;
			foreach (var assembly in loadedData.assemblies)
			{
				RegisterTagMappings(assembly);
			}
			if (tagCount == UtilitiesYAML.GetTagMappings().Count)
			{
				// No change so skip re-initializing the YAML deserializer.
				return;
			}

			// Rebuild the deserializer so that if there are config files as well in the mod they can use
			// the new tag mappings. We have to null out the existing deserializer to get past a guard in
			// SetupYAMLReader().

			var deserializerFieldInfo = AccessTools.DeclaredField(typeof(UtilitiesYAML), "deserializer");
			deserializerFieldInfo.SetValue(null, null);

			SetupYAMLReader();
		}

		[HarmonyPatch(typeof(ModUtilities), nameof(ModUtilities.Initialize))]
		[HarmonyTranspiler]
		static IEnumerable<CodeInstruction> Mu_InitializeTranspiler(IEnumerable<CodeInstruction> instructions, ILGenerator generator)
		{
			// Take out extraneous log call.

			var cm = new CodeMatcher(instructions, generator);
			var logMethodInfo = AccessTools.DeclaredMethod(typeof(Debug), nameof(Debug.LogFormat), new System.Type[]
			{
				typeof(string),
				typeof(object[]),
			});
			var logMatch = new CodeMatch(OpCodes.Call, logMethodInfo);
			var loadStrMatch = new CodeMatch(OpCodes.Ldstr);

			cm.MatchEndForward(logMatch);
			var deleteEnd = cm.Pos;

			cm.MatchStartBackwards(loadStrMatch)
				.Advance(-1)
				.MatchStartBackwards(loadStrMatch);
			var offset = deleteEnd - cm.Pos;
			cm.RemoveInstructionsWithOffsets(0, offset);

			return cm.InstructionEnumeration();
		}
	}
}
