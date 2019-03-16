/*
 * Lap counter segment display.
 */

using System;
using SimElation.SliPro;
using SimHub.Plugins;

// ---------------------------------------------------------------------------------------------------------------------------------

namespace SimElation.SimHubIntegration.SliProPlugin
{
	/// <summary></summary>
	/// <remarks>
	/// Shows "Lxx-yy" where xx is the current lap and yy is the total number of laps, or
	/// "Lxxxxx" where xxxxx is the number of completed laps (if current of total is not applicable).
	/// </remarks>
	public class LapsCounterSegmentDisplay : SegmentDisplay
	{
		/// <summary>Constructor.</summary>
		public LapsCounterSegmentDisplay() : base("Lap")
		{
		}

		/// <inheritdoc/>
		public override void ProcessData(PluginManager pluginManager, NormalizedData normalizedData, SliPro.SliPro sliPro,
			SegmentDisplayPosition position)
		{
			String str;

			if ((0 != normalizedData.m_statusData.TotalLaps) && (normalizedData.m_statusData.CurrentLap <= 99) &&
				(normalizedData.m_statusData.TotalLaps <= 99))
			{
				str = String.Format("L{0,2}-{1,2}", Math.Min(99, normalizedData.m_statusData.CurrentLap), Math.Min(99,
					normalizedData.m_statusData.TotalLaps));
			}
			else
			{
				str = String.Format("L{0,5}", normalizedData.m_statusData.CompletedLaps);
			}

			sliPro.SetSegment(position, 0, SliPro.Constants.segmentDisplayWidth, str);
		}
	}
}
