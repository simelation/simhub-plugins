/*
 * Fuel status segment display.
 */

using System;
using SimElation.SliDevices;

// ---------------------------------------------------------------------------------------------------------------------------------

namespace SimElation.Simhub.SliPlugin
{
	/// <summary>Fuel status segment display.</summary>
	public sealed class FuelSegmentDisplay : SegmentDisplay
	{
		/// <summary>Constructor.</summary>
		public FuelSegmentDisplay() : base("Fuel", "Fuel remaining")
		{
		}

		/// <inheritdoc/>
		public override void ProcessData(NormalizedData normalizedData, Device.SegmentDisplayPosition position,
			IOutputFormatters outputFormatters, ref String str, ref uint[] decimalOrPrimeIndexList)
		{
			outputFormatters.Fuel(normalizedData.StatusData.Fuel, normalizedData.FuelRemainingLaps, ref str,
				ref decimalOrPrimeIndexList);
		}
	}
}
