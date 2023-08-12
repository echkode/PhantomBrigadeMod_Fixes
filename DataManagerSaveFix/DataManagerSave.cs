using System.Collections.Generic;

using PhantomBrigade;
using PhantomBrigade.Data;
using PhantomBrigade.Overworld.Components;

using UnityEngine;

namespace EchKode.PBMods.DataManagerSaveFix
{
	static class DataManagerSave
	{
		internal static void SaveAIData(
			OverworldEntity sourceEntity,
			DataContainerSavedOverworldEntity targetData)
		{
			var ai = new DataBlockAI();
			SaveStateMachineData(sourceEntity, ai);
			SaveOrders(sourceEntity, ai);
			SaveBlackboards(sourceEntity, ai);
			SaveRole(sourceEntity, ai);
			targetData.aiData = ai;
		}

		private static void SaveStateMachineData(OverworldEntity sourceEntity, DataBlockAI ai)
		{
			if (!sourceEntity.hasAgentStateMachine)
			{
				return;
			}

			var stateMachineC = sourceEntity.agentStateMachine;
			if (stateMachineC.stateMachine == null)
			{
				return;
			}

			ai.fsmName = stateMachineC.stateMachine.Name;
			ai.fsmState = stateMachineC.stateMachine.StateIdToName(stateMachineC.currentStateId);

			var overworld = Contexts.sharedInstance.overworld;
			if (!overworld.hasStateMachineMessages)
			{
				return;
			}

			ai.fsmMessages = new List<DataBlockFSMMessage>();
			foreach (var msgInfo in overworld.stateMachineMessages.messagesTimed)
			{
				ProcessMessage(
					overworld,
					sourceEntity,
					stateMachineC,
					msgInfo,
					ai);
			}
			foreach (var msgInfo in overworld.stateMachineMessages.messagesToAdd)
			{
				ProcessMessage(
					overworld,
					sourceEntity,
					stateMachineC,
					msgInfo,
					ai);
			}
		}

		private static void ProcessMessage(
			OverworldContext overworld,
			OverworldEntity sourceEntity,
			AgentStateMachine stateMachineC,
			StateMachineMessages.MessageInfo msgInfo,
			DataBlockAI ai)
		{
			if (!msgInfo.CheckIsMessageValid(sourceEntity.id.id, stateMachineC.stateGenId))
			{
				return;
			}

			var dataBlockFsmMessage = new DataBlockFSMMessage
			{
				timer = msgInfo.timestamp - overworld.simulationTime.f,
				msg = msgInfo.msg,
				stateSpecific = msgInfo.IsStateSpecific
			};
			ai.fsmMessages.Add(dataBlockFsmMessage);
		}

		private static void SaveOrders(OverworldEntity sourceEntity, DataBlockAI ai)
		{
			if (sourceEntity.hasCurrentAIOrder)
			{
				ai.currentOrder = sourceEntity.currentAIOrder.behaviorKey;
			}
			if (sourceEntity.hasPreviousAIOrder)
			{
				ai.previousOrder = sourceEntity.previousAIOrder.behaviorKey;
			}
		}

		private static void SaveBlackboards(OverworldEntity sourceEntity, DataBlockAI ai)
		{
			SaveDataBlackboard(sourceEntity, ai);
			SaveEntityBlackboard(sourceEntity, ai);
		}

		private static void SaveDataBlackboard(OverworldEntity sourceEntity, DataBlockAI ai)
		{
			if (!sourceEntity.hasAgentDataBlackboard)
			{
				return;
			}
			ai.dataBlackboardFloat = new SortedDictionary<string, float>(sourceEntity.agentDataBlackboard.floatData);
			ai.dataBlackboardVector = new SortedDictionary<string, Vector3>(sourceEntity.agentDataBlackboard.vectorData);
		}

		private static void SaveEntityBlackboard(OverworldEntity sourceEntity, DataBlockAI ai)
		{
			if (!sourceEntity.hasAgentEntityBlackboard)
			{
				return;
			}

			var entries = new SortedDictionary<string, string>();
			foreach (var kvp in sourceEntity.agentEntityBlackboard.entityData)
			{
				var overworldEntity = IDUtility.GetOverworldEntity(kvp.Value);
				// FIX original: stored empty string on null or no internal name.
				if (overworldEntity == null)
				{
					continue;
				}
				if (!overworldEntity.hasNameInternal)
				{
					continue;
				}
				entries.Add(kvp.Key, overworldEntity.nameInternal.s);
			}
			ai.entityBlackboardNames = entries;
		}

		private static void SaveRole(OverworldEntity sourceEntity, DataBlockAI ai)
		{
			var role = sourceEntity.isAssaultSquad
				? "assault"
				: sourceEntity.isPatrolSquad
					? "patrol"
					: sourceEntity.isResponseSquad
						? "response"
						: sourceEntity.isConvoySquad
							? "convoy"
							: "";
			if (!string.IsNullOrEmpty(role))
			{
				ai.role = role;
			}
		}
	}
}