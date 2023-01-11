using HarmonyLib;

using PBCIViewOverworldEvent = CIViewOverworldEvent;

using UnityEngine;

namespace EchKode.PBMods.Fixes
{
	using AnimDelegate = System.Action<float>;

	static class CIViewOverworldEvent
	{
		private static Transform centeringHolder;
		private static Traverse<string> eventKeyCached;
		private static Traverse<string> stepKeyCached;
		private static AnimDelegate FadeInAnim;

		internal static void FadeOutEnd()
		{
			if (centeringHolder == null)
			{
				var t = Traverse.Create(PBCIViewOverworldEvent.ins);
				centeringHolder = t.Field<Transform>(nameof(centeringHolder)).Value;
				eventKeyCached = t.Field<string>(nameof(eventKeyCached));
				stepKeyCached = t.Field<string>(nameof(stepKeyCached));
				FadeInAnim = (AnimDelegate)AccessTools
					.DeclaredMethod(typeof(PBCIViewOverworldEvent), nameof(FadeInAnim))
					.CreateDelegate(typeof(AnimDelegate), PBCIViewOverworldEvent.ins);
			}

			LeanTween.cancelIfTweening(centeringHolder.gameObject);
			LTDescr ltDescr = LeanTween.value(centeringHolder.gameObject, 0.0f, 1f, 0.5f);
			ltDescr.setEase(LeanTweenType.easeInSine);
			ltDescr.setIgnoreTimeScale(true);
			ltDescr.setOnUpdate(FadeInAnim);
			PBCIViewOverworldEvent.ins.TryEntryToStep(eventKeyCached.Value, stepKeyCached.Value);
		}
	}
}
