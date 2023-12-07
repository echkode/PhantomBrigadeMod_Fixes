﻿namespace EchKode.PBMods.CombatExecutionEndLateSystemFix
{
	public static class CombatExecutionEndLateSystemFix
	{
		public static void SplitWaitAction(
			ActionEntity action,
			float actionEndTime,
			float turnStartTime)
		{
			if (action.isDisposed)
			{
				return;
			}
			if (action.startTime.f > turnStartTime)
			{
				return;
			}
			if (!action.hasDataLinkAction)
			{
				return;
			}

			var dataCore = action.dataLinkAction.data?.dataCore;
			if (dataCore == null)
			{
				return;
			}
			if (dataCore.paintingType != PhantomBrigade.Data.PaintingType.Wait)
			{
				return;
			}

			var remaining = actionEndTime - turnStartTime;
			if (remaining < 0.5f)  // constant taken from InputCombatWaitDrawingUtility.AttemptFinish()
			{
				// No runt waits.
				return;
			}

			action.ReplaceStartTime(turnStartTime);
			action.ReplaceDuration(remaining);
		}
	}
}
