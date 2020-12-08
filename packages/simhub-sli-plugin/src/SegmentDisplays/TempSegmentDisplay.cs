/*
 * Temperature segment display.
 */

using System;
using SimElation.SliDevices;

// ---------------------------------------------------------------------------------------------------------------------------------

namespace SimElation.Simhub.SliPlugin
{
	/// <summary>Temperature segment display.</summary>
	public sealed class TempSegmentDisplay : SegmentDisplay
	{
		/// <summary>Function type to get a temperature value from <see cref="NormalizedData"/>.</summary>
		/// <param name="normalizedData"></param>
		/// <returns>Temperature.</returns>
		public delegate double GetTemperature(NormalizedData normalizedData);

		/// <summary>Constructor.</summary>
		/// <param name="shortName">Description of the temperature. This will be displayed when switching
		/// to this segment display for a period of time.</param>
		/// <param name="longName">Description of the temperature for the UI.</param>
		/// <param name="prefix">String to prefix the temperature with, e.g. "Oil", "H20".</param>
		/// <param name="getTemperature">Function to get a temperature from <see cref="NormalizedData"/>.</param>
		public TempSegmentDisplay(String shortName, String longName, String prefix,
				GetTemperature getTemperature) : base(shortName, longName)
		{
			m_prefix = prefix;
			m_getTemperature = getTemperature;
		}

		/// <inheritdoc/>
		public override void ProcessData(NormalizedData normalizedData, Device.SegmentDisplayPosition position,
			IOutputFormatters outputFormatters, ref String str, ref uint[] decimalOrPrimeIndexList)
		{
			outputFormatters.Temperature(m_prefix, m_getTemperature(normalizedData), ref str, ref decimalOrPrimeIndexList);
		}

		private readonly String m_prefix;
		private readonly GetTemperature m_getTemperature;
	}
}
