/*
 * Brake bias segment display.
 */

using System;
using SimElation.SliDevices;

// ---------------------------------------------------------------------------------------------------------------------------------

namespace SimElation.Simhub.SliPlugin
{
	/// <summary>Brake bias segment display.</summary>
	public sealed class BrakeBiasSegmentDisplay : SegmentDisplay
	{
		/// <summary>Constructor.</summary>
		/// <param name="shortName">Description of the information show by the display. This will be displayed when switching
		/// to this segment display for a period of time.</param>
		public BrakeBiasSegmentDisplay(String shortName) : base(shortName, "Brake bias")
		{
		}

		/// <inheritdoc/>
		public override void ProcessData(NormalizedData normalizedData, Device.SegmentDisplayPosition position,
			IOutputFormatters outputFormatters, ref String str, ref uint[] decimalOrPrimeIndexList)
		{
			outputFormatters.BrakeBias(normalizedData.StatusData.BrakeBias, ref str, ref decimalOrPrimeIndexList);
		}
	}
}
