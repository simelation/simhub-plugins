/*
 * Laps remaining segment display.
 */

using System;
using SimElation.SliDevices;

// ---------------------------------------------------------------------------------------------------------------------------------

namespace SimElation.Simhub.SliPlugin
{
	/// <summary>Laps remaining segment display.</summary>
	public sealed class LapsToGoSegmentDisplay : SegmentDisplay
	{
		/// <summary>Constructor.</summary>
		public LapsToGoSegmentDisplay() : base("togo", "Laps remaining")
		{
		}

		/// <inheritdoc/>
		public override void ProcessData(NormalizedData normalizedData, Device.SegmentDisplayPosition position,
			IOutputFormatters outputFormatters, ref String str, ref uint[] decimalOrPrimeIndexList)
		{
			if (normalizedData.StatusData.RemainingLaps > 0)
				outputFormatters.LapsToGo(normalizedData.StatusData.RemainingLaps, ref str, ref decimalOrPrimeIndexList);
		}
	}
}
