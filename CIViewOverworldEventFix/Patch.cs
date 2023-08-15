// Copyright (c) 2023 EchKode
// SPDX-License-Identifier: BSD-3-Clause

using HarmonyLib;

using PBCIViewOverworldEvent = CIViewOverworldEvent;

namespace EchKode.PBMods.CIViewOverworldEventFix
{
	[HarmonyPatch]
	static class Patch
	{
		[HarmonyPatch(typeof(PBCIViewOverworldEvent), "FadeOutEnd")]
		[HarmonyPrefix]
		static bool Civoe_FadeOutEndPrefix()
		{
			CIViewOverworldEvent.FadeOutEnd();
			return false;
		}
	}
}
