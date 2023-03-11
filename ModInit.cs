namespace EchKode.PBMods.Fixes
{
	partial class ModLink
	{
		private static void Initialize()
		{
			ModManager.Initialize();
			CombatCollisionSystem.Initialize();
			ProjectileProximityFuseSystem.Initialize();
			ProjectileSplashDamageSystem.Initialize();
			ECS.EkCombatTeardownSystem.Initialize();
			CombatScenarioStateSystem.Initialize();
			CIViewCombatMode.Initialize();
		}
	}
}
