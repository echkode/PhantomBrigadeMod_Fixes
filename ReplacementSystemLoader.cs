using System;
using System.Collections.Generic;

using Entitas;

using HarmonyLib;

using PhantomBrigade;

using UnityEngine;

namespace EchKode.PBMods.Fixes
{
	internal static class ReplacementSystemLoader
	{
		public static void Load<T, U>(GameController gameController, string state, Func<Contexts, U> ctor)
			where T : IExecuteSystem
			where U : T
		{
			var gcs = gameController.m_stateDict[state];
			var feature = gcs.m_systems[0];
			var fi = AccessTools.Field(feature.GetType(), "_executeSystems");
			if (fi == null)
			{
				Debug.LogWarningFormat(
					"Mod {0} ({1}) unable to replace systems | can't find _executeSystems field in feature {2}",
					ModLink.modIndex,
					ModLink.modId,
					feature.GetType().Name);
				return;
			}

			var systems = (List<IExecuteSystem>)fi.GetValue(feature);
			if (systems == null)
			{
				Debug.LogWarningFormat(
					"Mod {0} ({1}) unable to replace systems | null _executeSystems field in feature {2}",
					ModLink.modIndex,
					ModLink.modId,
					feature.GetType().Name);
				return;
			}

			var idx = systems.FindIndex(sys => sys is T);
			if (idx == -1)
			{
				Debug.LogWarningFormat(
					"Mod {0} ({1}) unable to replace systems | can't find system {3} in {2}._executeSystems",
					ModLink.modIndex,
					ModLink.modId,
					feature.GetType().Name,
					typeof(T).Name);
				return;
			}

			systems[idx] = ctor(Contexts.sharedInstance);
			Debug.LogFormat(
				"Mod {0} ({1}) extending {2}",
				ModLink.modIndex,
				ModLink.modId,
				typeof(T).Name);

			// XXX not sure how necessary this is since the profiler is something you generally use from within
			// the Unity editor.
			fi = AccessTools.Field(feature.GetType(), "_executeSystemNames");
			var names = (List<string>)fi.GetValue(feature);
			names[idx] = systems[idx].GetType().FullName;
		}
	}
}
