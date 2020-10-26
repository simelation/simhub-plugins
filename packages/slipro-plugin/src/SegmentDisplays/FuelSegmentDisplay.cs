/*
 * Fuel status segment display.
 */

using System;
using SimElation.SliPro;
using SimHub.Plugins;

// ---------------------------------------------------------------------------------------------------------------------------------

namespace SimElation.SimHubIntegration.SliProPlugin
{
	/// <summary>Fuel status segment display.</summary>
	public class FuelSegmentDisplay : SegmentDisplay
	{
		/// <summary>Constructor.</summary>
		public FuelSegmentDisplay() : base("Fuel")
		{
		}

		/// <inheritdoc/>
		public override void ProcessData(PluginManager pluginManager, NormalizedData normalizedData, SliPro.SliPro sliPro,
			SegmentDisplayPosition position)
		{
			// NB round down both the fuel level and estimated laps.
			String str = String.Format("F{0:00}", Math.Min((int)normalizedData.m_statusData.Fuel, 99));
			double value = 0.0;

			if (normalizedData.m_fuelRemainingLaps != null)
				value = (double)normalizedData.m_fuelRemainingLaps;

			if (value != 0.0)
				str += String.Format("L{0:00}", Math.Min((int)value, 99));
			else
				str += "   ";

			sliPro.SetSegment(position, 0, SliPro.Constants.segmentDisplayWidth, str);
		}
	}
}
