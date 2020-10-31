/*
 * Lap time segment display.
 */

using System;
using SimElation.SliPro;
using SimHub.Plugins;

// ---------------------------------------------------------------------------------------------------------------------------------

namespace SimElation.SimHubIntegration.SliProPlugin
{
	/// <summary>Lap time segment display.</summary>
	class LapTimeSegmentDisplay : SegmentDisplay
	{
		/// <summary>Function type to get a lap time value from <see cref="NormalizedData"/>.</summary>
		/// <param name="normalizedData"></param>
		/// <returns>Lap time or null if not available.</returns>
		public delegate TimeSpan GetTimeSpan(NormalizedData normalizedData);

		/// <summary>Constructor.</summary>
		/// <param name="shortName">Description of the lap time. This will be displayed when switching
		/// to this segment display for a period of time (so keep it fewer than 7 characters).</param>
		/// <param name="longName">Description of the lap time for the UI.</param>
		/// <param name="getTimeSpan">Function to get a delta value from <see cref="NormalizedData"/>.</param>
		public LapTimeSegmentDisplay(String shortName, String longName, GetTimeSpan getTimeSpan) : base(shortName, longName)
		{
			m_getTimeSpan = getTimeSpan;
		}

		/// <inheritdoc/>
		public override void ProcessData(PluginManager pluginManager, NormalizedData normalizedData, SliPro.SliPro sliPro,
			SegmentDisplayPosition position)
		{
			uint[] decimalOrPrimeIndexList;
			String str;

			Utils.Formatters.LapTime(m_getTimeSpan(normalizedData), out str, out decimalOrPrimeIndexList);

			// TODO only "correct" on right side due to sli-pro decimal/prime positions.
			sliPro.SetSegment(position, 0, SliPro.Constants.segmentDisplayWidth, str, decimalOrPrimeIndexList);
		}

		private readonly GetTimeSpan m_getTimeSpan;
	}
}
