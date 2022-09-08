// Copyright (c) 2022 EchKode
// SPDX-License-Identifier: BSD-3-Clause

using PhantomBrigade;
using PhantomBrigade.Overworld;

using UnityEngine;

namespace EchKode.PBMods.Fixes
{
	internal static class CombatActionEvent
	{
		internal sealed class UnitPilotPair
		{
			public int CombatID;
			public int PilotID;
		}

		internal static void OnEjectionPrologue(CombatEntity combatant, out UnitPilotPair pair)
		{
			Debug.Log($"Pilot ejecting for unit C-{combatant.id.id}");
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

			Debug.Log($"Handling credit for ejecting pilot from C-{pair.CombatID}");
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
					Debug.Log($"Unable to locate entity for pilot of unit {unitID}");
					return;
				}

				Debug.Log($"CombatActionEvent | OnEjection -- crediting pilot {pilot.ToLog()} with forced ejection of pilot {ejectedPilot.ToLog()}");
				pilot.IncrementMemory("pilot_auto_combat_takedowns");
				return;
			}

			Debug.Log("Last strike record not found");
		}
	}
}
