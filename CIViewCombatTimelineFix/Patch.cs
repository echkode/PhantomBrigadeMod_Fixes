// Copyright (c) 2023 EchKode
// SPDX-License-Identifier: BSD-3-Clause

using HarmonyLib;

using UnityEngine;

namespace EchKode.PBMods.CIViewCombatTimelineFix
{
	[HarmonyPatch]
	static class Patch
	{
		internal static bool useOriginalImplemenation = false;

		[HarmonyPatch(typeof(CIViewCombatTimeline), "AdjustTimelineRegions")]
		[HarmonyPrefix]
		static bool Civct_AdjustTimelineRegions(
			int actionIDSelected,
			float actionStartTime,
			float offsetSelected,
			bool offsetCorrectionAllowed,
			int depth,
			CIViewCombatTimeline __instance)
		{
			if (useOriginalImplemenation)
			{
				return true;
			}

			if (!offsetCorrectionAllowed)
			{
				Debug.LogWarningFormat(
					"Mod {0} ({1}) AdjustTimelineRegions called with unexpected argument value, reverting to original implementation | offsetCorrectionAllowed: false",
					ModLink.modIndex,
					ModLink.modID);
				return true;
			}

			if (depth != 0)
			{
				Debug.LogWarningFormat(
					"Mod {0} ({1}) AdjustTimelineRegions called with unexpected argument value, reverting to original implementation | depth: {2}",
					ModLink.modIndex,
					ModLink.modID,
					depth);
				return true;
			}

			CIViewCombatTimelineFix.AdjustTimelineRegions(
				__instance,
				actionIDSelected,
				actionStartTime,
				offsetSelected);
			return false;
		}
	}
}
