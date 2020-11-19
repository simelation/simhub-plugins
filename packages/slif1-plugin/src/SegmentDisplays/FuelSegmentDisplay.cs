/*
 * Fuel status segment display.
 */

using System;
using SimElation.SliF1;
using SimHub.Plugins;

// ---------------------------------------------------------------------------------------------------------------------------------

namespace SimElation.SimHubIntegration.SliF1Plugin
{
	/// <summary>Fuel status segment display.</summary>
	public class FuelSegmentDisplay : SegmentDisplay
	{
		/// <summary>Constructor.</summary>
		public FuelSegmentDisplay() : base("Fuel", "Fuel remaining")
		{
		}

		/// <inheritdoc/>
		public override void ProcessData(PluginManager pluginManager, NormalizedData normalizedData, SliF1.SliF1 sliF1,
			SegmentDisplayPosition position)
		{
			// NB round down both the fuel level and estimated laps.
			String str = String.Format("{0:00}", Math.Min((int)normalizedData.m_statusData.Fuel, 99));
			double value = normalizedData.m_fuelRemainingLaps ?? 0.0;

			if (value != 0.0)
				str += String.Format("{0:00}", Math.Min((int)value, 99));
			else
				str += "  ";

			sliF1.SetSegment(position, 0, SliF1.Constants.segmentDisplayWidth, str, 1);
		}
	}
}
