using HarmonyLib;

using PhantomBrigade;
using PhantomBrigade.AIOverworld.BT;
using PBBTAction_MoveToEntity = PhantomBrigade.AIOverworld.BT.Nodes.BTAction_MoveToEntity;

using UnityEngine;

namespace EchKode.PBMods.Fixes
{
	// Code for Patch class:
	//
	//[HarmonyPatch(typeof(PBBTAction_MoveToEntity), "OnUpdate")]
	//[HarmonyPrefix]
	//static bool Btamte_OnUpdatePrefix(ref BTStatus __result, PBBTAction_MoveToEntity __instance, OverworldEntity self)
	//{
	//	__result = BTAction_MoveToEntity.OnUpdate(__instance, self);
	//	return false;
	//}

	static class BTAction_MoveToEntity
	{
		internal static BTStatus OnUpdate(PBBTAction_MoveToEntity inst, OverworldEntity self)
		{
			if (!self.hasAgentEntityBlackboard)
			{
				return BTStatus.Error;
			}

			var (ok, position) = GetTargetEntityPosition(inst, self);
			if (!ok)
			{
				position = GetFallbackPosition(inst, self);
			}
			UpdateOutputPosition(inst, self, position);

			return BTStatus.Success;
		}

		private static (bool, Vector3) GetTargetEntityPosition(PBBTAction_MoveToEntity inst, OverworldEntity self)
		{
			var t = Traverse.Create(inst);
			var entityKey = t.Field<string>("entityKey").Value;
			if (!self.agentEntityBlackboard.entityData.ContainsKey(entityKey))
			{
				return (false, Vector3.zero);
			}
			var target = IDUtility.GetOverworldEntity(self.agentEntityBlackboard.entityData[entityKey]);
			if (target == null)
			{
				// original: null check was happening too late.
				SendMessageOnNoTargetEntity(self);
				return (false, Vector3.zero);
			}

			SendMessageOnFriendlyProvince(self, target);

			var position = t.Method("GetClosingPosition", target.position.v, self.position.v).GetValue<Vector3>();
			return (true, position);
		}

		private static void SendMessageOnNoTargetEntity(OverworldEntity self)
		{
			if (!self.hasAgentStateMachine)
			{
				return;
			}
			OverworldFSMUtil.QueueMessage(self, "movement_complete", false);
		}

		private static void SendMessageOnFriendlyProvince(OverworldEntity self, OverworldEntity target)
		{
			if (!self.hasAgentStateMachine)
			{
				return;
			}
			if (!target.hasProvinceCurrent)
			{
				return;
			}

			var pe = IDUtility.GetPersistentEntity(target.provinceCurrent.persistentID);
			if (pe == null)
			{
				return;
			}
			if (!CombatUIUtility.IsFactionFriendly(pe.hasFaction ? pe.faction.s : "unknown"))
			{
				return;
			}

			OverworldFSMUtil.QueueMessage(self, "player_lost", false);
			OverworldFSMUtil.QueueMessage(self, "timer_done", false);
		}

		private static Vector3 GetFallbackPosition(PBBTAction_MoveToEntity inst, OverworldEntity self)
		{
			var t = Traverse.Create(inst);
			var fallbackPositionKey = t.Field<string>("fallbackPositionKey").Value;
			if (!self.agentDataBlackboard.vectorData.ContainsKey(fallbackPositionKey))
			{
				return self.position.v + self.rotation.q * Vector3.forward * 20f;
			}

			var fallbackPosition = self.agentDataBlackboard.vectorData[fallbackPositionKey];
			return t.Method("GetClosingPosition", fallbackPosition, self.position.v).GetValue<Vector3>();
		}

		private static void UpdateOutputPosition(
			PBBTAction_MoveToEntity inst,
			OverworldEntity self,
			Vector3 position)
		{
			var t = Traverse.Create(inst);
			var outputKey = t.Field<string>("outputKey").Value;
			if (self.agentDataBlackboard.vectorData.ContainsKey(outputKey))
			{
				self.agentDataBlackboard.vectorData[outputKey] = position;
				return;
			}
			self.agentDataBlackboard.vectorData.Add(outputKey, position);
		}
	}
}
