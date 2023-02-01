using System.Collections.Generic;

using HarmonyLib;

using PhantomBrigade;
using PhantomBrigade.Input.Components;
using PBCIViewCombatMode = CIViewCombatMode;

using UnityEngine;

namespace EchKode.PBMods.Fixes
{
	using UnitComparer = System.Func<PersistentEntity, PersistentEntity, int>;

	static class CIViewCombatMode
	{
		private delegate void RedrawList(
			CIHelperTimelineUnitGroup helperGroup,
			List<PersistentEntity> list,
			float hue,
			ref int unitsDisplayed,
			ref int offsetAccumulated,
			int poolSizeLast,
			ref int poolInstanceIndex,
			ref int poolInstancesUsed);

		private static bool initialized;
		private static Traverse ins;
		private static List<PersistentEntity> briefingOrder;
		private static HashSet<int> activePlayerUnits;

		private static List<int> unitCombatIDsDisplayed;
		private static List<PersistentEntity> unitListSorted;
		private static List<PersistentEntity> unitListSortedFriendly;
		private static List<PersistentEntity> unitListSortedAllied;
		private static List<PersistentEntity> unitListSortedEnemy;
		private static Dictionary<int, CIHelperTimelineUnit> unitTabInstances;
		private static List<CIHelperTimelineUnit> unitTabPool;

		private static RedrawList redrawList;

		private static System.Comparison<PersistentEntity> standardUnitComparison;

		internal static void Initialize()
		{
			briefingOrder = new List<PersistentEntity>();
			activePlayerUnits = new HashSet<int>();

			ins = Traverse.Create(PBCIViewCombatMode.ins);
			unitCombatIDsDisplayed = ins.Field<List<int>>("unitCombatIDsDisplayed").Value;
			unitListSorted = ins.Field<List<PersistentEntity>>("unitListSorted").Value;
			unitListSortedFriendly = ins.Field<List<PersistentEntity>>("unitListSortedFriendly").Value;
			unitListSortedAllied = ins.Field<List<PersistentEntity>>("unitListSortedAllied").Value;
			unitListSortedEnemy = ins.Field<List<PersistentEntity>>("unitListSortedEnemy").Value;
			unitTabInstances = ins.Field<Dictionary<int, CIHelperTimelineUnit>>("unitTabInstances").Value;
			unitTabPool = ins.Field<List<CIHelperTimelineUnit>>("unitTabPool").Value;

			var mi = AccessTools.DeclaredMethod(typeof(PBCIViewCombatMode), "CompareUnitForTabSorting");
			if (mi == null)
			{
				Debug.LogWarningFormat(
					"Mod {0} ({1}) Unable to initialize CIViewCombatMode | reflection failed to find method CompareUnitForTabSorting",
					ModLink.modIndex,
					ModLink.modId);
				return;
			}
			var compareUnits = (UnitComparer)mi.CreateDelegate(typeof(UnitComparer), PBCIViewCombatMode.ins);
			standardUnitComparison = new System.Comparison<PersistentEntity>(compareUnits);

			mi = AccessTools.DeclaredMethod(typeof(PBCIViewCombatMode), "RedrawList");
			if (mi == null)
			{
				Debug.LogWarningFormat(
					"Mod {0} ({1}) Unable to initialize CIViewCombatMode | reflection failed to find method RedrawList",
					ModLink.modIndex,
					ModLink.modId);
				return;
			}
			redrawList = (RedrawList)mi.CreateDelegate(typeof(RedrawList), PBCIViewCombatMode.ins);

			Debug.LogFormat(
				"Mod {0} ({1}) Initialized reflection hooks for CIViewCombatMode",
				ModLink.modIndex,
				ModLink.modId);

			initialized = true;
		}

		internal static void RedrawUnitTabs()
		{
			if (!initialized)
			{
				return;
			}

			var entered = ins.Field<bool>("entered").Value;
			var modeLast = ins.Field<CombatUIModes>("modeLast").Value;

			if (!entered || modeLast != CombatUIModes.Unit_Selection)
			{
				ins.Field<bool>("unitRedrawScheduled").Value = true;
				return;
			}

			ins.Field<bool>("unitRedrawScheduled").Value = false;
			var participantUnits = ScenarioUtility.GetCombatParticipantUnits();
			var currentScenario = ScenarioUtility.GetCurrentScenario();
			var hasPlayerSquad = currentScenario != null && currentScenario.squadUsed;

			unitCombatIDsDisplayed.Clear();
			unitListSorted.Clear();
			unitListSortedFriendly.Clear();
			unitListSortedAllied.Clear();
			unitListSortedEnemy.Clear();
			unitTabInstances.Clear();
			UIHelper.PrepareForPoolIteration(
				ref unitTabPool,
				out var poolSizeLast,
				out var poolInstanceIndex,
				out var poolInstancesUsed);

			BuildSortedUnitLists(participantUnits, hasPlayerSquad);

			var h1 = new HSBColor(PBCIViewCombatMode.ins.colorFriendly).h;
			var h2 = new HSBColor(PBCIViewCombatMode.ins.colorAllied).h;
			var h3 = new HSBColor(PBCIViewCombatMode.ins.colorEnemy).h;
			var unitsDisplayed = 0;
			var offsetAccumulated = 0;
			redrawList(
				PBCIViewCombatMode.ins.unitGroupFriendly,
				unitListSortedFriendly,
				h1,
				ref unitsDisplayed,
				ref offsetAccumulated,
				poolSizeLast,
				ref poolInstanceIndex,
				ref poolInstancesUsed);
			redrawList(
				PBCIViewCombatMode.ins.unitGroupAllied,
				unitListSortedAllied,
				h2,
				ref unitsDisplayed,
				ref offsetAccumulated,
				poolSizeLast,
				ref poolInstanceIndex,
				ref poolInstancesUsed);
			redrawList(
				PBCIViewCombatMode.ins.unitGroupHostile,
				unitListSortedEnemy,
				h3,
				ref unitsDisplayed,
				ref offsetAccumulated,
				poolSizeLast,
				ref poolInstanceIndex,
				ref poolInstancesUsed);
			UIHelper.HideUnusedPoolInstances(unitTabPool, poolInstancesUsed);
			PBCIViewCombatMode.ins.RedrawUnitSelection();
		}

		private static void BuildSortedUnitLists(
			List<PersistentEntity> participantUnits,
			bool hasPlayerSquad)
		{
			foreach (var unit in participantUnits)
			{
				if (unit.isHidden)
				{
					continue;
				}
				if (!unit.isUnitDeployed)
				{
					continue;
				}
				if (unit.isDestroyed)
				{
					continue;
				}
				if (unit.isWrecked)
				{
					continue;
				}

				var combatUnit = IDUtility.GetLinkedCombatEntity(unit);
				if (!ScenarioUtility.IsUnitActive(unit, combatUnit, false))
				{
					continue;
				}

				AddToUnitList(unit, combatUnit, hasPlayerSquad);
			}

			SortPlayerUnits();  // original just sorted with standardUnitComparison
			unitListSortedAllied.Sort(standardUnitComparison);
			unitListSortedEnemy.Sort(standardUnitComparison);
			unitListSorted.AddRange(unitListSortedFriendly);
			unitListSorted.AddRange(unitListSortedAllied);
			unitListSorted.AddRange(unitListSortedEnemy);
		}

		private static void AddToUnitList(
			PersistentEntity unit,
			CombatEntity combatUnit,
			bool hasPlayerSquad)
		{
			var isFriendly = CombatUIUtility.IsFactionFriendly(unit.faction.s);
			if (!isFriendly)
			{
				unitListSortedEnemy.Add(unit);
				return;
			}

			if (!unit.isDisposableOutsideCombat)
			{
				unitListSortedFriendly.Add(unit);
				return;
			}

			if (!combatUnit.isPlayerControllable)
			{
				Debug.LogWarningFormat(
					"Mod {0} ({1}) Friendly units should be player controllable | unit ID: {2} | combat ID: {3}",
					ModLink.modIndex,
					ModLink.modId,
					unit.id.id,
					combatUnit.id.id);
				return;
			}

			if (hasPlayerSquad)
			{
				unitListSortedAllied.Add(unit);
				return;
			}

			unitListSortedFriendly.Add(unit);
		}

		private static void SortPlayerUnits()
		{
			if (unitListSortedFriendly.Count == 0)
			{
				return;
			}

			if (!Contexts.sharedInstance.persistent.hasSquadComposition)
			{
				unitListSortedFriendly.Sort(standardUnitComparison);
				unitListSortedFriendly.Sort(standardUnitComparison);
				Debug.LogFormat(
					"Mod {0} ({1}) Using standard sort order for friendly units | no squad composition",
					ModLink.modIndex,
					ModLink.modId);
				return;
			}

			activePlayerUnits.Clear();
			foreach (var unit in unitListSortedFriendly)
			{
				activePlayerUnits.Add(unit.id.id);
			}

			briefingOrder.Clear();
			foreach (var squadMember in Contexts.sharedInstance.persistent.squadComposition.slots)
			{
				if (string.IsNullOrEmpty(squadMember.unitNameInternal))
				{
					continue;
				}

				var pe = IDUtility.GetPersistentEntity(squadMember.unitNameInternal);
				if (pe == null)
				{
					unitListSortedFriendly.Sort(standardUnitComparison);
					Debug.LogFormat(
						"Mod {0} ({1}) Using standard sort order for friendly units | unable to resolve name to PE | name: {2}",
						ModLink.modIndex,
						ModLink.modId,
						squadMember.unitNameInternal);
					return;
				}

				if (!activePlayerUnits.Contains(pe.id.id))
				{
					continue;
				}

				briefingOrder.Add(pe);
			}

			if (briefingOrder.Count != unitListSortedFriendly.Count)
			{
				unitListSortedFriendly.Sort(standardUnitComparison);
				Debug.LogFormat(
					"Mod {0} ({1}) Using standard sort order for friendly units | unit list count: {2} | briefing count: {3}",
					ModLink.modIndex,
					ModLink.modId,
					unitListSortedFriendly.Count,
					briefingOrder.Count);
				return;
			}

			unitListSortedFriendly.Clear();
			unitListSortedFriendly.AddRange(briefingOrder);
		}
	}
}
