/*
 * Laps remaining segment display.
 */

using System;
using SimElation.SliF1;
using SimHub.Plugins;

// ---------------------------------------------------------------------------------------------------------------------------------

namespace SimElation.SimHubIntegration.SliF1Plugin
{
	/// <summary>Laps remaining segment display.</summary>
	public class LapsToGoSegmentDisplay : SegmentDisplay
	{
		/// <summary>Constructor.</summary>
		public LapsToGoSegmentDisplay() : base("togo", "Laps remaining")
		{
		}

		/// <inheritdoc/>
		public override void ProcessData(PluginManager pluginManager, NormalizedData normalizedData, SliF1.SliF1 sliF1,
			SegmentDisplayPosition position)
		{
			String str = String.Format("{0,4}", normalizedData.m_statusData.RemainingLaps);

			sliF1.SetSegment(position, 0, SliF1.Constants.segmentDisplayWidth, str);
		}
	}
}
