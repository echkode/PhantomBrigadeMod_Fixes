// Copyright (c) 2022 EchKode
// SPDX-License-Identifier: BSD-3-Clause

using System.Collections.Generic;

using Entitas;

using HarmonyLib;

using PhantomBrigade;
using PhantomBrigade.Data;
using PhantomBrigade.Combat.Systems;
using PBProjectileProximityFuseSystem = PhantomBrigade.Combat.Systems.ProjectileProximityFuseSystem;

using UnityEngine;

namespace EchKode.PBMods.Fixes
{
	public class ProjectileProximityFuseSystem : PBProjectileProximityFuseSystem
	{
		private readonly IGroup<CombatEntity> group;

		public static void Initialize()
		{
			Heartbeat.Systems.Add(gc =>
				ReplacementSystemLoader.Load<PBProjectileProximityFuseSystem, ProjectileProximityFuseSystem>(
					gc,
					"combat",
					contexts => new ProjectileProximityFuseSystem(contexts)));
		}

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
					continue;  // original code: break
				}

				var data = projectile.dataLinkSubsystemProjectile.data;
				if (data?.fuseProximity == null)
				{
					continue;
				}

				var position = projectile.position.v;
				// Why is facing being scaled by 10?
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
							continue;  // original code: break
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
					Debug.LogWarning($"Destroying projectile {projectile.ToLog()} due to proximity fuse triggering at distance {distance}");
				}
				projectile.isProjectileProximityFuse = false;
				projectile.TriggerProjectile();
			}
		}
	}
}
