// Copyright (c) 2022 EchKode
// SPDX-License-Identifier: BSD-3-Clause

using Entitas;

namespace EchKode.PBMods.Fixes.ECS
{
	[EkCombat]
	public sealed class LastStrikeComponent : IComponent
	{
		public int sourceID;
		public int combatID;
		public int pilotID;
	}
}
