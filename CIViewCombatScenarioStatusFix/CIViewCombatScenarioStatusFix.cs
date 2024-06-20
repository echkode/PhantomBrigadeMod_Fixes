// Copyright (c) 2024 EchKode
// SPDX-License-Identifier: BSD-3-Clause

using PhantomBrigade.Combat.Components;
using PhantomBrigade.Data;

using UnityEngine;

namespace EchKode.PBMods.CIViewCombatScenarioStatusFix
{
	public static class CIViewCombatScenarioStatusFix
	{
		public static void LogStateScopeMetadata(
			int currentTurn,
			ScenarioStateScopeMetadata scopeMetadata,
			DataBlockScenarioState state)
		{
			var turnCheck = state.turn;
			var turnValue = turnCheck.relative
				? currentTurn - scopeMetadata.entryTurn
				: turnCheck.value;
			var total = turnCheck.relative
				? turnCheck.value
				: Mathf.Max(1, turnCheck.value - scopeMetadata.entryTurn);
			var remaining = turnCheck.value - turnValue;
			Debug.LogFormat(
				"Mod {0} ({1}) state scope metadata | turn: {2} | step: {3} | entry turn: {4}\n  turn check | comparison: {5} | value: {6} ({7}) | actual: {8}\n  turns: {9}/{10}",
				ModLink.ModIndex,
				ModLink.ModID,
				currentTurn,
				scopeMetadata.entryStepKey,
				scopeMetadata.entryTurn,
				turnCheck.check,
				turnCheck.value,
				turnCheck.relative ? "relative" : "absolute",
				turnValue,
				remaining,
				total);
		}
	}
}
