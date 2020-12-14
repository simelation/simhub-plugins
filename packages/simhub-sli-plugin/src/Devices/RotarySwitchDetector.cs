/*
 * Rotary switch detection.
 */

using System;
using System.Threading;

// ---------------------------------------------------------------------------------------------------------------------------------

namespace SimElation.SliDevices
{
	/// <summary>Class to handle detecting rotary switches changing position.</summary>
	public sealed class RotarySwitchDetector : IDisposable
	{
		/// <summary>Offset value for undetected rotary switch.</summary>
		public const int unknownIndex = -1;

		/// <summary>Callback type for when a rotary switch is detected, or times out.</summary>
		/// <param name="rotarySwitchIndex">
		/// The index of the detected rotary switch.
		/// If no rotary is found when the detection times out, the callback is invoked with <see cref="unknownIndex"/>.
		/// </param>
		public delegate void Callback(int rotarySwitchIndex);

		/// <summary>Constructor.</summary>
		/// <param name="timeoutMs">How long to detect for, in milliseconds.</param>
		/// <param name="callback">Callback for when a rotary switch is detected.</param>
		/// <param name="deviceDescriptor">The device descriptor, for input report format.</param>
		public RotarySwitchDetector(int timeoutMs, Callback callback, IDeviceDescriptor deviceDescriptor)
		{
			m_callback = callback;
			m_deviceDescriptor = deviceDescriptor;

			// Cancelleation timer for nothing found.
			m_timer = new Timer((object state) =>
			{
				if (!m_isDisposed && !m_isFound)
					m_callback(unknownIndex);
			}, null, timeoutMs, Timeout.Infinite);

			m_previousPositions = new int[(int)m_deviceDescriptor.Constants.MaxNumberOfRotarySwitches];
			for (int i = 0; i < m_previousPositions.Length; ++i)
			{
				m_previousPositions[i] = -1;
			}
		}

		/// <summary>Dispose.</summary>
		public void Dispose()
		{
			if (m_isDisposed)
				return;

			// TODO does disposing timer invoke its callback?
			m_timer.Dispose();

			m_isDisposed = true;
		}

		/// <summary>Process a HidReport data buffer and look for a rotary switch change.</summary>
		/// <param name="rxBuffer">Received <see cref="HidLibrary.HidReport.Data"/> from the SLI.</param>
		public void ProcessHidReport(byte[] rxBuffer)
		{
			if (m_isFound)
				return;

			// Note rotary switch position is a uint16 in the InputReport but we are only reading the low byte (0-11).
			for (int i = 0; i < m_deviceDescriptor.Constants.MaxNumberOfRotarySwitches; ++i)
			{
				var offset = m_deviceDescriptor.InputReport.RotarySwitchesOffset + (i * sizeof(ushort));

				if (offset < rxBuffer.Length)
				{
					int newPosition = rxBuffer[offset];

					if (-1 != m_previousPositions[i])
					{
						if (newPosition != m_previousPositions[i])
						{
							m_isFound = true;
							m_callback(i);
							return;
						}
					}

					m_previousPositions[i] = newPosition;
				}
			}
		}

		/// <summary>Display indexed from 1 for UI purposes.</summary>
		/// <param name="rotarySwitchIndex"></param>
		public static int RotarySwitchIndexToUiValue(int rotarySwitchIndex)
		{
			return rotarySwitchIndex + 1;
		}

		private bool m_isDisposed = false;

		private readonly Callback m_callback;
		private readonly Timer m_timer;

		private readonly int[] m_previousPositions;
		private bool m_isFound = false;

		private readonly IDeviceDescriptor m_deviceDescriptor;
	}
}
