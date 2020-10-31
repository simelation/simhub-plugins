/*
 * SLI-Pro rotary detection.
 */

using System;
using System.Threading;

// ---------------------------------------------------------------------------------------------------------------------------------

namespace SimElation.SliPro
{
	/// <summary>Class to handle detecting rotary switches changing position.</summary>
	class RotaryDetector : IDisposable
	{
		/// <summary>Callback type for when a rotary switch is detected, or times out.</summary>
		/// <param name="offset">
		/// The offset of the rotary switch data within a <see cref="HidLibrary.HidReport"/> data buffer.
		/// If no rotary is found when the detection times out, the callback is invoked with <see cref="undefinedOffset"/>.
		/// </param>
		public delegate void Callback(int offset);

		/// <summary>Offset value for undetected rotary.</summary>
		public const int undefinedOffset = -1;

		private readonly Callback m_callback;
		private readonly Timer m_timer;

		private readonly int[] m_previousRotaryPositions =
			new int[(int)Constants.maxNumberOfRotarySwitches] { -1, -1, -1, -1, -1, -1 };
		private int m_rotaryOffset = undefinedOffset;
		private bool m_isDisposed = false;

		/// <summary>Constructor.</summary>
		/// <param name="timeoutMs">How long to detect for, in milliseconds.</param>
		/// <param name="callback">Callback for when a rotary switch is detected.</param>
		public RotaryDetector(int timeoutMs, Callback callback)
		{
			m_callback = callback;

			// Cancelleation timer for nothing found.
			m_timer = new Timer((object state) =>
			{
				if (!m_isDisposed && (m_rotaryOffset == undefinedOffset))
					m_callback(undefinedOffset);
			}, null, timeoutMs, Timeout.Infinite);
		}

		/// <summary>Dispose.</summary>
		public void Dispose()
		{
			Dispose(true);
		}

		protected virtual void Dispose(bool isDisposing)
		{
			if (m_isDisposed)
				return;

			if (isDisposing)
				m_timer.Dispose();

			m_isDisposed = true;
		}

		/// <summary>Process a HidReport data buffer and look for a rotary switch change.</summary>
		/// <param name="rxBuffer">Received <see cref="HidLibrary.HidReport.Data"/> from the SLI-Pro.</param>
		public void ProcessHidReport(byte[] rxBuffer)
		{
			if (m_rotaryOffset != undefinedOffset)
				return;

			// Rotary position is a uint16 in the InputReport but we are only reading the low byte.
			for (uint i = 0; i < Constants.maxNumberOfRotarySwitches; ++i)
			{
				var offset = InputReport.rotarySwitchesOffset + (i * sizeof(ushort));

				if (offset < rxBuffer.Length)
				{
					int newPosition = rxBuffer[offset];

					if (-1 != m_previousRotaryPositions[i])
					{
						if (newPosition != m_previousRotaryPositions[i])
						{
							m_rotaryOffset = (int)offset;
							m_callback(m_rotaryOffset);
							return;
						}
					}

					m_previousRotaryPositions[i] = newPosition;
				}
			}
		}
	}

} // namespace SimElation.SliPro
