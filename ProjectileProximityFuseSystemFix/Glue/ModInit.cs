// Copyright (c) 2023 EchKode
// SPDX-License-Identifier: BSD-3-Clause

using PhantomBrigade;

using PBProjectileProximityFuseSystem = PhantomBrigade.Combat.Systems.ProjectileProximityFuseSystem;

namespace EchKode.PBMods.ProjectileProximityFuseSystemFix
{
	public partial class ModLink
	{
		static void Initialize()
		{
			Heartbeat.Systems.Add(gc =>
				ReplacementSystemLoader.Load<PBProjectileProximityFuseSystem, ProjectileProximityFuseSystem>(
					gc,
					GameStates.combat,
					contexts => new ProjectileProximityFuseSystem(contexts)));
		}
	}
}
