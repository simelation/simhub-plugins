/*
 * Laps remaining segment display.
 */

using System;
using SimElation.SliPro;
using SimHub.Plugins;

namespace SimElation.SimHubIntegration.SliProPlugin
{
	/// <summary>Laps remaining segment display.</summary>
	public class LapsToGoSegmentDisplay : SegmentDisplay
	{
		/// <summary>Constructor.</summary>
		public LapsToGoSegmentDisplay() : base("togo")
		{
		}

		/// <inheritdoc/>
		public override void ProcessData(PluginManager pluginManager, NormalizedData normalizedData, SliPro.SliPro sliPro,
			SegmentDisplayPosition position)
		{
			String str = String.Format("Lr{0,4}", normalizedData.m_statusData.RemainingLaps);

			sliPro.SetSegment(position, 0, SliPro.Constants.segmentDisplayWidth, str);
		}
	}
}
