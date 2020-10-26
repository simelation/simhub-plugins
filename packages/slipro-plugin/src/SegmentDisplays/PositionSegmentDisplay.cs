/*
 * Position segment display.
 */

using System;
using SimElation.SliPro;
using SimHub.Plugins;

// ---------------------------------------------------------------------------------------------------------------------------------

namespace SimElation.SimHubIntegration.SliProPlugin
{
	/// <summary>Position segment display.</summary>
	/// <remarks>
	/// Shows current position as "Pxx-yy" where xx is the current position and yy is the number of runners.
	/// </remarks>
	public class PositionSegmentDisplay : SegmentDisplay
	{
		/// <summary>Constructor.</summary>
		public PositionSegmentDisplay() : base("Posn")
		{
		}

		/// <inheritdoc/>
		public override void ProcessData(PluginManager pluginManager, NormalizedData normalizedData, SliPro.SliPro sliPro,
			SegmentDisplayPosition position)
		{
			String str = String.Format("P{0,2}-{1,2}", Math.Min(99, normalizedData.m_statusData.Position),
				Math.Min(99, normalizedData.m_statusData.OpponentsCount));

			sliPro.SetSegment(position, 0, SliPro.Constants.segmentDisplayWidth, str);
		}
	}
}
