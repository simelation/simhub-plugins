/*
 * Lap counter segment display.
 */

using System;
using SimElation.SliDevices;

// ---------------------------------------------------------------------------------------------------------------------------------

namespace SimElation.Simhub.SliPlugin
{
	/// <summary>Lap counter display.</summary>
	/// <remarks>
	/// Shows "Lxx-yy" where xx is the current lap and yy is the total number of laps, or
	/// "Lxxxxx" where xxxxx is the number of completed laps (if current of total is not applicable).
	/// </remarks>
	public sealed class LapsCounterSegmentDisplay : SegmentDisplay
	{
		/// <summary>Constructor.</summary>
		public LapsCounterSegmentDisplay() : base("Lap", "Current lap #")
		{
		}

		/// <inheritdoc/>
		public override void ProcessData(NormalizedData normalizedData, Device.SegmentDisplayPosition position,
			IOutputFormatters outputFormatters, ref String str, ref uint[] decimalOrPrimeIndexList)
		{
			outputFormatters.LapCounter(normalizedData.StatusData.TotalLaps, normalizedData.StatusData.CurrentLap,
				normalizedData.StatusData.CompletedLaps, ref str, ref decimalOrPrimeIndexList);
		}
	}
}
