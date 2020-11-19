/*
 * Delta segment display.
 */

using System;
using SimElation.SliF1;
using SimHub.Plugins;

// ---------------------------------------------------------------------------------------------------------------------------------

namespace SimElation.SimHubIntegration.SliF1Plugin
{
	/// <summary>Delta time segment display.</summary>
	public class DeltaSegmentDisplay : SegmentDisplay
	{
		/// <summary>Function type to get a delta value from <see cref="NormalizedData"/>.</summary>
		/// <param name="normalizedData"></param>
		/// <returns>Delta time or null if not available.</returns>
		public delegate double? GetDelta(NormalizedData normalizedData);

		/// <summary>Constructor.</summary>
		/// <param name="shortName">Description of the delta time. This will be displayed when switching
		/// to this segment display for a period of time (so keep it fewer than 7 characters).</param>
		/// <param name="longName">Description of the delta time for the UI.</param>
		/// <param name="getDelta">Function to get a delta value from <see cref="NormalizedData"/>.</param>
		public DeltaSegmentDisplay(String shortName, String longName, GetDelta getDelta) : base(shortName, longName)
		{
			m_getDelta = getDelta;
		}

		/// <inheritdoc/>
		public override void ProcessData(PluginManager pluginManager, NormalizedData normalizedData, SliF1.SliF1 sliF1,
			SegmentDisplayPosition position)
		{
			String str;
			double? value = m_getDelta(normalizedData);
			uint[] decimalOrPrimeIndexList = { };

			if (value != null)
				Utils.Formatters.DeltaTime((double)value, out str, out decimalOrPrimeIndexList);
			else
				str = "".PadLeft((int)SliF1.Constants.segmentDisplayWidth);

			sliF1.SetSegment(position, 0, SliF1.Constants.segmentDisplayWidth, str, decimalOrPrimeIndexList);
		}

		private readonly GetDelta m_getDelta;
	}
}
