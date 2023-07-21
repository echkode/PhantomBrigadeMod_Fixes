namespace EchKode.PBMods.Fixes
{
	partial class ModLink
	{
		private static void Initialize()
		{
			ModManager.Initialize();
			ProjectileProximityFuseSystem.Initialize();
			ECS.EkCombatTeardownSystem.Initialize();
			CIViewCombatMode.Initialize();
		}
	}
}
