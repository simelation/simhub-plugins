/*
 * Segement display base functionality.
 */

using System;
using SimElation.SliPro;
using SimHub.Plugins;

// ---------------------------------------------------------------------------------------------------------------------------------

namespace SimElation.SimHubIntegration.SliProPlugin
{
	/// <summary>Base class for a segment display of some data.</summary>
	/// <remarks>
	/// Left segment display has decimals at indices  0, 1, 2, 4 and primes at 3, 5.
	/// Right segment display has decimals at indices 0, 2, 3, 4 and primes at 1, 5.
	/// </remarks>
	public abstract class SegmentDisplay
	{
		private readonly String m_shortName;

		/// <summary>Name for the segment display, to show in UI.</summary>
		public String LongName { get; }

		/// <summary>Constructor.</summary>
		/// <param name="shortName">Description of the information show by the display. This will be displayed when switching
		/// to this segment display for a period of time (so keep it fewer than 7 characters).</param>
		/// <param name="longName">Longer form name for the segment display, to show in UI.</param>
		public SegmentDisplay(String shortName, String longName)
		{
			m_shortName = shortName;
			LongName = longName;
		}

		/// <summary>Show the name provided to the constructor.</summary>
		/// <param name="sliPro"></param>
		/// <param name="position">To pass to <see cref="SliPro.SliPro.SetSegment"/>.</param>
		public void ShowName(SliPro.SliPro sliPro, SegmentDisplayPosition position)
		{
			sliPro.SetSegment(position, 0, SliPro.Constants.segmentDisplayWidth, m_shortName);
		}

		/// <summary>Process game data from SimHub.</summary>
		/// <remarks>
		/// The implementation of this function should probably call <see cref="SliPro.SliPro.SetSegment"/>.
		/// </remarks>
		/// <param name="pluginManager"></param>
		/// <param name="normalizedData"></param>
		/// <param name="sliPro"></param>
		/// <param name="position">To pass to <see cref="SliPro.SliPro.SetSegment"/>.</param>
		public abstract void ProcessData(PluginManager pluginManager, NormalizedData normalizedData, SliPro.SliPro sliPro,
			SegmentDisplayPosition position);
	}
}
