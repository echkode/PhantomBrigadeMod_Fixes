// Copyright (c) 2023 EchKode
// SPDX-License-Identifier: BSD-3-Clause

using System.Collections.Generic;

using HarmonyLib;

using PhantomBrigade;
using PhantomBrigade.Data;

using UnityEngine;

namespace EchKode.PBMods.CIViewCombatTimelineFix
{
	internal sealed class CIViewCombatTimelineFix : CIViewCombatTimeline
	{
		internal static bool Log;

		internal static void AdjustTimelineRegions(
			CIViewCombatTimeline inst,
			int actionIDSelected,
			float actionStartTime,
			float offsetSelected)
		{
			var t = Traverse.Create(inst);
			if (!t.Field<bool>("timelineRegionDragged").Value)
			{
				return;
			}
			if (offsetSelected == 0f)
			{
				return;
			}

			var selectedIndex = ClearRegionChanges(t, actionIDSelected);
			if (selectedIndex == -1)
			{
				Debug.LogWarningFormat(
					"Mod {0} ({1}) AdjustTimelineRegions cannot find matching region for action | action: A-{2} | start time: {3:F3}s | offsetSelected: {4:F3}s",
					ModLink.modIndex,
					ModLink.modID,
					actionIDSelected,
					actionStartTime,
					offsetSelected);
				return;
			}

			if (offsetSelected < 0f)
			{
				AdjustEarlierRegions(
					t,
					actionStartTime,
					offsetSelected,
					selectedIndex);
			}
			else
			{
				AdjustLaterRegions(
					t,
					actionStartTime,
					offsetSelected,
					selectedIndex);
			}

			if (Log)
			{
				DumpModifiedRegions(t);
			}

			RepositionActionButtons(inst, t);
		}

		static int ClearRegionChanges(Traverse t, int actionIDSelected)
		{
			var selectedRegionIndex = -1;

			var timelineRegions = t.Field<List<TimelineRegion>>("timelineRegions").Value;
			timelineRegions.Clear();

			var timelineRegionsModified = t.Field<List<TimelineRegion>>("timelineRegionsModified").Value;
			if (timelineRegionsModified.Count == 0)
			{
				return selectedRegionIndex;
			}

			timelineRegionsModified.Sort((x, y) => x.startTime.CompareTo(y.startTime));
			timelineRegions.AddRange(timelineRegionsModified);

			for (var i = 0; i < timelineRegions.Count; i += 1)
			{
				var region = timelineRegions[i];
				region.changed = false;
				region.offset = 0f;
				timelineRegions[i] = region;
				timelineRegionsModified[i] = region;
				if (actionIDSelected == region.actionID)
				{
					selectedRegionIndex = i;
					t.Field<TimelineRegion>("timelineRegionSelected").Value = region;
				}
			}

			return selectedRegionIndex;
		}

		static void AdjustEarlierRegions(
			Traverse t,
			float actionStartTime,
			float offsetSelected,
			int selectedIndex)
		{
			var selectedRegion = AdjustSelectedRegion(
				t,
				actionStartTime,
				offsetSelected,
				selectedIndex,
				GetMinStartTimes,
				Mathf.Max,
				CorrectEarlierOffset);
			if (!selectedRegion.changed)
			{
				return;
			}
			if (selectedRegion.locked)
			{
				return;
			}

			var timelineRegionsModified = t.Field<List<TimelineRegion>>("timelineRegionsModified").Value;
			var primaryTrackIndex = selectedRegion.primary ? selectedIndex : -1;
			var secondaryTrackIndex = selectedRegion.secondary ? selectedIndex : -1;

			for (var i = selectedIndex - 1; i >= 0; i -= 1)
			{
				var region = timelineRegionsModified[i];
				if (region.locked)
				{
					break;
				}

				if (region.primary && region.secondary)
				{
					var combat = Contexts.sharedInstance.combat;
					var maxTime = (float)combat.currentTurn.i * combat.turnLength.i + DataShortcuts.sim.maxActionTimePlacement;
					var primaryStartTime = primaryTrackIndex != - 1
						? timelineRegionsModified[primaryTrackIndex].startTime
						: maxTime;
					var secondaryStartTime = secondaryTrackIndex != -1
						? timelineRegionsModified[secondaryTrackIndex].startTime
						: maxTime;
					var startTime = Mathf.Min(primaryStartTime, secondaryStartTime);
					var offset = startTime - region.endTime;

					if (offset > 0f || offset.RoughlyEqual(0f, 0.0005f))
					{
						break;
					}

					region.startTime += offset;
					region.offset = offset;
					region.changed = true;
					timelineRegionsModified[i] = region;

					primaryTrackIndex = i;
					secondaryTrackIndex = i;
				}
				else if (region.primary && primaryTrackIndex != -1)
				{
					primaryTrackIndex = PushRegionEarlier(
						region,
						i,
						timelineRegionsModified,
						primaryTrackIndex);
				}
				else if (region.secondary && secondaryTrackIndex != -1)
				{
					secondaryTrackIndex = PushRegionEarlier(
						region,
						i,
						timelineRegionsModified,
						secondaryTrackIndex);
				}

			}
		}

		static int PushRegionEarlier(
			TimelineRegion region,
			int index,
			List<TimelineRegion> timelineRegionsModified,
			int trackIndex)
		{
			var adjustedRegion = timelineRegionsModified[trackIndex];
			var offset = adjustedRegion.startTime - region.endTime;
			if (offset > 0f || offset.RoughlyEqual(0f, 0.0005f))
			{
				return trackIndex;
			}

			region.startTime += offset;
			region.offset = offset;
			region.changed = true;
			timelineRegionsModified[index] = region;

			return index;
		}

		static void AdjustLaterRegions(
			Traverse t,
			float actionStartTime,
			float offsetSelected,
			int selectedIndex)
		{
			var selectedRegion = AdjustSelectedRegion(
				t,
				actionStartTime,
				offsetSelected,
				selectedIndex,
				GetMaxStartTimes,
				Mathf.Min,
				CorrectLaterOffset);
			if (!selectedRegion.changed)
			{
				return;
			}
			if (selectedRegion.locked)
			{
				return;
			}

			var timelineRegionsModified = t.Field<List<TimelineRegion>>("timelineRegionsModified").Value;
			var primaryTrackIndex = selectedRegion.primary ? selectedIndex : -1;
			var secondaryTrackIndex = selectedRegion.secondary ? selectedIndex : -1;

			for (var i = selectedIndex + 1; i < timelineRegionsModified.Count; i += 1)
			{
				var region = timelineRegionsModified[i];
				if (region.locked)
				{
					break;
				}

				if (region.primary && region.secondary)
				{
					var combat = Contexts.sharedInstance.combat;
					var minTime = (float)combat.currentTurn.i * combat.turnLength.i;
					var primaryEndTime = primaryTrackIndex != -1
						? timelineRegionsModified[primaryTrackIndex].endTime
						: minTime;
					var secondaryEndTime = secondaryTrackIndex != -1
						? timelineRegionsModified[secondaryTrackIndex].endTime
						: minTime;
					var endTime = Mathf.Max(primaryEndTime, secondaryEndTime);
					var offset = endTime - region.startTime;

					if (offset < 0f || offset.RoughlyEqual(0f, 0.0005f))
					{
						break;
					}

					region.startTime += offset;
					region.offset = offset;
					region.changed = true;
					timelineRegionsModified[i] = region;

					primaryTrackIndex = i;
					secondaryTrackIndex = i;
				}
				else if (region.primary && primaryTrackIndex != -1)
				{
					primaryTrackIndex = PushRegionLater(
						region,
						i,
						timelineRegionsModified,
						primaryTrackIndex);
				}
				else if (region.secondary && secondaryTrackIndex != -1)
				{
					secondaryTrackIndex = PushRegionLater(
						region,
						i,
						timelineRegionsModified,
						secondaryTrackIndex);
				}

			}
		}

		static int PushRegionLater(
			TimelineRegion region,
			int index,
			List<TimelineRegion> timelineRegionsModified,
			int trackIndex)
		{
			var adjustedRegion = timelineRegionsModified[trackIndex];
			var offset = adjustedRegion.endTime - region.startTime;
			if (offset < 0f || offset.RoughlyEqual(0f, 0.0005f))
			{
				return trackIndex;
			}

			region.startTime += offset;
			region.offset = offset;
			region.changed = true;
			timelineRegionsModified[index] = region;

			return index;
		}

		static TimelineRegion AdjustSelectedRegion(
			Traverse t,
			float actionStartTime,
			float offsetSelected,
			int selectedIndex,
			System.Func<Traverse, int, System.ValueTuple<float, float>> getTimes,
			System.Func<float, float, float> minmax,
			System.Func<float, float, float, float> correctOffset)
		{
			var (primaryTime, secondaryTime) = getTimes(t, selectedIndex);
			var selectedRegion = t.Field<TimelineRegion>("timelineRegionSelected").Value;
			if (selectedRegion.locked)
			{
				return selectedRegion;
			}

			if (Log)
			{
				Debug.LogFormat(
					"Mod {0} ({1}) AdjustTimelineRegions adjust selected region | action: A-{2} | primary start limit: {3:F3}s | secondary start limit: {4:F3}s",
					ModLink.modIndex,
					ModLink.modID,
					selectedRegion.actionID,
					primaryTime,
					secondaryTime);
			}

			var startTime = actionStartTime;

			if (selectedRegion.primary && selectedRegion.secondary)
			{
				var minmaxTime = minmax(primaryTime, secondaryTime);
				startTime = minmax(minmaxTime, startTime);
			}
			else if (selectedRegion.primary)
			{
				startTime = minmax(primaryTime, startTime);
			}
			else if (selectedRegion.secondary)
			{
				startTime = minmax(secondaryTime, startTime);
			}
			else
			{
				return selectedRegion;
			}

			var offset = correctOffset(startTime, actionStartTime, offsetSelected);
			if (offset.RoughlyEqual(0f, 0.0005f))
			{
				return selectedRegion;
			}

			selectedRegion.startTime = startTime;
			selectedRegion.offset = offset;
			selectedRegion.changed = true;

			var timelineRegionsModified = t.Field<List<TimelineRegion>>("timelineRegionsModified").Value;
			timelineRegionsModified[selectedIndex] = selectedRegion;

			if (Log)
			{
				Debug.LogFormat(
					"Mod {0} ({1}) AdjustTimelineRegions adjust selected region | action: A-{2} | action start time: {3:F3}s | offsetSelected: {4:F3}s | region start time: {5:F3}s | region offset: {6:F3}s",
					ModLink.modIndex,
					ModLink.modID,
					selectedRegion.actionID,
					actionStartTime,
					offsetSelected,
					selectedRegion.startTime,
					selectedRegion.offset);
			}

			return selectedRegion;
		}

		static (float, float) GetMinStartTimes(Traverse t, int selectedIndex)
		{
			var combat = Contexts.sharedInstance.combat;
			var minPrimaryTime = (float)combat.currentTurn.i * combat.turnLength.i;
			var minSecondaryTime = minPrimaryTime;
			var timelineRegionsModified = t.Field<List<TimelineRegion>>("timelineRegionsModified").Value;
			var sb = Log ? new System.Text.StringBuilder() : null;

			if (Log)
			{
				sb.AppendFormat("GetMinStartTimes | count: {0}", timelineRegionsModified.Count);
				sb.AppendFormat("\n  0 {0:F3}s {1:F3}s",
					minPrimaryTime,
					minSecondaryTime);
			}
			for (var i = 0; i < selectedIndex; i += 1)
			{
				var region = timelineRegionsModified[i];

				if (region.primary && region.secondary)
				{
					var minTime = region.locked
						? region.startTime
						: Mathf.Max(minPrimaryTime, minSecondaryTime);
					minTime += region.duration;
					minPrimaryTime = minTime;
					minSecondaryTime = minTime;
					if (Log)
					{
						sb.AppendFormat("\n  {0} {1:F3}s {2:F3}s {3:F3}s D{4}",
							i,
							minPrimaryTime,
							minSecondaryTime,
							region.duration,
							region.locked ? "L" : "");
					}
					continue;
				}

				if (region.primary)
				{
					minPrimaryTime = region.locked
						? region.startTime
						: minPrimaryTime;
					minPrimaryTime += region.duration;
					if (Log)
					{
						sb.AppendFormat("\n  {0} {1:F3}s {2:F3}s {3:F3}s P{4}",
							i,
							minPrimaryTime,
							minSecondaryTime,
							region.duration,
							region.locked ? "L" : "");
					}
				}

				if (region.secondary)
				{
					minSecondaryTime = region.locked
						? region.startTime
						: minSecondaryTime;
					minSecondaryTime += region.duration;
					if (Log)
					{
						sb.AppendFormat("\n  {0} {1:F3}s {2:F3}s {3:F3}s S{4}",
							i,
							minPrimaryTime,
							minSecondaryTime,
							region.duration,
							region.locked ? "L" : "");
					}
				}
			}

			if (Log)
			{
				Debug.LogFormat(
					"Mod {0} ({1}) AdjustTimelineRegions {2}",
					ModLink.modIndex,
					ModLink.modID,
					sb);
			}

			return (minPrimaryTime, minSecondaryTime);
		}

		static (float, float) GetMaxStartTimes(Traverse t, int selectedIndex)
		{
			var combat = Contexts.sharedInstance.combat;
			var maxPrimaryTime = (float)combat.currentTurn.i * combat.turnLength.i + DataShortcuts.sim.maxActionTimePlacement;
			var maxSecondaryTime = maxPrimaryTime;
			var timelineRegionsModified = t.Field<List<TimelineRegion>>("timelineRegionsModified").Value;
			var lastPrimaryDuration = 0f;
			var lastSecondaryDuration = 0f;
			for (var i = timelineRegionsModified.Count - 1; i >= selectedIndex; i -= 1)
			{
				var region = timelineRegionsModified[i];
				if (region.primary && lastPrimaryDuration == 0f)
				{
					lastPrimaryDuration = region.duration;
					if (region.startTime > maxPrimaryTime)
					{
						Debug.LogWarningFormat(
							"Mod {0} ({1}) AdjustTimelineRegions last action after max time placement | action ID: {2} ({3}) | action start time: {4:F3}s | track: primary | max placement: {5:F3}s",
							ModLink.modIndex,
							ModLink.modID,
							region.actionID,
							region.actionKey,
							region.startTime,
							maxPrimaryTime);
						maxPrimaryTime = region.startTime;
					}
				}
				if (region.secondary && lastSecondaryDuration == 0f)
				{
					lastSecondaryDuration = region.duration;
					if (region.startTime > maxSecondaryTime)
					{
						Debug.LogWarningFormat(
							"Mod {0} ({1}) AdjustTimelineRegions last action after max time placement | action ID: {2} ({3}) | action start time: {4:F3}s | track: secondary | max placement: {5:F3}s",
							ModLink.modIndex,
							ModLink.modID,
							region.actionID,
							region.actionKey,
							region.startTime,
							maxSecondaryTime);
						maxSecondaryTime = region.startTime;
					}
				}
				if (lastPrimaryDuration != 0f && lastSecondaryDuration != 0f)
				{
					break;
				}
			}
			maxPrimaryTime += lastPrimaryDuration;
			maxSecondaryTime += lastSecondaryDuration;

			var sb = Log ? new System.Text.StringBuilder() : null;
			if (Log)
			{
				sb.AppendFormat("GetMaxStartTimes | count: {0}", timelineRegionsModified.Count);
				sb.AppendFormat("\n  {0} {1:F3}s {2:F3}s",
					timelineRegionsModified.Count,
					maxPrimaryTime,
					maxSecondaryTime);
			}

			for (var i = timelineRegionsModified.Count - 1; i > selectedIndex; i -= 1)
			{
				var region = timelineRegionsModified[i];

				if (region.primary && region.secondary)
				{
					var maxTime = region.locked
						? region.startTime
						: Mathf.Min(maxPrimaryTime, maxSecondaryTime) - region.duration;
					maxPrimaryTime = maxTime;
					maxSecondaryTime = maxTime;
					if (Log)
					{
						sb.AppendFormat("\n  {0} {1:F3}s {2:F3}s {3:F3}s D{4}",
							i,
							maxPrimaryTime,
							maxSecondaryTime,
							region.duration,
							region.locked ? "L" : "");
					}
					continue;
				}

				if (region.primary)
				{
					maxPrimaryTime = region.locked
						? region.startTime
						: maxPrimaryTime - region.duration;
					if (Log)
					{
						sb.AppendFormat("\n  {0} {1:F3}s {2:F3}s {3:F3}s P{4}",
							i,
							maxPrimaryTime,
							maxSecondaryTime,
							region.duration,
							region.locked ? "L" : "");
					}
				}

				if (region.secondary)
				{
					maxSecondaryTime = region.locked
						? region.startTime
						: maxSecondaryTime - region.duration;
					if (Log)
					{
						sb.AppendFormat("\n  {0} {1:F3}s {2:F3}s {3:F3}s S{4}",
							i,
							maxPrimaryTime,
							maxSecondaryTime,
							region.duration,
							region.locked ? "L" : "");
					}
				}
			}

			if (Log)
			{
				Debug.LogFormat(
					"Mod {0} ({1}) AdjustTimelineRegions {2}",
					ModLink.modIndex,
					ModLink.modID,
					sb);
			}

			var selectedRegion = timelineRegionsModified[selectedIndex];
			return (maxPrimaryTime - selectedRegion.duration, maxSecondaryTime - selectedRegion.duration);
		}

		static float CorrectEarlierOffset(
			float startTime,
			float actionStartTime,
			float offset)
		{
			if (startTime > actionStartTime)
			{
				offset += startTime - actionStartTime;
			}
			return offset;
		}

		static float CorrectLaterOffset(
			float startTime,
			float actionStartTime,
			float offset)
		{
			if (startTime < actionStartTime)
			{
				offset += startTime - actionStartTime;
			}
			return offset;
		}

		static void RepositionActionButtons(CIViewCombatTimeline inst, Traverse t)
		{
			var combat = Contexts.sharedInstance.combat;
			var turnStartTime = (float)combat.currentTurn.i * combat.turnLength.i;
			var helpersActionsPlanned = t.Field<Dictionary<int, CIHelperTimelineAction>>("helpersActionsPlanned").Value;
			var timelineRegionsModified = t.Field<List<TimelineRegion>>("timelineRegionsModified").Value;

			foreach (var region in timelineRegionsModified)
			{
				if (!region.changed)
				{
					continue;
				}
				if (!helpersActionsPlanned.TryGetValue(region.actionID, out var helperTimelineAction))
				{
					continue;
				}

				var action = IDUtility.GetActionEntity(region.actionID);
				if (action == null)
				{
					continue;
				}
				if (action.isDisposed)
				{
					continue;
				}

				var x = inst.timelineOffsetLeft + (region.startTime - turnStartTime) * inst.timelineSecondSize;
				helperTimelineAction.transform.SetPositionLocalX(x);
				action.ReplaceStartTime(region.startTime);
			}
		}

		static void DumpModifiedRegions(Traverse t)
		{
			var timelineRegionsModified = t.Field<List<TimelineRegion>>("timelineRegionsModified").Value;
			var sb = new System.Text.StringBuilder();
			foreach (var region in timelineRegionsModified)
			{
				sb.AppendFormat("\n  {0}A-{1} ({2}) {3} {4:F3}s {5:F3}s {6:F3}s",
					region.changed ? '*' : ' ',
					region.actionID,
					region.actionKey,
					region.locked ? 'L' : 'U',
					region.offset,
					region.startTime,
					region.duration);
			}
			Debug.LogFormat(
				"Mod {0} ({1}) AdjustTimelineRegions modified regions{2}",
				ModLink.modIndex,
				ModLink.modID,
				sb);
		}
	}
}
