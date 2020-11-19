/*
 * Position segment display.
 */

using System;
using SimElation.SliF1;
using SimHub.Plugins;

// ---------------------------------------------------------------------------------------------------------------------------------

namespace SimElation.SimHubIntegration.SliF1Plugin
{
	/// <summary>Position segment display.</summary>
	/// <remarks>
	/// Shows current position as "Pxx-yy" where xx is the current position and yy is the number of runners.
	/// </remarks>
	public class PositionSegmentDisplay : SegmentDisplay
	{
		/// <summary>Constructor.</summary>
		public PositionSegmentDisplay() : base("Posn", "Position")
		{
		}

		/// <inheritdoc/>
		public override void ProcessData(PluginManager pluginManager, NormalizedData normalizedData, SliF1.SliF1 sliF1,
			SegmentDisplayPosition position)
		{
			String str = String.Format("{0,2}{1,2}", Math.Min(99, normalizedData.m_statusData.Position),
				Math.Min(99, normalizedData.m_statusData.OpponentsCount));

			sliF1.SetSegment(position, 0, SliF1.Constants.segmentDisplayWidth, str, 1);
		}
	}
}
