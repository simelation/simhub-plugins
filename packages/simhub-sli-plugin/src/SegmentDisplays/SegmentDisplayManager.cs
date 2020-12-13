/*
 * Segment display manager.
 */

using System;
using System.Collections.Generic;
using System.Threading;
using SimElation.SliDevices;
using SimHub.Plugins;

// ---------------------------------------------------------------------------------------------------------------------------------

namespace SimElation.Simhub.SliPlugin
{
	/// <summary>Class to manage left/right segment displays.</summary>
	public class SegmentDisplayManager
	{
		/// <summary>Class wrapping the property for name of a display mode for the UI.</summary>
		/// <remarks>
		/// Needed to be able to use the name in xaml for the peek configuration buttons.
		/// Surely there's some util classes that are just property wrappers over a String...
		/// </remarks>
		public sealed class SegmentDisplayMode
		{
			/// <summary>Property for name of a display mode for the UI.</summary>
			public String FriendlyName { get; set; }
		};

		/// <summary>A list of the segment display modes for presenting in the UI.</summary>
		public SegmentDisplayMode[] FriendlyNameList
		{
			get => Array.ConvertAll(m_segmentDisplayList,
				(SegmentDisplay segmentDisplay) => new SegmentDisplayMode() { FriendlyName = segmentDisplay.FriendlyName });
		}

		/// <summary>Constrictor.</summary>
		/// <param name="pluginManager"></param>
		/// <param name="deviceInstance"></param>
		/// <param name="position"></param>
		/// <param name="outputFormatters"></param>
		/// <param name="segmentDisplayList"></param>
		public SegmentDisplayManager(PluginManager pluginManager, DeviceInstance deviceInstance,
			Device.SegmentDisplayPosition position, IOutputFormatters outputFormatters, SegmentDisplay[] segmentDisplayList)
		{
			m_position = position;
			m_segmentDisplayList = segmentDisplayList;
			m_outputFormatters = outputFormatters;
			m_timer = new Timer((object state) => m_showNameTimer = false);

			// Actions for peek.
			foreach (var item in segmentDisplayList)
			{
				pluginManager.AddAction<SliPlugin>(deviceInstance.MakeActionName(item.FriendlyName),
					(_, __) =>
					{
						// Start peeking at a different mode than current on future game updates.
						// Note we don't show the peeked mode's name for a time like we do when the current mode is changed -
						// user probably wants to see the data RIGHT NOW.
						int index = Array.FindIndex(m_segmentDisplayList,
							(segmentDisplay) => segmentDisplay.FriendlyName == item.FriendlyName);
						if (index != -1)
							m_peekList.Add(index);
					},
					(_, __) =>
					{
						// Stop peeking.
						int index = Array.FindIndex(m_segmentDisplayList,
							(segmentDisplay) => segmentDisplay.FriendlyName == item.FriendlyName);
						if (index != -1)
							m_peekList.Remove(index);
					});
			}
		}

		/// <summary>Called from <see cref="IDataPlugin.DataUpdate"/> when a game is running and not paused.</summary>
		/// <param name="pluginManager"></param>
		/// <param name="normalizedData"></param>
		/// <param name="device">The device to set the display on.</param>
		public void ProcessData(PluginManager pluginManager, NormalizedData normalizedData, Device device)
		{
			int index = -1;
			var showNameTimer = m_showNameTimer;

			// Peek active?
			if (m_peekList.Count > 0)
			{
				index = m_peekList[m_peekList.Count - 1];

				// Don't show name after a mode change if peek is active.
				// NB "peek current mode name" still works, so you can peek a particular mode and hold the show name button...
				showNameTimer = false;
			}
			else
			{
				index = m_currentIndex;
			}

			if (ValidateIndex(index) != -1)
			{
				var segmentDisplay = m_segmentDisplayList[index];

				if ((m_showNameButtonCount > 0) || showNameTimer)
				{
					segmentDisplay.ShowName(device, m_position);
				}
				else
				{
					String str = "";
					uint[] decimalOrPrimeIndexList = s_decimalOrPrimeIndexListEmpty;

					segmentDisplay.ProcessData(normalizedData, m_position, m_outputFormatters, ref str,
						ref decimalOrPrimeIndexList);

					device.SetSegment(m_position, str, decimalOrPrimeIndexList);
				}
			}
			else
			{
				device.SetSegment(m_position, "n-a");
			}
		}

		/// <summary>Get the index of the display mode that is after the current one in the list. Handles cycling.</summary>
		public int GetNextIndex()
		{
			int newIndex = ValidateIndex(m_currentIndex + 1);
			return (newIndex == -1) ? 0 : newIndex;
		}

		/// <summary>Get the index of the display mode that is before the current one in the list. Handles cycling.</summary>
		public int GetPreviousIndex()
		{
			int newIndex = ValidateIndex(m_currentIndex - 1);
			return (newIndex == -1) ? (m_segmentDisplayList.Length - 1) : newIndex;
		}

		/// <summary>Set the current mode by index.</summary>
		/// <param name="index"></param>
		/// <param name="segmentNameTimeoutMs">How long to display the name of the new mode, rather than its data.</param>
		public void SetByIndex(int index, long segmentNameTimeoutMs)
		{
			m_currentIndex = index;

			if (segmentNameTimeoutMs > 0)
			{
				// For future data updates, show the name of the current segment until the timer fires.
				m_timer.Change(segmentNameTimeoutMs, Timeout.Infinite);
				m_showNameTimer = true;
			}
		}

		/// <summary>Display the current mode's name on future game updates.</summary>
		/// <param name="isPress">If true, start peeking at the current mode's name.</param>
		public void PeekName(bool isPress)
		{
			if (isPress)
				++m_showNameButtonCount;
			else
				m_showNameButtonCount = Math.Max(0, m_showNameButtonCount - 1);
		}

		/// <summary>Validate a display mode index.</summary>
		/// <param name="index"></param>
		/// <returns>The <paramref name="index"/>if it is valid, otherwise -1.</returns>
		private int ValidateIndex(int index)
		{
			return ((index >= 0) && (index < m_segmentDisplayList.Length)) ? index : -1;
		}

		private readonly Device.SegmentDisplayPosition m_position;
		private readonly SegmentDisplay[] m_segmentDisplayList;
		private readonly IOutputFormatters m_outputFormatters;

		private readonly Timer m_timer;
		private bool m_showNameTimer = false;
		private int m_showNameButtonCount = 0;
		private readonly List<int> m_peekList = new List<int>();
		private int m_currentIndex = -1;

		private readonly static uint[] s_decimalOrPrimeIndexListEmpty = { };
	}
}
