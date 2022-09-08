// Copyright (c) 2022 EchKode
// SPDX-License-Identifier: BSD-3-Clause

using HarmonyLib;

using PhantomBrigade;
using PhantomBrigade.Data;
using PhantomBrigade.Combat.Systems;

using Entitas;
using UnityEngine;
using System.Collections.Generic;

namespace EchKode.PBMods.Fixes
{
	using PBProjectileProximityFuseSystem = PhantomBrigade.Combat.Systems.ProjectileProximityFuseSystem;

	public class ProjectileProximityFuseSystem : PBProjectileProximityFuseSystem
	{
		private readonly IGroup<CombatEntity> group;

		public static void Initialize()
		{
			Heartbeat.Systems.Add(Load);
		}

		public static void Load(GameController gameController)
		{
			var gcs = gameController.m_stateDict["combat"];
			var combatSystems = gcs.m_systems[0];
			var fi = AccessTools.Field(combatSystems.GetType(), "_executeSystems");
			var systems = (List<IExecuteSystem>)fi.GetValue(combatSystems);
			var idx = systems.FindIndex(sys => sys is PBProjectileProximityFuseSystem);
			if (idx == -1)
			{
				return;
			}

			systems[idx] = new ProjectileProximityFuseSystem(Contexts.sharedInstance);
			Debug.Log($"Mod {ModLink.modIndex} ({ModLink.modId}) extending {nameof(ProjectileProximityFuseSystem)}");

			// XXX not sure how necessary this is since the profiler is something you generally use from within
			// the Unity editor.
			fi = AccessTools.Field(combatSystems.GetType(), "_executeSystemNames");
			var names = (List<string>)fi.GetValue(combatSystems);
			names[idx] = systems[idx].GetType().FullName;
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
