// Copyright (c) 2023 EchKode
// SPDX-License-Identifier: BSD-3-Clause

using System.Collections.Generic;

using PhantomBrigade;
using PhantomBrigade.Data;

using UnityEngine;

namespace EchKode.PBMods.InputCombatMeleeUtilityFix
{
	static class CombatUIUtility
	{
		[System.Flags]
		internal enum ActionOverlapCheck
		{
			None = 0,
			PrimaryTrack = 1,
			SecondaryTrack = 2,
			BothTracks = PrimaryTrack | SecondaryTrack,
		}

		internal static (bool, float) TryPlaceAction(
			int ownerID,
			DataContainerAction actionData,
			float startTime,
			float duration,
			ActionOverlapCheck overlapCheck)
		{
			if (overlapCheck == ActionOverlapCheck.None)
			{
				return CheckPlacementTime(startTime);
			}

			var primaryTrackOnly = overlapCheck == ActionOverlapCheck.PrimaryTrack;
			if (primaryTrackOnly && actionData.dataCore.trackType == TrackType.Secondary)
			{
				return CheckPlacementTime(startTime);
			}

			var secondaryTrackOnly = overlapCheck == ActionOverlapCheck.SecondaryTrack;
			if (secondaryTrackOnly && actionData.dataCore.trackType == TrackType.Primary)
			{
				return CheckPlacementTime(startTime);
			}

			var owner = IDUtility.GetCombatEntity(ownerID);
			if (owner == null)
			{
				Debug.LogWarningFormat(
					"Mod {0} ({1}) TryPlaceAction unable to resolve combat ID to entity | ID: C-{2}",
					ModLink.modIndex,
					ModLink.modID,
					ownerID);
				return (false, 0f);
			}

			var endTime = startTime + duration;
			var actions = new List<ActionEntity>(Contexts.sharedInstance.action.GetEntitiesWithActionOwner(ownerID));
			actions.Sort(SortByStartTime);
			foreach (var action in actions)
			{
				// Weed out single-track actions if we're not checking that track.
				if (primaryTrackOnly && !action.isOnPrimaryTrack)
				{
					continue;
				}
				if (secondaryTrackOnly && !action.isOnSecondaryTrack)
				{
					continue;
				}

				var actionStartTime = action.startTime.f;
				if (endTime < actionStartTime)
				{
					break;
				}

				var actionEndTime = actionStartTime + action.duration.f;
				if (actionEndTime < startTime)
				{
					continue;
				}

				startTime = actionEndTime;
				endTime = startTime + duration;
			}

			return CheckPlacementTime(startTime);
		}

		static (bool, float) CheckPlacementTime(float startTime)
		{
			var combat = Contexts.sharedInstance.combat;
			var maxPlacementTime = combat.currentTurn.i * combat.turnLength.i + DataShortcuts.sim.maxActionTimePlacement;
			if (startTime < maxPlacementTime)
			{
				return (true, startTime);
			}
			return (false, 0f);
		}

		static int SortByStartTime(ActionEntity lhs, ActionEntity rhs) => lhs.startTime.f.CompareTo(rhs.startTime.f);
	}
}
