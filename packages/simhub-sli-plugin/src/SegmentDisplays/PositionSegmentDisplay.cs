/*
 * Position segment display.
 */

using System;
using SimElation.SliDevices;

// ---------------------------------------------------------------------------------------------------------------------------------

namespace SimElation.Simhub.SliPlugin
{
	/// <summary>Position segment display.</summary>
	/// <remarks>
	/// Shows current position as "Pxx-yy" where xx is the current position and yy is the number of runners.
	/// </remarks>
	public sealed class PositionSegmentDisplay : SegmentDisplay
	{
		/// <summary>Constructor.</summary>
		public PositionSegmentDisplay() : base("Posn", "Position")
		{
		}

		/// <inheritdoc/>
		public override void ProcessData(NormalizedData normalizedData, Device.SegmentDisplayPosition position,
			IOutputFormatters outputFormatters, ref String str, ref uint[] decimalOrPrimeIndexList)
		{
			outputFormatters.Position(normalizedData.StatusData.Position, normalizedData.StatusData.OpponentsCount,
				ref str, ref decimalOrPrimeIndexList);
		}
	}
}
