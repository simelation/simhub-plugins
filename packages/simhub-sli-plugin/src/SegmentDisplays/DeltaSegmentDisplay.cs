/*
 * Delta segment display.
 */

using System;
using SimElation.SliDevices;

// ---------------------------------------------------------------------------------------------------------------------------------

namespace SimElation.Simhub.SliPlugin
{
	/// <summary>Delta time segment display.</summary>
	public sealed class DeltaSegmentDisplay : SegmentDisplay
	{
		/// <summary>Function type to get a delta value from <see cref="NormalizedData"/>.</summary>
		/// <param name="normalizedData"></param>
		/// <returns>Delta time or null if not available.</returns>
		public delegate double? GetDelta(NormalizedData normalizedData);

		/// <summary>Constructor.</summary>
		/// <param name="shortName">Description of the delta time. This will be displayed when switching
		/// to this segment display for a period of time.</param>
		/// <param name="longName">Description of the delta time for the UI.</param>
		/// <param name="getDelta">Function to get a delta value from <see cref="NormalizedData"/>.</param>
		public DeltaSegmentDisplay(String shortName, String longName, GetDelta getDelta) :
			base(shortName, longName)
		{
			m_getDelta = getDelta;
		}

		/// <inheritdoc/>
		public override void ProcessData(NormalizedData normalizedData, Device.SegmentDisplayPosition position,
			IOutputFormatters outputFormatters, ref String str, ref uint[] decimalOrPrimeIndexList)
		{
			double? value = m_getDelta(normalizedData);

			if (value != null)
				outputFormatters.DeltaTime((double)value, ref str, ref decimalOrPrimeIndexList);
		}

		private readonly GetDelta m_getDelta;
	}
}
