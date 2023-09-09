// Copyright (c) 2023 EchKode
// SPDX-License-Identifier: BSD-3-Clause

using HarmonyLib;

using PBActionUtility = PhantomBrigade.ActionUtility;

using UnityEngine;

namespace EchKode.PBMods.ActionUtilityFix
{
	[HarmonyPatch]
	static class Patch
	{
		[HarmonyPatch(typeof(PBActionUtility), "GetScatterAngleAtTime")]
		[HarmonyPostfix]
		static void Au_GetScatterAngleAtTimePostfix(ref float __result)
		{
			__result = Mathf.Max(0f, __result);
		}
	}
}
