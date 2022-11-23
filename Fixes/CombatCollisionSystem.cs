// Copyright (c) 2022 EchKode
// SPDX-License-Identifier: BSD-3-Clause

using System.Collections.Generic;

using PhantomBrigade;
using PhantomBrigade.Combat.Systems;
using PhantomBrigade.Data;

using UnityEngine;

namespace EchKode.PBMods.Fixes
{
	using PBCombatCollisionSystem = PhantomBrigade.Combat.Systems.CombatCollisionSystem;

	public class CombatCollisionSystem : PBCombatCollisionSystem
	{
		public static void Initialize()
		{
			Heartbeat.Systems.Add(gc =>
				ReplacementSystemLoader.Load<PBCombatCollisionSystem, CombatCollisionSystem>(
					gc,
					"combat",
					contexts => new CombatCollisionSystem(contexts, ECS.Contexts.sharedInstance.ekCombat)));
		}

		private readonly CombatContext combat;
		private readonly ECS.EkCombatContext ekCombat;

		public CombatCollisionSystem(Contexts pbContexts, ECS.EkCombatContext ekCombat)
			: base(pbContexts)
		{
			combat = pbContexts.combat;
			this.ekCombat = ekCombat;
		}

		protected override void Execute(List<CombatEntity> entities)
		{
			foreach (var collisionEvent in entities)
			{
				var collisionSource = IDUtility.GetCombatEntity(collisionEvent.collisionSource.combatID);
				if (collisionEvent.collisionOther.t == null)
				{
					continue;
				}

				var gameObject = collisionEvent.collisionOther.t.gameObject;
				if (gameObject == null)
				{
					continue;
				}

				var spent = false;
				if (collisionEvent.CollisionEnter)
				{
					var (skip, sp) = CollisionEnter(collisionEvent, collisionSource, gameObject);
					if (skip)
					{
						continue;
					}
					spent = sp;
				}

				if (spent && !collisionEvent.isCollisionPenetrating)
				{
					if (collisionSource.hasDataLinkSubsystemProjectile)
					{
						collisionSource.TriggerProjectile();
					}
					else
					{
						collisionSource.isDestroyed = true;
					}
				}

				collisionEvent.isDestroyed = true;
			}
		}

		private (bool, bool) CollisionEnter(
			CombatEntity collisionEvent,
			CombatEntity collisionSource,
			GameObject gameObject)
		{
			var deltaTime = combat.simulationDeltaTime.f;
			var simulationTime = combat.simulationTime.f;
			var (collidedEntity, unitPersistent) = ResolveCollidedEntityToUnit(collisionEvent, collisionSource);
			var subsystemProjectile = collisionSource.hasDataLinkSubsystemProjectile ? collisionSource.dataLinkSubsystemProjectile.data : null;
			var (damageFalloff, isFriendly) = DamageFromPart(collisionSource, collidedEntity, subsystemProjectile, gameObject);
			var ricochetChance = collisionSource.hasRicochetChance ? collisionSource.ricochetChance.f : 0f;
			var hasMovementSpeed = collisionSource.hasMovementSpeedCurrent;
			var ricochetPossible = ricochetChance > 0f && Random.Range(0f, 1f) < ricochetChance;

			if (collidedEntity != null)
			{
				if (collidedEntity.id.id == collisionEvent.collisionSource.combatID)
				{
					// Skip self-collisions.
					return (true, false);
				}

				if (collisionEvent.hasSourceEntity && collisionEvent.sourceEntity.combatID == collidedEntity.id.id)
				{
					// Skip self-inflicted wounds.
					return (true, false);
				}

				if (!collidedEntity.isUnitTag)
				{
					return (false, false);
				}

				var spent = TryHitUnit(
					deltaTime,
					simulationTime,
					collisionEvent,
					collisionSource,
					collidedEntity,
					unitPersistent,
					subsystemProjectile,
					isFriendly,
					hasMovementSpeed,
					damageFalloff,
					ricochetPossible);
				return (false, spent);
			}

			if (gameObject.layer == LayerMasks.environmentLayerID)
			{
				var spent = TryHitEnvironment(
					deltaTime,
					collisionEvent,
					collisionSource,
					subsystemProjectile,
					gameObject,
					isFriendly,
					hasMovementSpeed,
					damageFalloff,
					ricochetPossible);
				return (false, spent);
			}

			if (gameObject.layer == LayerMasks.propLayerID)
			{
				Area.AreaManager.OnPropImpactFromProjectile(
					collisionEvent.collisionOther.t.gameObject.GetComponent<Collider>().GetInstanceID(),
					collisionEvent.collisionOther.t.position,
					collisionSource.hasFacing ? collisionSource.facing.v : Vector3.forward,
					1f,
					1f);
				if (subsystemProjectile?.visual?.impact != null)
				{
					var impact = subsystemProjectile.visual.impact;
					var key = !impact.factionUsed || isFriendly ? impact.key : impact.keyEnemy;
					if (!string.IsNullOrEmpty(key))
					{
						var position = collisionEvent.collisionContactPoint.position;
						var normal = collisionEvent.collisionContactPoint.normal;
						AssetPoolUtility.ActivateInstance(key, position, normal, impact.scale);
					}
				}

				return (false, true);
			}

			return (false, false);
		}

		private static (CombatEntity, PersistentEntity)
			ResolveCollidedEntityToUnit(CombatEntity collisionEvent, CombatEntity collisionSource)
		{
			if (!collisionEvent.hasCollidedEntity)
			{
				return (null, null);
			}

			if (!collisionEvent.hasSourceEntity)
			{
				return (null, null);
			}

			var combatant = IDUtility.GetCombatEntity(collisionEvent.collidedEntity.combatID);
			var unit = IDUtility.GetLinkedPersistentEntity(combatant);
			var persistentEntity = IDUtility.GetLinkedPersistentEntity(IDUtility.GetCombatEntity(collisionEvent.sourceEntity.combatID));
			var isFriendly = CombatUIUtility.IsUnitFriendly(unit);
			collisionSource.ReplaceUnitHit(persistentEntity.id.id, isFriendly);

			return (combatant, unit);
		}

		private static (float, bool) DamageFromPart(
			CombatEntity collisionSource,
			CombatEntity collidedEntity,
			DataBlockSubsystemProjectile_V2 subsystemProjectile,
			GameObject gameObject)
		{
			if (!collisionSource.hasParentPart)
			{
				return (1f, true);
			}

			var equipmentEntity = IDUtility.GetEquipmentEntity(collisionSource.parentPart.equipmentID);
			if (equipmentEntity == null)
			{
				return (1f, true);
			}

			var curve = subsystemProjectile?.falloff?.curve;
			if (curve == null)
			{
				return (1f, true);
			}

			var minRange = Mathf.Max(0f, DataHelperStats.GetCachedStatForPart("wpn_range_min", equipmentEntity));
			var maxRange = Mathf.Max(0f, DataHelperStats.GetCachedStatForPart("wpn_range_max", equipmentEntity));
			var totalRange = Mathf.Max(0f, maxRange - minRange);
			if (totalRange.RoughlyEqual(0f))
			{
				Debug.LogWarning($"Part {equipmentEntity.ToLog()} has invalid range stats resulting in division by zero");
				totalRange = 1f;
			}

			var useCollidedPosition = collidedEntity?.hasPosition ?? false;
			var distance = useCollidedPosition
				? Vector3.Distance(collisionSource.flightInfo.origin, collidedEntity.position.v)
				: Vector3.Distance(collisionSource.flightInfo.origin, gameObject.transform.position);
			var timeNormalized = Mathf.Clamp01((distance - minRange) / totalRange);
			var damageFalloff = Mathf.Clamp01(curve.GetCurveSample(timeNormalized));
			var isFriendly = !equipmentEntity.hasPartParentUnit
				|| CombatUIUtility.IsUnitFriendly(IDUtility.GetPersistentEntity(equipmentEntity.partParentUnit.persistentID));

			return (damageFalloff, isFriendly);
		}

		private bool TryHitUnit(
			float deltaTime,
			float simulationTime,
			CombatEntity collisionEvent,
			CombatEntity collisionSource,
			CombatEntity collidedEntity,
			PersistentEntity unit,
			DataBlockSubsystemProjectile_V2 subsystemProjectile,
			bool isFriendly,
			bool hasMovementSpeed,
			float damageFalloff,
			bool ricochetPossible)
		{
			var doRicochet = hasMovementSpeed && ricochetPossible && damageFalloff <= DataShortcuts.sim.falloffRicochetThreshold;
			if (!doRicochet && hasMovementSpeed)
			{
				if (collidedEntity.hasCurrentMeleeAction && DataShortcuts.sim.ricochetOnMelee)
				{
					var actionEntity = IDUtility.GetActionEntity(collidedEntity.currentMeleeAction.actionID);
					if (actionEntity != null && actionEntity.hasMovementMeleeAttacker)
					{
						var startTime = actionEntity.startTime.f;
						var duration = Mathf.Max(0.1f, actionEntity.duration.f);
						var normalizedTime = Mathf.Clamp01(Mathf.Max(0f, simulationTime - startTime) / duration);
						var movementMeleeAttacker = actionEntity.movementMeleeAttacker;
						if (normalizedTime > movementMeleeAttacker.impactTimeStart && normalizedTime < movementMeleeAttacker.impactTimeEnd)
						{
							Debug.LogWarning($"Forcing ricochet for entity {collidedEntity.ToLog()} due to it performing a melee strike");
							doRicochet = true;
						}
					}
				}

				if (collidedEntity.hasCurrentDashAction && DataShortcuts.sim.ricochetOnDash)
				{
					if (collisionSource.hasInflictedDamage)
					{
						var ricochetDamage = collisionSource.inflictedDamage.f * DataShortcuts.sim.ricochetOnDashDamageScalar;
						collisionSource.ReplaceInflictedDamage(ricochetDamage);
					}
					if (collisionSource.hasInflictedConcussion)
					{
						var ricochetConcussion = collisionSource.inflictedConcussion.f * DataShortcuts.sim.ricochetOnDashDamageScalar;
						collisionSource.ReplaceInflictedConcussion(ricochetConcussion);
					}
					doRicochet = true;
				}

				if (unit != null && !unit.hasEntityLinkPilot && DataShortcuts.sim.ricochetOnEjected)
				{
					doRicochet = true;
				}
			}

			if (doRicochet)
			{
				return Ricochet(deltaTime, collisionEvent, collisionSource, collidedEntity, damageFalloff);
			}

			if (subsystemProjectile?.visual?.impact != null)
			{
				var impact = subsystemProjectile.visual.impact;
				var key = !impact.factionUsed || isFriendly ? impact.key : impact.keyEnemy;
				if (!string.IsNullOrEmpty(key))
				{
					var position = collisionEvent.collisionContactPoint.position;
					var normal = collisionEvent.collisionContactPoint.normal;
					AssetPoolUtility.ActivateInstance(key, position, normal, impact.scale);
				}
			}

			if (!collisionSource.isDamageSplash && (collisionSource.hasInflictedDamage || collisionSource.hasInflictedConcussion || collisionSource.hasInflictedHeat || collisionSource.hasInflictedStagger))
			{
				var damage = (collisionSource.hasInflictedDamage ? collisionSource.inflictedDamage.f : 0f) * damageFalloff;
				var damageEntity = DamageEntity(collisionEvent, collisionSource, collidedEntity, damage, damageFalloff, false);
				RecordLastStrike(damageEntity);
				if (!string.IsNullOrEmpty(subsystemProjectile?.audio?.onImpact))
				{
					damageEntity.AddImpactSound(subsystemProjectile.audio.onImpact);
				}
			}

			if (!collisionSource.ImpactSplashOnDamage)
			{
				collisionSource.isImpactSplash = false;
			}

			return true;
		}

		private bool Ricochet(
			float deltaTime,
			CombatEntity collisionEvent,
			CombatEntity collisionSource,
			CombatEntity collidedEntity,
			float damageFalloff)
		{
			var spent = false;
			var position = collisionEvent.collisionContactPoint.position;
			var normal = collisionEvent.collisionContactPoint.normal;
			AssetPoolUtility.ActivateInstance("fx_hit_unit_ricochet", position, normal);
			var currentSpeed = collisionSource.movementSpeedCurrent.f;
			var speedFactor = Mathf.Clamp01(1f - DataShortcuts.sim.ricochetVelocityDecay);
			collisionSource.ReplaceMovementSpeedCurrent(currentSpeed * speedFactor);
			if (collisionSource.hasScale)
			{
				var scale = collisionSource.scale.v;
				scale.z *= DataShortcuts.sim.ricochetVelocityDecay;
				collisionSource.ReplaceScale(scale);
			}

			if (Mathf.Approximately(speedFactor, 0f))
			{
				spent = true;
			}

			var hasDamage = collisionSource.hasInflictedDamage || collisionSource.hasInflictedConcussion || collisionSource.hasInflictedHeat || collisionSource.hasInflictedStagger;
			if (!collisionSource.isDamageSplash && hasDamage)
			{
				var damage = (collisionSource.hasInflictedDamage ? collisionSource.inflictedDamage.f : 0f) * damageFalloff;
				var damageEvent = DamageEntity(collisionEvent, collisionSource, collidedEntity, damage, damageFalloff, true);
				RecordLastStrike(damageEvent);
				var scale = Mathf.Clamp01(1f - Mathf.Clamp01(DataShortcuts.sim.ricochetDamageScalar));
				if (collisionSource.hasInflictedDamage)
				{
					collisionSource.ReplaceInflictedDamage(collisionSource.inflictedDamage.f * scale);
				}
				if (collisionSource.hasInflictedConcussion)
				{
					collisionSource.ReplaceInflictedConcussion(collisionSource.inflictedConcussion.f * scale);
				}
				if (collisionSource.hasInflictedHeat)
				{
					collisionSource.ReplaceInflictedHeat(collisionSource.inflictedHeat.f * scale);
				}
				if (collisionSource.hasInflictedImpact)
				{
					collisionSource.ReplaceInflictedImpact(collisionSource.inflictedImpact.f * scale);
				}
				if (collisionSource.hasInflictedStagger)
				{
					collisionSource.ReplaceInflictedStagger(collisionSource.inflictedStagger.f * scale);
				}
			}

			var v1 = collisionSource.facing.v;
			var quaternion = Quaternion.Euler(Random.insideUnitSphere * DataShortcuts.sim.ricochetScatterDegrees);
			var vector3 = Quaternion.LookRotation(v1) * quaternion * Vector3.forward;
			collisionSource.ReplaceFacing(vector3);
			collisionSource.ReplaceRotation(Quaternion.LookRotation(vector3));
			collisionSource.ReplacePosition(collisionSource.position.v + deltaTime * currentSpeed * vector3);
			collisionSource.ReplaceSimpleForce(DataShortcuts.sim.gravityForce * DataShortcuts.sim.ricochetGravityScalar);
			collisionSource.SimpleMovement = true;
			if (collisionSource.hasRicochetChance)
			{
				var ricochetChance = collisionSource.ricochetChance.f * 0.5;
				if (ricochetChance < 0.10000000149011612)
				{
					collisionSource.RemoveRicochetChance();
				}
				else
				{
					collisionSource.ReplaceRicochetChance((float)ricochetChance);
				}
			}

			return spent;
		}

		private static bool TryHitEnvironment(
			float deltaTime,
			CombatEntity collisionEvent,
			CombatEntity collisionSource,
			DataBlockSubsystemProjectile_V2 subsystemProjectile,
			GameObject gameObject,
			bool isFriendly,
			bool hasMovementSpeed,
			float damageFalloff,
			bool ricochetPossible)
		{
			var collisionCosine = Mathf.Abs(Vector3.Dot(-collisionSource.facing.v, collisionEvent.collisionContactPoint.normal));
			var doRicochet = hasMovementSpeed && ricochetPossible && collisionCosine < 0.25f;
			var contactPosition = collisionEvent.collisionContactPoint.position;
			var normalized = Vector3.Lerp(collisionSource.facing.v, collisionEvent.collisionContactPoint.normal, 0.5f).normalized;
			var exactTileset = CombatSceneHelper.ins.areaManager.GetExactTileset(contactPosition);

			if (doRicochet)
			{
				AudioUtility.CreateAudioEvent("ricochet", collisionEvent.collisionContactPoint.position);
				AssetPoolUtility.ActivateInstance(exactTileset.fxNameHit, contactPosition, normalized);
				var direction = Vector3.Reflect(collisionSource.facing.v, collisionEvent.collisionContactPoint.normal);
				var facing = Quaternion.Euler(Random.Range(-5f, 5f), Random.Range(-5f, 5f), Random.Range(-5f, 5f)) * direction;
				var currentSpeed = collisionSource.movementSpeedCurrent.f;
				collisionSource.ReplaceFacing(facing.normalized);
				collisionSource.ReplacePosition(collisionSource.position.v + deltaTime * currentSpeed * facing);
				collisionSource.ReplaceMovementSpeedCurrent(currentSpeed * 0.66f);
				collisionSource.ReplaceSimpleForce(DataShortcuts.sim.gravityForce * 2f);
				collisionSource.SimpleFaceMotion = true;
				if (collisionSource.hasRicochetChance)
				{
					var ricochetChance = collisionSource.ricochetChance.f * 0.5;
					if (ricochetChance < 0.10000000149011612)
					{
						collisionSource.RemoveRicochetChance();
					}
					else
					{
						collisionSource.ReplaceRicochetChance((float)ricochetChance);
					}
				}
				return false;
			}

			AudioUtility.CreateAudioEvent("impact_bullet_rock", contactPosition);
			AssetPoolUtility.ActivateInstance(exactTileset.fxNameHit, contactPosition, normalized);
			if (DataShortcuts.sim.environmentCollisionDebug)
			{
				Debug.DrawLine(contactPosition, contactPosition - collisionSource.facing.v * 3f, exactTileset.hardness > 0.5f ? Color.red : Color.green, 5f);
			}

			if (!string.IsNullOrEmpty(exactTileset.sfxNameImpact) && hasMovementSpeed)
			{
				var currentSpeed = collisionSource.movementSpeedCurrent.f;
				AudioUtility.CreateAudioEvent(exactTileset.sfxNameImpact, contactPosition).AddAudioSyncUpdate("velocity", Mathf.InverseLerp(0.0f, DataShortcuts.sim.mximumProjectileSpeed, currentSpeed));
			}

			if (subsystemProjectile?.visual?.impact != null)
			{
				var impact = subsystemProjectile.visual.impact;
				var key = !impact.factionUsed || isFriendly ? impact.key : impact.keyEnemy;
				if (!string.IsNullOrEmpty(key))
				{
					var position = collisionEvent.collisionContactPoint.position;
					var normal = collisionEvent.collisionContactPoint.normal;
					AssetPoolUtility.ActivateInstance(key, position, normal, impact.scale);
				}
			}

			if (exactTileset.destructible && collisionSource.hasInflictedImpact && !collisionSource.isImpactSplash)
			{
				var position = gameObject.transform.position;
				var impact = collisionSource.inflictedImpact.f * Mathf.Clamp(damageFalloff, DataLinker<DataContainerSettingsSimulation>.data.minimumEnvironmentalDamageFalloffThreshold, 1f);
				if (collisionEvent.hasCollisionDamageScale)
				{
					// Impact damage is not adjusted by the damage scale in the original code.
					impact *= collisionEvent.collisionDamageScale.f;
				}
				CombatSceneHelper.ins.areaManager.ApplyDamageToPosition(position, impact);
				if (DataShortcuts.sim.environmentCollisionDebug)
				{
					Debug.DrawLine(contactPosition, position, Color.yellow, 5f);
				}
			}

			return true;
		}

		private CombatEntity DamageEntity(
		  CombatEntity collisionEvent,
		  CombatEntity collisionSource,
		  CombatEntity damagedEntity,
		  float damage,
		  float damageFalloff,
		  bool ricochet)
		{
			if (damage <= 0f)
			{
				damage = 1f;
			}

			var entity = combat.CreateEntity();
			entity.isDamageEvent = true;
			entity.AddDamageGroup(PhantomBrigade.Combat.Components.DamageEventGroup.ContactStandard);
			entity.AddSourceEntity(collisionSource.id.id);
			entity.AddEventTargetEntity(damagedEntity.id.id);
			entity.AddEventSourceEntity(collisionSource.sourceEntity.combatID);
			var facing = damagedEntity.hasCollisionImpactVector
				? damagedEntity.collisionImpactVector.v
				: -collisionSource.facing.v;
			entity.AddFacing(facing);
			entity.AddPosition(collisionEvent.collisionContactPoint.position);

			if (collisionSource.hasLevel)
			{
				entity.AddLevel(collisionSource.level.i);
			}

			if (collisionEvent.hasTriggerSource)
			{
				entity.AddTriggerSource(collisionEvent.triggerSource.c);
			}

			entity.isDamageDispersed = collisionSource.isDamageDispersed;
			entity.isDamageSplash = collisionSource.isDamageSplash;
			entity.isDamageOptimal = damageFalloff > 0.5f;
			entity.isDamageFromRicochet = ricochet;
			entity.isInflictedDamageNormalized = collisionSource.isInflictedDamageNormalized;

			var scale = collisionEvent.hasCollisionDamageScale ? collisionEvent.collisionDamageScale.f : 1f;
			if (ricochet)
			{
				scale *= Mathf.Clamp01(DataShortcuts.sim.ricochetDamageScalar);
			}

			if (collisionSource.hasInflictedConcussion)
			{
				entity.AddInflictedConcussion(collisionSource.inflictedConcussion.f * damageFalloff * scale);
			}

			if (collisionSource.hasInflictedHeat)
			{
				entity.AddInflictedHeat(collisionSource.inflictedHeat.f * damageFalloff * scale);
			}

			if (collisionSource.hasInflictedStagger)
			{
				entity.AddInflictedStagger(collisionSource.inflictedStagger.f * damageFalloff * scale);
			}

			entity.AddInflictedDamage(damage * scale);

			return entity;
		}

		private void RecordLastStrike(CombatEntity damageEvent)
		{
			var target = damageEvent.eventTargetEntity.combatID;
			var combatant = IDUtility.GetCombatEntity(target);
			var unit = IDUtility.GetLinkedPersistentEntity(combatant);
			var pilot = IDUtility.GetLinkedPilot(unit);
			if (pilot == null)
			{
				return;
			}
			if (pilot.isEjected || pilot.isDeceased)
			{
				return;
			}

			var source = damageEvent.eventSourceEntity.combatID;
			foreach (var entity in ekCombat.GetEntities())
			{
				if (!entity.hasLastStrike)
				{
					continue;
				}
				if (entity.lastStrike.combatID == target)
				{
					if (entity.lastStrike.sourceID != source)
					{
						Debug.Log($"Switching last strike on C-{target} to C-{source}");
						entity.lastStrike.sourceID = source;
					}
					return;
				}
			}

			Debug.Log($"Adding last strike on C-{target} with source C-{source}");
			var ekEntity = ekCombat.CreateEntity();
			ekEntity.AddLastStrike(source, target, pilot.id.id);
		}
	}
}
