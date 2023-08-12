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
