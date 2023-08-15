// Copyright (c) 2023 EchKode
// SPDX-License-Identifier: BSD-3-Clause

using System.Collections.Generic;

using Entitas;

using HarmonyLib;

using PhantomBrigade;
using PhantomBrigade.Data;
using PhantomBrigade.Combat.Systems;
using PBProjectileProximityFuseSystem = PhantomBrigade.Combat.Systems.ProjectileProximityFuseSystem;

using UnityEngine;

namespace EchKode.PBMods.ProjectileProximityFuseSystemFix
{
	public class ProjectileProximityFuseSystem : PBProjectileProximityFuseSystem
	{
		private readonly IGroup<CombatEntity> group;

		public ProjectileProximityFuseSystem(Contexts contexts)
			: base(contexts)
		{
			var fi = AccessTools.DeclaredField(typeof(PBProjectileProximityFuseSystem), nameof(group));
			group = (IGroup<CombatEntity>)fi.GetValue(this);
		}

		protected override void Execute(List<CombatEntity> entities)
		{
			foreach (var projectile in group.GetEntities())
			{
				if (!projectile.isProjectilePrimed)
				{
					continue;
				}

				var data = projectile.dataLinkSubsystemProjectile.data;
				if (data?.fuseProximity == null)
				{
					continue;
				}

				var position = projectile.position.v;
				var targetPosition = position + projectile.facing.v * 10f;
				if (projectile.hasProjectileGuidanceTargetPosition)
				{
					targetPosition = projectile.projectileGuidanceTargetPosition.v;
				}
				else if (projectile.hasProjectileTargetEntity)
				{
					var combatant = IDUtility.GetCombatEntity(projectile.projectileTargetEntity.combatID);
					if (combatant != null)
					{
						if (combatant.hasCurrentDashAction || combatant.hasCurrentMeleeAction)
						{
							continue;  // FIX original code: break
						}
						targetPosition = combatant.position.v;
						if (combatant.hasLocalCenterPoint)
						{
							targetPosition += combatant.localCenterPoint.v;
						}
					}
				}
				else if (projectile.hasProjectileTargetPosition)
				{
					targetPosition = projectile.projectileTargetPosition.v;
				}

				if (projectile.hasProjectileGuidanceSuspended)
				{
					continue;
				}

				var distance = Vector3.Distance(targetPosition, position);
				if (distance >= data.fuseProximity.distance)
				{
					continue;
				}

				if (DataShortcuts.sim.logProjectileStatus)
				{
					Debug.DrawLine(position, targetPosition, Color.yellow, 10f);
					Debug.LogWarningFormat(
						"Destroying projectile {0} due to proximity fuse triggering at distance {1}",
						projectile.ToLog(),
						distance);
				}
				projectile.isProjectileProximityFuse = false;
				projectile.TriggerProjectile();
			}
		}
	}
}
