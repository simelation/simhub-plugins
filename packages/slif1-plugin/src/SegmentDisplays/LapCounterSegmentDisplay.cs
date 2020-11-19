/*
 * Lap counter segment display.
 */

using System;
using SimElation.SliF1;
using SimHub.Plugins;

// ---------------------------------------------------------------------------------------------------------------------------------

namespace SimElation.SimHubIntegration.SliF1Plugin
{
	/// <summary></summary>
	/// <remarks>
	/// Shows "Lxx-yy" where xx is the current lap and yy is the total number of laps, or
	/// "Lxxxxx" where xxxxx is the number of completed laps (if current of total is not applicable).
	/// </remarks>
	public class LapsCounterSegmentDisplay : SegmentDisplay
	{
		/// <summary>Constructor.</summary>
		public LapsCounterSegmentDisplay() : base("Lap", "Current lap #")
		{
		}

		/// <inheritdoc/>
		public override void ProcessData(PluginManager pluginManager, NormalizedData normalizedData, SliF1.SliF1 sliF1,
			SegmentDisplayPosition position)
		{
			String str;

			if ((0 != normalizedData.m_statusData.TotalLaps) && (normalizedData.m_statusData.CurrentLap <= 99) &&
				(normalizedData.m_statusData.TotalLaps <= 99))
			{
				str = String.Format("{0,2}{1,2}", Math.Min(99, normalizedData.m_statusData.CurrentLap), Math.Min(99,
					normalizedData.m_statusData.TotalLaps));
				sliF1.SetSegment(position, 0, SliF1.Constants.segmentDisplayWidth, str, 1);
			}
			else
			{
				str = String.Format("{0,4}", normalizedData.m_statusData.CompletedLaps);
				sliF1.SetSegment(position, 0, SliF1.Constants.segmentDisplayWidth, str);
			}
		}
	}
}
