// Copyright (c) 2022 EchKode
// SPDX-License-Identifier: BSD-3-Clause

using HarmonyLib;
using PhantomBrigade.Data;

namespace EchKode.PBMods.Fixes
{
	static class Screenshot
	{
		public static bool Initialized;
		private static string path;

		public static void Initialize(CaptureWithAlpha instance)
		{
			var fi = AccessTools.DeclaredField(typeof(CaptureWithAlpha), "lossless");
			if (fi == null)
			{
				return;
			}

			fi.SetValue(instance, true);
			// CaptureWithAlpha.SaveScreenshot() builds its path to the screenshot folder so
			// we have to return the path to the parent folder.
			path = DataPathHelper.GetScreenshotFolder() + "../";
			Initialized = true;
		}

		public static string GetProjectPath() => path;
	}
}
