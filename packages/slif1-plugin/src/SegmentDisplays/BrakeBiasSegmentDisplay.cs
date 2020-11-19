/*
 * Brake bias segment display.
 */

using System;
using SimElation.SliF1;
using SimHub.Plugins;

// ---------------------------------------------------------------------------------------------------------------------------------

namespace SimElation.SimHubIntegration.SliF1Plugin
{
	/// <summary>Brake bias segment display.</summary>
	/// <remarks>
	/// Display bias as "FxxRyy" where xx and yy are percentages.
	/// </remarks>
	public class BrakeBiasSegmentDisplay : SegmentDisplay
	{
		/// <summary>Constructor.</summary>
		public BrakeBiasSegmentDisplay() : base("bias", "Brake bias")
		{
		}

		/// <inheritdoc/>
		public override void ProcessData(PluginManager pluginManager, NormalizedData normalizedData, SliF1.SliF1 sliF1,
			SegmentDisplayPosition position)
		{
			var frontBias = normalizedData.m_statusData.BrakeBias;
			String str = String.Format("{0:00}{1:00}", frontBias, 100 - frontBias);

			sliF1.SetSegment(position, 0, SliF1.Constants.segmentDisplayWidth, str, 1);
		}
	}
}
