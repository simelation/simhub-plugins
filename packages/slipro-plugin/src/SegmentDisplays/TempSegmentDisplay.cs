/*
 * Temperature segement display.
 */

using System;
using SimElation.SliPro;
using SimHub.Plugins;

// ---------------------------------------------------------------------------------------------------------------------------------

namespace SimElation.SimHubIntegration.SliProPlugin
{
	/// <summary>Temperature segement display.</summary>
	public class TempSegmentDisplay : SegmentDisplay
	{
		/// <summary>Function type to get a temperature value from <see cref="NormalizedData"/>.</summary>
		/// <param name="normalizedData"></param>
		/// <returns>Temperature.</returns>
		public delegate double GetTemperature(NormalizedData normalizedData);

		/// <summary>Constructor.</summary>
		/// <param name="name">Description of the temperature. This will be displayed when switching
		/// to this segment display for a period of time (so keep it fewer than 7 characters).</param>
		/// <param name="prefix">String to prefix the temperature with, e.g. "Oil", "H20".</param>
		/// <param name="getTemperature">Function to get a temperature from <see cref="NormalizedData"/>.</param>
		public TempSegmentDisplay(String name, String prefix, GetTemperature getTemperature) : base(name)
		{
			m_prefix = prefix;
			m_getTemperature = getTemperature;
		}

		/// <inheritdoc/>
		public override void ProcessData(PluginManager pluginManager, NormalizedData normalizedData, SliPro.SliPro sliPro,
			SegmentDisplayPosition position)
		{
			String str = String.Format("{0}{1,3}", m_prefix, Math.Round(m_getTemperature(normalizedData)));

			sliPro.SetSegment(position, 0, SliPro.Constants.segmentDisplayWidth, str, 2, 3);
		}

		private String m_prefix;
		private GetTemperature m_getTemperature;
	}
}
