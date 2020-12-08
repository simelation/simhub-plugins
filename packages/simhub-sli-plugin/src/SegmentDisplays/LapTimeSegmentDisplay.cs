/*
 * Lap time segment display.
 */

using System;
using SimElation.SliDevices;

// ---------------------------------------------------------------------------------------------------------------------------------

namespace SimElation.Simhub.SliPlugin
{
	/// <summary>Lap time segment display.</summary>
	public sealed class LapTimeSegmentDisplay : SegmentDisplay
	{
		/// <summary>Function type to get a lap time value from <see cref="NormalizedData"/>.</summary>
		/// <param name="normalizedData"></param>
		/// <returns>Lap time or null if not available.</returns>
		public delegate TimeSpan GetTimeSpan(NormalizedData normalizedData);

		/// <summary>Constructor.</summary>
		/// <param name="shortName">Description of the lap time. This will be displayed when switching
		/// to this segment display for a period of time.</param>
		/// <param name="longName">Description of the lap time for the UI.</param>
		/// <param name="getTimeSpan">Function to get a delta value from <see cref="NormalizedData"/>.</param>
		public LapTimeSegmentDisplay(String shortName, String longName,
			GetTimeSpan getTimeSpan) : base(shortName, longName)
		{
			m_getTimeSpan = getTimeSpan;
		}

		/// <inheritdoc/>
		public override void ProcessData(NormalizedData normalizedData, Device.SegmentDisplayPosition position,
			IOutputFormatters outputFormatters, ref String str, ref uint[] decimalOrPrimeIndexList)
		{
			outputFormatters.LapTime(m_getTimeSpan(normalizedData), ref str, ref decimalOrPrimeIndexList);
		}

		private readonly GetTimeSpan m_getTimeSpan;
	}
}
