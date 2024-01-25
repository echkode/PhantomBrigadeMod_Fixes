// Copyright (c) 2024 EchKode
// SPDX-License-Identifier: BSD-3-Clause

using System.Collections.Generic;

using HarmonyLib;

using PhantomBrigade.Data;
using PhantomBrigade.Mods;

using UnityEngine;

namespace EchKode.PBMods.DataMultiLinkerLoadDataFix
{
	[HarmonyPatch]
	static class Patch
	{
		internal static void ApplyPatch(Harmony harmonyInstance)
		{
			// Harmony has a hard time patching generic methods automatically, so we have to do it manually.
			// DataMultiLinker<> is a generic type that all multi-databases use but I haven't been able to
			// patch static methods on a generic type without triggering runtime exceptions. So instead I'm
			// patching a method on ModManager that the method I want to patch (DataMultiLinker<>.LoadData)
			// calls.
			//
			// This patch is applied to the generic type parameter of DataContainer but the patched method will
			// be used for every subclass of that type. It has nothing to do with the inheritance hierarchy but
			// with how .NET reifies generic methods.

			var genProcMethodInfo = AccessTools.DeclaredMethod(typeof(ModManager), nameof(ModManager.ProcessConfigModsForMultiLinker));
			var procMethodInfo = genProcMethodInfo.MakeGenericMethod(typeof(DataContainer));
			var replacementMethod = new HarmonyMethod(AccessTools.DeclaredMethod(typeof(Patch), nameof(ProcessConfigModsForMultiLinkerPostfix)));
			harmonyInstance.Patch(procMethodInfo, postfix: replacementMethod);
		}

		static void ProcessConfigModsForMultiLinkerPostfix(System.Type dataType)
		{
			var baseType = typeof(DataMultiLinker<>);
			var multiLinkerType = baseType.MakeGenericType(dataType);
			var refreshListMethodInfo = AccessTools.Method(multiLinkerType, "RefreshList");
			if (refreshListMethodInfo == null)
			{
				Debug.LogWarningFormat(
					"Mod {0} ({1}) unable to get RefreshList method | multilinker type: {2}",
					ModLink.modIndex,
					ModLink.modID,
					multiLinkerType.GenericTypeArguments[0].Name);
				return;
			}
			refreshListMethodInfo.Invoke(null, new object[] { });

			Debug.LogFormat(
				"Mod {0} ({1}) invoked RefreshList | multilinker type: {2}",
				ModLink.modIndex,
				ModLink.modID,
				multiLinkerType.GenericTypeArguments[0].Name);
		}

		[HarmonyPatch(typeof(DataHelperAction), nameof(DataHelperAction.IsAvailable), new System.Type[]
		{
			typeof(DataContainerAction),
			typeof(PersistentEntity),
			typeof(bool),
			typeof(HashSet<EquipmentEntity>),
		})]
		[HarmonyPrefix]
		static void Dha_IsAvailablePrefix(DataContainerAction data)
		{
			// This is just for reporting so you can see what's going on in the logs.

			if (data == null)
			{
				return;
			}
			if (data.key == null)
			{
				Debug.LogWarningFormat(
					"Mod {0} ({1}) null key on action",
					ModLink.modIndex,
					ModLink.modID);
			}
		}

		[HarmonyPatch(typeof(DataContainerAction), nameof(DataContainerAction.OnAfterDeserialization))]
		[HarmonyPostfix]
		static void Dca_OnAfterDeserializationPostfix(string key, DataContainerAction __instance)
		{
			// This is just for reporting so you can see what's going on in the logs.

			Debug.LogFormat(
				"Mod {0} ({1}) OnAfterDeserialization | key: {2} | instance key: {3}",
				ModLink.modIndex,
				ModLink.modID,
				key,
				__instance.key);
		}
	}
}
