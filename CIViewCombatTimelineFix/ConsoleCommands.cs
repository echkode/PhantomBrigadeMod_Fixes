// Copyright (c) 2023 EchKode
// SPDX-License-Identifier: BSD-3-Clause

using System.Collections.Generic;
using System.Reflection;

using HarmonyLib;

using QFSW.QC;

namespace EchKode.PBMods.CIViewCombatTimelineFix
{
	using CommandList = List<(string QCName, string Description, MethodInfo Method)>;

	static partial class ConsoleCommands
	{
		static readonly CommandList commands = new CommandList()
		{
			("fix.toggle-adjust-timeline-implementation", "Switch which adjust timeline region implemenation is used", AccessTools.DeclaredMethod(typeof(ConsoleCommands), nameof(ToggleImplementation))),
			("fix.toggle-adjust-timeline-logging", "Toggle logging in AdjustTimelineRegions", AccessTools.DeclaredMethod(typeof(ConsoleCommands), nameof(ToggleLogging))),
		};

		static void ToggleImplementation()
		{
			Patch.useOriginalImplemenation = !Patch.useOriginalImplemenation;
			QuantumConsole.Instance.LogToConsole("AdjustTimelineRegions implementation: " + (Patch.useOriginalImplemenation ? "original" : "patched"));
		}

		static void ToggleLogging()
		{
			CIViewCombatTimelineFix.Log = !CIViewCombatTimelineFix.Log;
			QuantumConsole.Instance.LogToConsole("Logging: " + (CIViewCombatTimelineFix.Log ? "on" : "off"));
		}
	}
}
