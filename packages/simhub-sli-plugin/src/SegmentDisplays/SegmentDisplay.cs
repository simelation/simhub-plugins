/*
 * Segment display base functionality.
 */

using System;
using SimElation.SliDevices;

// ---------------------------------------------------------------------------------------------------------------------------------

// Ambiguous reference in cref attribute.
#pragma warning disable CS0419

namespace SimElation.Simhub.SliPlugin
{
	/// <summary>Base class for a segment display of some data.</summary>
	/// <remarks>
	/// Left segment display has decimals at indices  0, 1, 2, 4 and primes at 3, 5.
	/// Right segment display has decimals at indices 0, 2, 3, 4 and primes at 1, 5.
	/// </remarks>
	public abstract class SegmentDisplay
	{
		/// <summary>Name for the segment display, to show in UI.</summary>
		public String FriendlyName { get; }

		/// <summary>Constructor.</summary>
		/// <param name="shortName">Description of the information show by the display. This will be displayed when switching
		/// to this segment display for a period of time.</param>
		/// <param name="friendlyName">Longer form name for the segment display, to show in UI.</param>
		public SegmentDisplay(String shortName, String friendlyName)
		{
			m_shortName = shortName;
			FriendlyName = friendlyName;
		}

		/// <summary>Show the name provided to the constructor.</summary>
		/// <param name="sliDevice"></param>
		/// <param name="position">To pass to <see cref="Device.SetSegment"/>.</param>
		public void ShowName(Device sliDevice, Device.SegmentDisplayPosition position)
		{
			sliDevice.SetSegment(position, m_shortName);
		}

		/// <summary>Process game data from SimHub.</summary>
		/// <remarks>
		/// The implementation of this function should probably call <see cref="Device.SetSegment"/>.
		/// </remarks>
		/// <param name="normalizedData"></param>
		/// <param name="position">Whether the segment is display on the left or right. Can affect decimal/primes on the SLI-Pro.
		/// </param>
		/// <param name="outputFormatters"></param>
		/// <param name="str">Reference to a string to assign to.</param>
		/// <param name="decimalOrPrimeIndexList">To assign to if decimal (or primes with SLI-Pro) are to be set. Should be a
		/// list of indexes.</param>
		public abstract void ProcessData(NormalizedData normalizedData, Device.SegmentDisplayPosition position,
			IOutputFormatters outputFormatters, ref String str, ref uint[] decimalOrPrimeIndexList);

		private readonly String m_shortName;
	}
}

// Ambiguous reference in cref attribute.
#pragma warning restore CS0419
