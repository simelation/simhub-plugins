using System;
using SimElation.SliPro;
using SimHub.Plugins;

namespace SimElation.SimHubIntegration.SliProPlugin
{
	class LapTimeSegmentDisplay : SegmentDisplay
	{
		public delegate TimeSpan GetTimeSpan(NormalizedData normalizedData);

		public LapTimeSegmentDisplay(String name, GetTimeSpan getTimeSpan) : base(name)
		{
			m_getTimeSpan = getTimeSpan;
		}

		public override void ProcessData(PluginManager pluginManager, NormalizedData normalizedData, SliPro.SliPro sliPro,
			SegmentDisplayPosition position)
		{
			uint[] decimalOrPrimeIndexList;
			String str;

			Utils.Formatters.LapTime(m_getTimeSpan(normalizedData), out str, out decimalOrPrimeIndexList);

			// TODO only "correct" on right side due to sli-pro decimal/prime positions.
			sliPro.SetSegment(position, 0, SliPro.Constants.segmentDisplayWidth, str, decimalOrPrimeIndexList);
		}

		private GetTimeSpan m_getTimeSpan;
	}
}
