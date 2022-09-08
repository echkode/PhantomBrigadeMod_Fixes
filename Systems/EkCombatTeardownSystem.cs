// Copyright (c) 2022 EchKode
// SPDX-License-Identifier: BSD-3-Clause

using Entitas;

namespace EchKode.PBMods.Fixes.ECS
{
	public sealed class EkCombatTeardownSystem : ITearDownSystem
	{
		private readonly EkCombatContext context;

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
