/*
 * Speed segment display.
 */

using System;
using SimElation.SliDevices;

// ---------------------------------------------------------------------------------------------------------------------------------

namespace SimElation.Simhub.SliPlugin
{
	/// <summary>Speed segment display.</summary>
	public sealed class SpeedSegmentDisplay : SegmentDisplay
	{
		/// <summary>Constructor.</summary>
		/// <param name="shortName">Description of the information show by the display. This will be displayed when switching
		/// to this segment display for a period of time.</param>
		public SpeedSegmentDisplay(String shortName) : base(shortName, "Current speed")
		{
		}

		/// <inheritdoc/>
		public override void ProcessData(NormalizedData normalizedData, Device.SegmentDisplayPosition position,
			IOutputFormatters outputFormatters, ref String str, ref uint[] decimalOrPrimeIndexList)
		{
			// TODO what is FilteredSpeedLocal?
			outputFormatters.Speed(normalizedData.StatusData.SpeedLocal, ref str, ref decimalOrPrimeIndexList);
		}
	}
}
