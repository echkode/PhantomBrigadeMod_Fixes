// Copyright (c) 2022 EchKode
// SPDX-License-Identifier: BSD-3-Clause

using System.Collections.Generic;

using PBCIViewCombatMode = CIViewCombatMode;

using HarmonyLib;

namespace EchKode.PBMods.Fixes
{
	static class CombatLandingSystem
	{
		// This has been fixed as of PB release 0.22.

		private static List<int> unitsToRemove;

		internal static void Initialize()
		{
			var fi = AccessTools.DeclaredField(typeof(PhantomBrigade.Combat.Systems.CombatLandingSystem), nameof(unitsToRemove));
			unitsToRemove = (List<int>)fi.GetValue(null);
		}

		internal static void RefreshTabs()
		{
			if (unitsToRemove.Count == 0)
			{
				return;
			}
			PBCIViewCombatMode.ins.RedrawUnitTabs();
		}
	}
}
