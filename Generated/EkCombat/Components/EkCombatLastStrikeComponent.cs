// Copyright (c) 2022 EchKode
// SPDX-License-Identifier: BSD-3-Clause

namespace EchKode.PBMods.Fixes.ECS
{
	//------------------------------------------------------------------------------
	// <auto-generated>
	//     This code was generated by Entitas.CodeGeneration.Plugins.ComponentEntityApiGenerator.
	//
	//     Changes to this file may cause incorrect behavior and will be lost if
	//     the code is regenerated.
	// </auto-generated>
	//------------------------------------------------------------------------------
	public partial class EkCombatEntity
	{

		public LastStrikeComponent lastStrike { get { return (LastStrikeComponent)GetComponent(EkCombatComponentsLookup.LastStrike); } }
		public bool hasLastStrike { get { return HasComponent(EkCombatComponentsLookup.LastStrike); } }

		public void AddLastStrike(int newSourceID, int newCombatID, int newPilotID)
		{
			var index = EkCombatComponentsLookup.LastStrike;
			var component = (LastStrikeComponent)CreateComponent(index, typeof(LastStrikeComponent));
			component.sourceID = newSourceID;
			component.combatID = newCombatID;
			component.pilotID = newPilotID;
			AddComponent(index, component);
		}

		public void ReplaceLastStrike(int newSourceID, int newCombatID, int newPilotID)
		{
			var index = EkCombatComponentsLookup.LastStrike;
			var component = (LastStrikeComponent)CreateComponent(index, typeof(LastStrikeComponent));
			component.sourceID = newSourceID;
			component.combatID = newCombatID;
			component.pilotID = newPilotID;
			ReplaceComponent(index, component);
		}

		public void RemoveLastStrike()
		{
			RemoveComponent(EkCombatComponentsLookup.LastStrike);
		}
	}

	//------------------------------------------------------------------------------
	// <auto-generated>
	//     This code was generated by Entitas.CodeGeneration.Plugins.ComponentMatcherApiGenerator.
	//
	//     Changes to this file may cause incorrect behavior and will be lost if
	//     the code is regenerated.
	// </auto-generated>
	//------------------------------------------------------------------------------
	public sealed partial class EkCombatMatcher
	{

		static Entitas.IMatcher<EkCombatEntity> _matcherLastStrike;

		public static Entitas.IMatcher<EkCombatEntity> LastStrike
		{
			get
			{
				if (_matcherLastStrike == null)
				{
					var matcher = (Entitas.Matcher<EkCombatEntity>)Entitas.Matcher<EkCombatEntity>.AllOf(EkCombatComponentsLookup.LastStrike);
					matcher.componentNames = EkCombatComponentsLookup.componentNames;
					_matcherLastStrike = matcher;
				}

				return _matcherLastStrike;
			}
		}
	}
}
