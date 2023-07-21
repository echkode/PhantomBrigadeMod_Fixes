// Copyright (c) 2022 EchKode
// SPDX-License-Identifier: BSD-3-Clause

using PhantomBrigade;
using PhantomBrigade.Overworld;

using UnityEngine;

namespace EchKode.PBMods.Fixes
{
	// Code for Patch class:
	//
	//[HarmonyPatch(typeof(PhantomBrigade.Combat.CombatActionEvent), "OnEjection")]
	//[HarmonyPrefix]
	//static void Cae_OnEjectionPrefix(CombatEntity unitCombat, out CombatActionEvent.UnitPilotPair __state)
	//{
	//	CombatActionEvent.OnEjectionPrologue(unitCombat, out __state);
	//}
	//
	//[HarmonyPatch(typeof(PhantomBrigade.Combat.CombatActionEvent), "OnEjection")]
	//[HarmonyPostfix]
	//static void Cae_OnEjectionPostfix(CombatActionEvent.UnitPilotPair __state)
	//{
	//	CombatActionEvent.OnEjectionEpilogue(__state);
		// Occasionally pilotless units are left in the tab order. I suspect it happens when an enemy pilot
		// ejects the turn after being damaged to the point that the AI will trigger an ejection.
		//CIViewCombatMode.ins.RedrawUnitTabs();
	//}

	internal static class CombatActionEvent
	{
		internal sealed class UnitPilotPair
		{
			public int CombatID;
			public int PilotID;
		}

		internal static void OnEjectionPrologue(CombatEntity combatant, out UnitPilotPair pair)
		{
			Debug.LogFormat("Pilot ejecting for unit C-{0}", combatant.id.id);
			var unit = IDUtility.GetLinkedPersistentEntity(combatant);
			var pilot = IDUtility.GetLinkedPilot(unit);
			pair = new UnitPilotPair()
			{
				CombatID = combatant.id.id,
				PilotID = pilot?.id.id ?? -99,
			};
		}

		internal static void OnEjectionEpilogue(UnitPilotPair pair)
		{
			if (pair.PilotID == -99)
			{
				Debug.Log("OnEjection of a non-pilot");
				return;
			}

			Debug.LogFormat("Handling credit for ejecting pilot from C-{0}", pair.CombatID);
			foreach (var entity in ECS.Contexts.sharedInstance.ekCombat.GetEntities())
			{
				if (!entity.hasLastStrike)
				{
					continue;
				}
				if (entity.lastStrike.combatID != pair.CombatID)
				{
					continue;
				}

				Debug.Log("Last strike found");
				var ejectedPilot = IDUtility.GetPersistentEntity(pair.PilotID);
				if (ejectedPilot == null)
				{
					Debug.Log("No pilot entity");
					return;
				}
				if (ejectedPilot.isDeceased)
				{
					Debug.Log("Pilot is deceased, no credit");
					return;
				}

				var combatant = IDUtility.GetCombatEntity(entity.lastStrike.sourceID);
				var sourceUnit = IDUtility.GetLinkedPersistentEntity(combatant);
				var pilot = IDUtility.GetLinkedPilot(sourceUnit);
				if (pilot == null)
				{
					var unitID = sourceUnit?.ToLog() ?? "<unknown>";
					Debug.LogFormat("Unable to locate entity for pilot of unit {0}", unitID);
					return;
				}

				Debug.LogFormat(
					"CombatActionEvent | OnEjection -- crediting pilot {0} with forced ejection of pilot {1}",
					pilot.ToLog(),
					ejectedPilot.ToLog());
				pilot.IncrementMemory("pilot_auto_combat_takedowns");
				return;
			}

			Debug.Log("Last strike record not found");
		}
	}
}
