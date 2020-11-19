/*
 * Lap time segment display.
 */

using System;
using SimElation.SliF1;
using SimHub.Plugins;

// ---------------------------------------------------------------------------------------------------------------------------------

namespace SimElation.SimHubIntegration.SliF1Plugin
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
		public override void ProcessData(PluginManager pluginManager, NormalizedData normalizedData, SliF1.SliF1 sliF1,
			SegmentDisplayPosition position)
		{
			uint[] decimalOrPrimeIndexList;
			String str;

			Utils.Formatters.LapTime(m_getTimeSpan(normalizedData), out str, out decimalOrPrimeIndexList);

			sliF1.SetSegment(position, 0, SliF1.Constants.segmentDisplayWidth, str, decimalOrPrimeIndexList);
		}

		private readonly GetTimeSpan m_getTimeSpan;
	}
}
