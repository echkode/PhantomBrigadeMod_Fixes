using HarmonyLib;

using PhantomBrigade.Data;
using PBDataManagerSave = PhantomBrigade.Data.DataManagerSave;

namespace EchKode.PBMods.DataManagerSaveFix
{
	[HarmonyPatch]
	static class Patch
	{
		[HarmonyPatch(typeof(PBDataManagerSave), "SaveAIData")]
		[HarmonyPrefix]
		static bool Dms_SaveAIDataPrefix(
			OverworldEntity sourceEntity,
			DataContainerSavedOverworldEntity targetData)
		{
			DataManagerSave.SaveAIData(sourceEntity, targetData);
			return false;
		}
	}
}
