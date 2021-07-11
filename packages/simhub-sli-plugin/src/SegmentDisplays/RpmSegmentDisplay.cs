/*
 * RPM segment display.
 */

using System;
using SimElation.SliDevices;

// ---------------------------------------------------------------------------------------------------------------------------------

namespace SimElation.Simhub.SliPlugin
{
	/// <summary>RPM segment display.</summary>
	public sealed class RpmSegmentDisplay : SegmentDisplay
	{
		/// <summary>Constructor.</summary>
		/// <param name="shortName">Description of the information show by the display. This will be displayed when switching
		/// to this segment display for a period of time.</param>
		public RpmSegmentDisplay(String shortName) : base(shortName, "Current RPM")
		{
		}

		/// <inheritdoc/>
		public override void ProcessData(NormalizedData normalizedData, Device.SegmentDisplayPosition position,
			IOutputFormatters outputFormatters, ref String str, ref uint[] decimalOrPrimeIndexList)
		{
			// TODO what is FilteredRpms?
			outputFormatters.Rpm(normalizedData.StatusData.Rpms, ref str, ref decimalOrPrimeIndexList);
		}
	}
}
