// Copyright (c) 2023 EchKode
// SPDX-License-Identifier: BSD-3-Clause

using System.Text;

using QFSW.QC;
using UnityEngine;

namespace EchKode.PBMods.CIViewCombatTimelineFix
{
	static partial class ConsoleCommands
	{
		internal static void Register()
		{
			if (commandsRegistered)
			{
				return;
			}

			var registeredFunctions = new StringBuilder();
			var k = 0;

			foreach (var (qcName, desc, method) in commands)
			{
				var functionName = $"{method.DeclaringType.Name}.{method.Name}";
				var commandName = commandPrefix + qcName;
				var commandInfo = new CommandAttribute(
					commandName,
					desc,
					MonoTargetType.Single);
				var commandData = new CommandData(method, commandInfo);
				if (!QuantumConsoleProcessor.TryAddCommand(commandData))
				{
					Debug.LogFormat(
						"Mod {0} ({1}) did not register QC command successfully: {2} <{3}>",
						ModLink.modIndex,
						ModLink.modID,
						qcName,
						functionName);
					continue;
				}
				registeredFunctions.Append(System.Environment.NewLine + $"  {commandName} <{functionName}>");
				k += 1;
			}

			Debug.LogFormat("Mod {0} ({1}) loaded QC commands | count: {2}{3}",
				ModLink.modIndex,
				ModLink.modID,
				k,
				registeredFunctions);

			commandsRegistered = true;
		}

		private const string commandPrefix = "ek.";
		private static bool commandsRegistered;
	}
}
