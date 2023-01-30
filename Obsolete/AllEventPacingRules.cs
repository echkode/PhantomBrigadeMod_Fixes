using System.Collections.Generic;
using System.Text;

using HarmonyLib;
using MonoMod.Utils;

using PhantomBrigade.Data;
using PhantomBrigade.Overworld.Components;
using PhantomBrigade.Overworld.Systems.EventPacingRules;
using PBAllEventPacingRules = PhantomBrigade.Overworld.Systems.EventPacingRules.AllEventPacingRules;

using UnityEngine;

namespace EchKode.PBMods.Fixes
{
	static class AllEventPacingRules
	{
		internal static bool IsInitialized = false;
		private static List<EventPacingRule> rules;
		private static readonly StringBuilder logBuilder = new StringBuilder();
		private static readonly Dictionary<string, float> chanceCache = new Dictionary<string, float>();

		internal static void Initialize()
		{
			rules = Traverse.Create(typeof(PBAllEventPacingRules)).Field<List<EventPacingRule>>(nameof(rules)).Value;
			if (rules == null)
			{
				Debug.LogWarningFormat("Mod {0} ({1}) reflection failed to get the rules",
					ModLink.modIndex,
					ModLink.modId);
				return;
			}
			Debug.LogFormat("Mod {0} ({1}) loaded rules | count: {2}",
				ModLink.modIndex,
				ModLink.modId,
				rules.Count);

			IsInitialized = true;
		}

		internal static void Apply(
			List<EventHistory.Record> history,
			Dictionary<string, float> chances,
			Dictionary<string, int> priorities)
		{
			try
			{
				chanceCache.Clear();
				chanceCache.AddRange(chances);
				foreach (EventPacingRule rule in rules)
				{
					rule.Apply(history, chances, priorities);
				}
				if (DataShortcuts.overworld.logEventEvaluationOverTime)
				{
					LogHistory(history);
					LogChances(chances);
				}
			}
			catch (System.NullReferenceException)
			{
				Debug.LogErrorFormat("Mod {0} ({1}) hit a null reference | rules is null: {2}",
					ModLink.modIndex,
					ModLink.modId,
					rules == null);
			}
		}

		private static void LogHistory(List<EventHistory.Record> history)
		{
			logBuilder.Clear();
			foreach (var r in history)
			{
				logBuilder.AppendFormat("{0}: {1:F2}, ", r.key, r.simulationTime);
			}
			LogBuilder("history");
		}

		private static void LogChances(Dictionary<string, float> chances)
		{
			logBuilder.Clear();
			foreach (var chance in chances)
			{
				if (chanceCache.TryGetValue(chance.Key, out var num) && chance.Value != num)
				{
					logBuilder.AppendFormat("{0}: {1} -> {2}, ", chance.Key, num, chance.Value);
				}
			}
			LogBuilder("new chances");
		}

		private static void LogBuilder(string label)
		{
			if (logBuilder.Length == 0)
			{
				return;
			}
			// Pick off trailing comma-space.
			logBuilder.Remove(logBuilder.Length - 2, 2);
			Debug.LogFormat("{0}: {1}", label, logBuilder);
		}
	}
}
