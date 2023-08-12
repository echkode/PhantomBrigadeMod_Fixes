using HarmonyLib;

using PhantomBrigade.Data;
using PBCombatScenarioSetupSystem = PhantomBrigade.Combat.Systems.CombatScenarioSetupSystem;

namespace EchKode.PBMods.CombatScenarioSetupSystemFix
{
	[HarmonyPatch]
	static class Patch
	{
		[HarmonyPatch(typeof(PBCombatScenarioSetupSystem), "DeployUnitsForPlayer")]
		[HarmonyPrefix]
		static bool Csss_DeployUnitsForPlayerPrefix(
			PBCombatScenarioSetupSystem __instance,
			DataContainerScenario scenario,
			DataContainerCombatArea area,
			PersistentEntity unitHostPersistent)
		{
			return CombatScenarioSetupSystem.DeployUnitsForPlayer(
				__instance,
				scenario,
				area,
				unitHostPersistent);
		}
	}
}
