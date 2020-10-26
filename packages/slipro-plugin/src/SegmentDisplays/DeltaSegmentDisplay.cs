/*
 * Delta segment display.
 */

using System;
using SimElation.SliPro;
using SimHub.Plugins;

// ---------------------------------------------------------------------------------------------------------------------------------

namespace SimElation.SimHubIntegration.SliProPlugin
{
	/// <summary>Delta time segment display.</summary>
	public class DeltaSegmentDisplay : SegmentDisplay
	{
		/// <summary>Function type to get a delta value from <see cref="NormalizedData"/>.</summary>
		/// <param name="normalizedData"></param>
		/// <returns>Delta time or null if not available.</returns>
		public delegate double? GetDelta(NormalizedData normalizedData);

		/// <summary>Constructor.</summary>
		/// <param name="name">Description of the delta time. This will be displayed when switching
		/// to this segment display for a period of time (so keep it fewer than 7 characters).</param>
		/// <param name="getDelta">Function to get a delta value from <see cref="NormalizedData"/>.</param>
		public DeltaSegmentDisplay(String name, GetDelta getDelta) : base(name)
		{
			m_getDelta = getDelta;
		}

		/// <inheritdoc/>
		public override void ProcessData(PluginManager pluginManager, NormalizedData normalizedData, SliPro.SliPro sliPro,
			SegmentDisplayPosition position)
		{
			String str;
			double? value = m_getDelta(normalizedData);
			uint[] decimalOrPrimeIndexList = { };

			if (value != null)
				Utils.Formatters.DeltaTime((double)value, out str, out decimalOrPrimeIndexList);
			else
				str = "".PadLeft((int)SliPro.Constants.segmentDisplayWidth);

			sliPro.SetSegment(position, 0, SliPro.Constants.segmentDisplayWidth, str, decimalOrPrimeIndexList);
		}

		private GetDelta m_getDelta;
	}
}
