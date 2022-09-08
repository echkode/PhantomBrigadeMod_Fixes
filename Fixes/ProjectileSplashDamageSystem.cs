// Copyright (c) 2022 EchKode
// SPDX-License-Identifier: BSD-3-Clause

using System.Collections.Generic;

using HarmonyLib;

using PhantomBrigade;
using PhantomBrigade.Data;
using PhantomBrigade.Combat.Components;
using PhantomBrigade.Combat.View;

using UnityEngine;
using Entitas;

namespace EchKode.PBMods.Fixes
{
	using PBProjectileSplashDamageSystem = PhantomBrigade.Combat.Systems.ProjectileSplashDamageSystem;

	public class ProjectileSplashDamageSystem : PBProjectileSplashDamageSystem
	{
		private readonly Collider[] overlapColliders;
		private readonly CombatView[] overlapViews;
		private readonly Dictionary<int, CombatEntity> hitEntities;

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
			var idx = systems.FindIndex(sys => sys is PBProjectileSplashDamageSystem);
			if (idx == -1)
			{
				return;
			}

			systems[idx] = new ProjectileSplashDamageSystem(Contexts.sharedInstance);
			Debug.Log($"Mod {ModLink.modIndex} ({ModLink.modId}) extending {nameof(ProjectileSplashDamageSystem)}");

			// XXX not sure how necessary this is since the profiler is something you generally use from within
			// the Unity editor.
			fi = AccessTools.Field(combatSystems.GetType(), "_executeSystemNames");
			var names = (List<string>)fi.GetValue(combatSystems);
			names[idx] = systems[idx].GetType().FullName;
		}

		public ProjectileSplashDamageSystem(Contexts contexts)
			: base(contexts)
		{
			var fi = AccessTools.DeclaredField(typeof(PBProjectileSplashDamageSystem), nameof(overlapColliders));
			overlapColliders = (Collider[])fi.GetValue(this);
			fi = AccessTools.DeclaredField(typeof(PBProjectileSplashDamageSystem), nameof(overlapViews));
			overlapViews = (CombatView[])fi.GetValue(this);
			fi = AccessTools.DeclaredField(typeof(PBProjectileSplashDamageSystem), nameof(hitEntities));
			hitEntities = (Dictionary<int, CombatEntity>)fi.GetValue(this);
		}

		protected override void Execute(List<CombatEntity> entities)
		{
			for (var i = 0; i < entities.Count; i += 1)
			{
				var entity = entities[i];
				entity.isDamageSplash = false;
				if (!entity.isProjectilePrimed)
				{
					continue;  // original code: break
				}

				var data = entity.dataLinkSubsystemProjectile.data;
				var splashDamage = data?.splashDamage;
				if (data == null || splashDamage == null)
				{
					Debug.LogWarning("Failed to apply splash damage due to missing splash damage or projectile data");
					continue;
				}

				var damageRadius = DataHelperStats.GetCachedStatForPart("wpn_damage_radius", IDUtility.GetEquipmentEntity(entity.parentPart.equipmentID));
				var destructionPosition = entity.projectileDestructionPosition.v;
				var exponent = splashDamage.exponent;
				var hasAsset = !string.IsNullOrEmpty(splashDamage.fxHit);
				var inflictedDamage = entity.hasInflictedDamage ? entity.inflictedDamage.f : 0.0f;
				var inflictedConcussion = entity.hasInflictedConcussion ? entity.inflictedConcussion.f : 0.0f;

				if (damageRadius.RoughlyEqual(0.0f))
				{
					Debug.LogWarning("Radius of projectile splash was set to 0, make sure to correct this in the data");
					damageRadius = 1f;
				}

				if (!string.IsNullOrEmpty(splashDamage.fxDetonation))
				{
					var position = destructionPosition;
					AssetPoolUtility.ActivateInstance(splashDamage.fxDetonation, position, Vector3.forward);
				}

				if (!string.IsNullOrEmpty(splashDamage.fxArea))
				{
					var position = destructionPosition;
					var scale = damageRadius * 2f * Vector3.one;
					AssetPoolUtility.ActivateInstance(splashDamage.fxArea, position, Vector3.forward, scale);
				}

				if (inflictedDamage <= 0f && inflictedConcussion <= 0f)
				{
					continue;
				}

				var hitCount = PhysicsService.CheckOverlapSphereNonAlloc((LayerMask)LayerMasks.impactTriggersMask, destructionPosition, damageRadius, overlapColliders, overlapViews);
				if (hitCount == 0)
				{
					continue;
				}

				DetermineHitEntities(hitCount);
				DealDamage(
					entity,
					data,
					splashDamage,
					destructionPosition,
					exponent,
					hasAsset,
					damageRadius,
					inflictedDamage,
					inflictedConcussion);

			}
		}

		private void DetermineHitEntities(int count)
		{
			hitEntities.Clear();
			for (var i = 0; i < count; i += 1)
			{
				var overlapView = overlapViews[i];
				if (overlapView != null)
				{
					var linkedCombatEntity = overlapView.GetLinkedCombatEntity();
					if (linkedCombatEntity != null && !hitEntities.ContainsKey(linkedCombatEntity.id.id))
					{
						hitEntities.Add(linkedCombatEntity.id.id, linkedCombatEntity);
					}
				}
			}
		}

		private void DealDamage(
			CombatEntity projectile,
			DataBlockSubsystemProjectile_V2 data,
			DataBlockProjectileSplashDamage splashDamage,
			Vector3 destructionPosition,
			float exponent,
			bool hasAsset,
			float damageRadius,
			float inflictedDamage,
			float inflictedConcussion)
		{
			foreach (var combatEntity in hitEntities.Values)
			{
				var hitDirection = (destructionPosition - combatEntity.position.v).normalized.Flatten();
				var hitPosition = combatEntity.position.v;
				if (combatEntity.hasLocalCenterPoint)
				{
					// Adjust hit position for object's center of mass.
					hitPosition += combatEntity.localCenterPoint.v;
				}
				if (hasAsset)
				{
					// Weird. You'd think they'd use the same hit position as when calculating
					// hit direction but instead they're passing in the adjusted hit position.
					AssetPoolUtility.ActivateInstance(splashDamage.fxHit, hitPosition, hitDirection);
				}
				var t = Mathf.Pow(Vector3.Distance(destructionPosition, hitPosition) / damageRadius, exponent);
				// Use a decay curve from point of impact to center-of-mass of object to determine
				// damage and concussion values.
				var actualDamage = Mathf.RoundToInt(Mathf.Lerp(inflictedDamage, inflictedDamage * 0.5f, t));
				var actualConcussion = Mathf.RoundToInt(Mathf.Lerp(inflictedConcussion, inflictedConcussion * 0.5f, t));
				var hitEntity = Contexts.sharedInstance.combat.CreateEntity();
				hitEntity.isDamageEvent = true;
				hitEntity.AddDamageGroup(DamageEventGroup.Splash);
				hitEntity.AddEventTargetEntity(combatEntity.id.id);
				if (projectile.hasSourceEntity)
				{
					hitEntity.AddEventSourceEntity(projectile.sourceEntity.combatID);
				}
				hitEntity.AddFacing(hitDirection);
				hitEntity.AddPosition(hitPosition);
				hitEntity.AddInflictedDamage(actualDamage);
				hitEntity.AddInflictedConcussion(actualConcussion);
				hitEntity.isDamageSplash = true;

				if (projectile.isDamageDispersed)
				{
					hitEntity.isDamageDispersed = true;
				}

				if (projectile.hasLevel)
				{
					hitEntity.AddLevel(projectile.level.i);
				}

				if (data.damageDelay != null && data.damageDelay.f > 0.0)
				{
					hitEntity.AddDelay(data.damageDelay.f);
				}
			}
		}
	}
}