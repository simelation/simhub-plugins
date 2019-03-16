/*
 * Brake bias segment display.
 */

using System;
using SimElation.SliPro;
using SimHub.Plugins;

namespace SimElation.SimHubIntegration.SliProPlugin
{
	/// <summary>Brake bias segment display.</summary>
	/// <remarks>
	/// Display bias as "FxxRyy" where xx and yy are percentages.
	/// </remarks>
	public class BrakeBiasSegmentDisplay : SegmentDisplay
	{
		/// <summary>Constructor.</summary>
		public BrakeBiasSegmentDisplay() : base("bbias")
		{
		}

		/// <inheritdoc/>
		public override void ProcessData(PluginManager pluginManager, NormalizedData normalizedData, SliPro.SliPro sliPro,
			SegmentDisplayPosition position)
		{
			var frontBias = normalizedData.m_statusData.BrakeBias;
			String str = String.Format("f{0:00}r{1:00}", frontBias, 100 - frontBias);

			sliPro.SetSegment(position, 0, SliPro.Constants.segmentDisplayWidth, str);
		}
	}
}
