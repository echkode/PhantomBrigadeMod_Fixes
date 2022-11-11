// Copyright (c) 2022 EchKode
// SPDX-License-Identifier: BSD-3-Clause

using Entitas;

using PhantomBrigade;

using UnityEngine;

namespace EchKode.PBMods.Fixes.ECS
{
	public sealed class EkCombatTeardownSystem : ITearDownSystem
	{
		private readonly EkCombatContext context;

		public static void Initialize()
		{
			Heartbeat.Systems.Add(Load);
		}

		public static void Load(GameController gameController)
		{
			var gcs = gameController.m_stateDict["combat"];
			var combatSystems = gcs.m_systems[0];
			combatSystems.Add(new EkCombatTeardownSystem(Contexts.sharedInstance));
			Debug.Log($"Mod {ModLink.modIndex} ({ModLink.modId}) adding {nameof(EkCombatTeardownSystem)}");
		}

		public EkCombatTeardownSystem(Contexts contexts)
		{
			context = contexts.ekCombat;
		}

		public void TearDown()
		{
			foreach (var entity in context.GetEntities())
			{
				entity.Destroy();
			}
		}
	}
}
