/*
 * SLI-Pro interface.
 */

using System;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using HidLibrary;
using Newtonsoft.Json;

// ---------------------------------------------------------------------------------------------------------------------------------

namespace SimElation.SliPro
{
	/// <summary>Various constant values for the SLI-Pro.</summary>
	public static class Constants
	{
		/// <summary>Vendor id for an SLI-Pro.</summary>
		public const int vendorId = 0x1dd2;

		/// <summary>Product id for an SLI-Pro.</summary>
		public const int productId = 0x0103;

		/// <summary>The number of characters in each segment display.</summary>
		public const uint segmentDisplayWidth = 6;

		/// <summary>The number of LEDs in the rev display.</summary>
		public const uint numberOfRevLeds = 13;

		/// <summary>The number of supported rotary switches.</summary>
		public const uint maxNumberOfRotarySwitches = 6;

		/// <summary>The number of supported potentiometers.</summary>
		public const uint maxNumberOfPots = 2;
	}

	/// <summary>The segment display positions.</summary>
	public enum SegmentDisplayPosition
	{
		/// <summary>The left segment display.</summary>
		left,

		/// <summary>The right segment display.</summary>
		right,

		/// <summary>Number of supported positions.</summary>
		count
	}

	/// <summary>Supported rotary switches for detection.</summary>
	public enum RotarySwitch
	{
		/// <summary>Rotary switch to control the left segment display.</summary>
		leftSegment,

		/// <summary>Rotary switch to control the right segment display.</summary>
		rightSegment,

		/// <summary>Rotary switch to control the brightness.</summary>
		brightness,

		/// <summary>Number of supported control rotaries.</summary>
		count
	}

	/// <summary>Interface to control an SLI-Pro board (https://www.leobodnar.com/products/SLI-PRO/).</summary>
	/// <remarks>
	/// Provides control over all LEDs and segment displays, and detection of rotary switches
	/// for changing brightness and the left/right segment displays.
	/// </remarks>
	public class SliPro : IDisposable
	{
		/// <summary>Public settings for the SLI-Pro.</summary>
		public class Settings : INotifyPropertyChanged
		{
			/// <summary>Offsets into  <see cref="InputReport"/> for supported rotary switches.</summary>
			public class RotarySwitchOffsetsImpl : INotifyPropertyChanged
			{
				/// <summary>Array indexer.</summary>
				/// <param name="rotarySwitch"></param>
				/// <returns>The offset into the <see cref="InputReport"/> for <see cref="RotarySwitch"/></returns>
				public int this[RotarySwitch rotarySwitch]
				{
					get => m_offsets[(int)rotarySwitch];
					set
					{
						m_offsets[(int)rotarySwitch] = value;
						OnPropertyChanged();
					}
				}

				/// <inheritdoc/>
				public event PropertyChangedEventHandler PropertyChanged;

				/// <inheritdoc/>
				protected void OnPropertyChanged([CallerMemberName] string name = null)
				{
					// TODO can't get Item[0] etc. working so fire change for all array elements.
					PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name + "[]"));
				}

				[JsonProperty]
				private int[] m_offsets = new int[(int)RotarySwitch.count]
				{
					RotaryDetector.undefinedOffset,
					RotaryDetector.undefinedOffset,
					RotaryDetector.undefinedOffset
				};
			}

			/// <summary>RotarySwitchOffsetsImpl accessor.</summary>
			public RotarySwitchOffsetsImpl RotarySwitchOffsets
			{
				get => m_rotarySwitchOffsets;
			}

			/// <summary>Offsets of the rotary switch state in received HidReport data.</summary>
			/// <remarks>
			/// Initial state is no rotary switch control over brightness or segments displays.
			/// </remarks>
			private RotarySwitchOffsetsImpl m_rotarySwitchOffsets = new RotarySwitchOffsetsImpl();

			/// <summary>Brightness property.</summary>
			public byte? Brightness
			{
				get => m_brightness;

				set
				{
					if (m_brightness != value)
					{
						m_brightness = value;
						OnPropertyChanged();
					}
				}
			}

			/// <inheritdoc/>
			public event PropertyChangedEventHandler PropertyChanged;

			/// <inheritdoc/>
			protected void OnPropertyChanged([CallerMemberName] string name = null)
			{
				PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
			}

			/// <summary>Optional brightness.</summary>
			private byte? m_brightness = null;
		}

		/// <summary>Delegate to access a <see cref="log4net.ILog"/> object for logging purposes.</summary>
		/// <returns><see cref="log4net.ILog"/> object.</returns>
		public delegate log4net.ILog GetLog();

		/// <summary>Delegate to call when a rotary switch changes position.</summary>
		/// <param name="rotarySwitch">The rotary which has changed.</param>
		/// <param name="previousPosition">Its previous position. May be -1 on startup.</param>
		/// <param name="newPosition">Its new position, indexed from 0.</param>
		public delegate void RotarySwitchChange(RotarySwitch rotarySwitch, int previousPosition, int newPosition);

		private Settings m_settings;
		private GetLog m_getLog;
		private RotarySwitchChange m_rotarySwitchChangeCallback;

		private HidDevice m_device;
		private Task<bool> m_txTask = null;

		private HidReport m_ledHidReport = new HidReport((int)LedStateReport.length + 1);
		private HidReport m_prevLedHidReport = new HidReport((int)LedStateReport.length + 1);
		private HidReport m_brightnessHidReport = new HidReport((int)BrightnessReport.length + 1);

		private int[] m_rotarySwitchPositions = new int[(int)RotarySwitch.count] { -1, -1, -1 };
		private RotaryDetector m_rotaryDetector = null;

		private bool m_isAvailable = false;
		private bool m_isDisposed = false;

		/// <summary>Constructor.</summary>
		/// <param name="settings">Settings for the SLI-Pro.</param>
		/// <param name="getLog">Function to use for logging.</param>
		/// <param name="rotarySwitchChangeCallback">Callback for when a rotary switch changes position.</param>
		public SliPro(Settings settings, GetLog getLog, RotarySwitchChange rotarySwitchChangeCallback)
		{
			getLog().InfoFormat("SLI-Pro: constructing in thread {0}", Thread.CurrentThread.ManagedThreadId);

			m_settings = settings;
			m_getLog = getLog;
			m_rotarySwitchChangeCallback = rotarySwitchChangeCallback;

			// Initial device is searched for on timer, in case it's not plugged in yet.
			Timer timer = new Timer((object state) =>
				{
					if (m_device != null)
						return;

					m_getLog().Info("SLI-Pro: searching for device");

					// TODO multiple device support, I guess.
					m_device = HidDevices.Enumerate(Constants.vendorId, Constants.productId).FirstOrDefault();

					if (m_device == null)
					{
						// Try again on next timer.
						m_getLog().Warn("SLI-Pro: no device");
						return;
					}

					((Timer)state).Dispose();

					m_getLog().InfoFormat("SLI-Pro: {0} found at {1}", m_device.Description, m_device.DevicePath);
					m_device.Inserted += OnInserted;
					m_device.Removed += OnRemoved;
					m_device.MonitorDeviceEvents = true;
					m_device.OpenDevice();

					// No need to init here, "Inserted" event will fire.
				});
			timer.Change(500, 1000);

			for (RotarySwitch i = 0; i < RotarySwitch.count; ++i)
			{
				m_getLog().InfoFormat("SLI-Pro: rotary switch {0} initial offset {1}", i, m_settings.RotarySwitchOffsets[i]);
			}

			m_settings.PropertyChanged +=
				(object sender, PropertyChangedEventArgs e) =>
				{
					switch (e.PropertyName)
					{
						case nameof(m_settings.Brightness):
							if ((m_settings.Brightness != null) && IsAvailable)
								SendBrightness((byte)m_settings.Brightness);
							break;

						default:
							break;

					}
				};
		}

		/// <summary>Dispose.</summary>
		/// <remarks>
		/// If open, clears all the LEDs and closes the device.
		/// </remarks>
		public void Dispose()
		{
			Dispose(true);
		}

		/// <summary>Dispose.</summary>
		/// <remarks>
		/// If open, clears all the LEDs and closes the device.
		/// </remarks>
		/// <param name="isDisposing"></param>
		protected virtual void Dispose(bool isDisposing)
		{
			if (m_isDisposed)
				return;

			if (isDisposing)
			{
				m_getLog().InfoFormat("SLI-Pro::Dispose() from thread {0}", Thread.CurrentThread.ManagedThreadId);

				if (m_isAvailable)
				{
					ResetLedState();
					SendLedState();

					m_device.CloseDevice();
					m_device = null;
					m_isAvailable = false;
				}

				if (m_rotaryDetector != null)
				{
					m_rotaryDetector.Dispose();
					m_rotaryDetector = null;
				}
			}

			m_isDisposed = true;
		}

		/// <summary>Is the SLI-Pro available?</summary>
		public bool IsAvailable { get => m_isAvailable; }

		/// <summary>Reset the internal LED state.</summary>
		/// <remarks>
		/// No write to the device is performed; <see cref="SendLedState"/> would need to be called.
		/// Additionally, brightness is not altered.
		/// </remarks>
		public void ResetLedState()
		{
			ResetLedHidReport(m_ledHidReport);
		}

		/// <summary>Set the brightness of the device's LEDs.</summary>
		/// <remarks>
		/// Note this DOES send the brightness message to the device and as such assumes <see cref="IsAvailable"/> is true.
		/// </remarks>
		/// <param name="level">The brightness between 0 (dullest) and 254 (brightest).</param>
		public void SendBrightness(byte level)
		{
			m_brightnessHidReport.ReportId = 0;

			var txBuffer = m_brightnessHidReport.Data;
			txBuffer[BrightnessReport.reportTypeIndex] = BrightnessReport.reportType;
			txBuffer[BrightnessReport.brightnessIndex] = level;

			// Not going to worry about async for brightness control!
			if (!m_device.WriteReport(m_brightnessHidReport))
				m_getLog().Warn("SLI-Pro: WriteReport() failed");
		}

		/// <summary>Set a text message using the segment displays and gear indicator.</summary>
		/// <remarks>
		/// If the message is more than 13 characters, the beginning and end of the string are displayed.
		/// Naturally some characters will not display correctly on a figure-8 LED segment!
		/// This call does not affect other LEDs than the segment displays; you may wish to call <see cref="ResetLedState"/>first.
		/// No write to the device is performed; <see cref="SendLedState"/> would need to be called.
		/// </remarks>
		/// <param name="str"></param>
		public void SetTextMessage(String str)
		{
			// Split the message across the left and right segment displays and use the gear indicator if need be.
			int substringLength = Math.Min(str.Length / 2, (int)Constants.segmentDisplayWidth);
			String left = str.Substring(0, substringLength);
			String right = str.Substring(str.Length - substringLength);

			m_ledHidReport.Data[LedStateReport.gearIndex] =
				(byte)(((left.Length + right.Length) != str.Length) ? str[substringLength] : ' ');

			SetSegment(SegmentDisplayPosition.left, 0, Constants.segmentDisplayWidth,
				left.PadLeft((int)Constants.segmentDisplayWidth));
			SetSegment(SegmentDisplayPosition.right, 0, Constants.segmentDisplayWidth,
				right.PadRight((int)Constants.segmentDisplayWidth));
		}

		/// <summary>Set the contents of a segment display.</summary>
		/// <param name="segmentDisplayPosition">The left or right segment display.</param>
		/// <param name="firstCharacterOffset">Offset (from 0) of the first character to set in the display.</param>
		/// <param name="length">The number of characters to set</param>
		/// <param name="str">The string to display. If shorter than <paramref name="length"/>, it is space padded.</param>
		/// <param name="decimalOrPrimeIndexList">
		/// Indexes for decimal/prime indicators to set. Index 0 is the first character in
		/// the segment display, not the <paramref name="firstCharacterOffset"/> character.
		/// </param>
		public void SetSegment(SegmentDisplayPosition segmentDisplayPosition, uint firstCharacterOffset, uint length, String str,
			params uint[] decimalOrPrimeIndexList)
		{
			switch (segmentDisplayPosition)
			{
				case SegmentDisplayPosition.left:
					InternalSetSegment(LedStateReport.leftSegmentIndex, Constants.segmentDisplayWidth, firstCharacterOffset,
						length, str, decimalOrPrimeIndexList);
					break;

				case SegmentDisplayPosition.right:
					InternalSetSegment(LedStateReport.rightSegmentIndex, Constants.segmentDisplayWidth, firstCharacterOffset,
						length, str, decimalOrPrimeIndexList);
					break;
			}
		}

		/// <summary>
		/// Set the gear indicator.
		/// </summary>
		/// <param name="value"></param>
		public void SetGear(String value)
		{
			m_ledHidReport.Data[LedStateReport.gearIndex] = (byte)((value.Length == 1) ? Char.ToLower(value[0]) : '-');
		}

		/// <summary>Set some rev LEDs based on an rpm %.</summary>
		/// <remarks>
		/// The state for any rev LEDs outside of the range specified by <paramref name="firstLedIndex"/> and
		/// <paramref name="maxNumberOfLeds"/> is not altered.
		/// </remarks>
		/// <param name="minRpmPercent">The rpm % at which the first rev LED should light.</param>
		/// <param name="rpmPercent">The rpm %.</param>
		/// <param name="firstLedIndex">The 0-based index of the first rev LED to set.</param>
		/// <param name="maxNumberOfLeds">The number of rev LEDs to light when at 100% <paramref name="rpmPercent"/>.</param>
		/// <returns>
		/// false if <paramref name="firstLedIndex"/> + <paramref name="maxNumberOfLeds"/> is out of range,
		/// <paramref name="minRpmPercent"/> is >= 100, <paramref name="rpmPercent"/> is > 100.</returns>
		public bool SetRevLeds(double minRpmPercent, double rpmPercent, uint firstLedIndex = 0,
			uint maxNumberOfLeds = Constants.numberOfRevLeds)
		{
			if ((firstLedIndex + maxNumberOfLeds) > Constants.numberOfRevLeds)
				return false;

			if ((minRpmPercent >= 100) || (rpmPercent > 100))
				return false;

			uint numberOfSetLeds = 0;

			if (rpmPercent >= minRpmPercent)
			{
				double val = ((rpmPercent - minRpmPercent) * (maxNumberOfLeds - firstLedIndex)) / (100 - minRpmPercent);
				numberOfSetLeds = (uint)Math.Round(val, MidpointRounding.AwayFromZero);
			}

			for (uint i = firstLedIndex; i < maxNumberOfLeds; ++i)
			{
				m_ledHidReport.Data[LedStateReport.revLed1Index + i] = (byte)(((i - firstLedIndex) < numberOfSetLeds) ? 1 : 0);
			}

			return true;
		}

		/// <summary>Set ALL rev LEDs based on an array of bools.</summary>
		/// <remarks>
		/// The <paramref name="ledStates"/> array can contain fewer elements than <see cref="Constants.numberOfRevLeds"/>;
		/// the remaining LEDs will be cleared.
		/// </remarks>
		/// <param name="ledStates"></param>
		public void SetRevLeds(bool[] ledStates)
		{
			for (uint i = 0; i < Constants.numberOfRevLeds; ++i)
			{
				m_ledHidReport.Data[LedStateReport.revLed1Index + i] = (byte)((i < ledStates.Length) ? (ledStates[i] ? 1 : 0) : 0);
			}
		}

		/// <summary>Set an individual rev LED.</summary>
		/// <param name="ledIndex">The 0-based index of the rev LED's state to set.</param>
		/// <param name="isSet"></param>
		/// <returns>false if <paramref name="ledIndex"/> is out of range, otherwise true.</returns>
		public bool SetRevLed(uint ledIndex, bool isSet)
		{
			if (ledIndex >= Constants.numberOfRevLeds)
				return false;

			m_ledHidReport.Data[LedStateReport.revLed1Index + ledIndex] = (byte)(isSet ? 1 : 0);

			return true;
		}

		/// <summary>Set a status LED.</summary>
		/// <remarks>
		/// 0-2 are the indexes of the status LEDs on the left of the display.
		/// 3-5 are the indexes of the status LEDs on the right of the display.
		/// </remarks>
		/// <param name="ledIndex">The 0-based index of the status LED's state to set.</param>
		/// <param name="isSet"></param>
		/// <returns>false if <paramref name="ledIndex"/> is out of range, otherwise true.</returns>
		public bool SetStatusLed(uint ledIndex, bool isSet)
		{
			if (ledIndex >= LedStateReport.statusLedCount)
				return false;

			m_ledHidReport.Data[LedStateReport.statusLed1Index + ledIndex] = (byte)(isSet ? 1 : 0);

			return true;
		}

		/// <summary>Set an external LED.</summary>
		/// <param name="ledIndex">The 0-based index of the external LED's state to set.</param>
		/// <param name="isSet"></param>
		/// <returns>false if <paramref name="ledIndex"/> is out of range, otherwise true.</returns>
		public bool SetExternalLed(uint ledIndex, bool isSet)
		{
			if (ledIndex >= LedStateReport.externalLedCount)
				return false;

			m_ledHidReport.Data[LedStateReport.externalLed1Index + ledIndex] = (byte)(isSet ? 1 : 0);

			return true;
		}

		/// <summary>Send current LED state to the deivce.</summary>
		/// <remarks>
		/// This call assumes <see cref="IsAvailable"/> is true.
		/// </remarks>
		public async void SendLedState()
		{
			if (m_txTask != null)
			{
				if (!m_txTask.IsCompleted)
				{
					m_getLog().Warn("SLI-Pro: skipping write; previous hasn't completed");
					return;
				}

				try
				{
					await m_txTask;
				}
				catch (Exception e)
				{
					m_getLog().Warn("SLI-Pro: WriteAsync() failed {0}", e);
				}
			}

			if (Buffer.Equals(m_ledHidReport.Data, m_prevLedHidReport.Data))
				return;

			m_txTask = m_device.WriteReportAsync(m_ledHidReport);

			// Swap over buffers. Don't clear the new buffer in case it we end up not needing to send it if is
			// the same as the previous (pretty unlikely in game though).
			(m_prevLedHidReport, m_ledHidReport) = (m_ledHidReport, m_prevLedHidReport);
		}

		/// <summary>Initiate a detection for a rotary switch.</summary>
		/// <remarks>
		/// The object will monitor received reports from the device for changes.
		/// </remarks>
		/// <param name="rotarySwitch">Which rotary switch operation to assign to the physically moved switch.</param>
		/// <param name="timeoutMs">How long to attempt to detect the rotary for, in milliseconds.</param>
		public void LearnRotary(RotarySwitch rotarySwitch, int timeoutMs = 5000)
		{
			m_getLog().InfoFormat("SLI-Pro: detecting rotary {0}...", rotarySwitch);

			if (m_rotaryDetector != null)
				m_rotaryDetector.Dispose();

			m_rotaryDetector = new RotaryDetector(timeoutMs, (int offset) =>
				{
					if (offset == RotaryDetector.undefinedOffset)
						m_getLog().InfoFormat("SLI-Pro: no rotary detected for {0}", rotarySwitch);
					else
						m_getLog().InfoFormat("SLI-Pro: setting buffer offset for {0} to {1}", rotarySwitch, offset);

					m_settings.RotarySwitchOffsets[rotarySwitch] = offset;

					m_rotaryDetector.Dispose();
					m_rotaryDetector = null;
				});
		}

		/// <summary>Get the current position of a rotary switch.</summary>
		/// <param name="rotarySwitch"></param>
		/// <returns>The current position of the rotary, or -1 if unknown or rotary not assigned.</returns>
		public int GetRotarySwitchPosition(RotarySwitch rotarySwitch)
		{
			return m_rotarySwitchPositions[(int)rotarySwitch];
		}

		/// <summary>Forget learned offset for a rotary switch.</summary>
		/// <param name="rotarySwitch">Which rotary switch to forget.</param>
		public void ForgetRotary(RotarySwitch rotarySwitch)
		{
			m_settings.RotarySwitchOffsets[rotarySwitch] = RotaryDetector.undefinedOffset;
		}

		private void InternalSetSegment(uint segmentIndex, uint segmentLength, uint offset, uint length, String value,
			params uint[] decimalOrPrimeIndexList)
		{
			for (uint i = offset; i < segmentLength; ++i)
			{
				int stringIndex = (int)(i - offset);
				m_ledHidReport.Data[segmentIndex + i] = (byte)((stringIndex < value.Length) ? value[stringIndex] : ' ');
			}

			foreach (uint i in decimalOrPrimeIndexList)
			{
				if (i < segmentLength)
					m_ledHidReport.Data[segmentIndex + i] |= LedStateReport.segmentDecimalOrPrimeBit;
			}
		}

		private void OnInserted()
		{
			if (m_isAvailable)
			{
				m_getLog().Warn("SLI-Pro: ignoring inserted event when already available");
				return;
			}

			m_getLog().Info("SLI-Pro: inserted event");

			// Initialize both tx buffers for report type 1.
			ResetLedHidReport(m_ledHidReport);
			ResetLedHidReport(m_prevLedHidReport);

			m_isAvailable = true;

			// Explicit brightness set in config, so use that (and ignore any rotary).
			if (m_settings.Brightness != null)
				SendBrightness((byte)m_settings.Brightness);

			// Start reading reports from the board.
			ReadHidReport();
		}

		private void OnRemoved()
		{
			m_getLog().InfoFormat("SLI-Pro: removed event thread {0}", Thread.CurrentThread.ManagedThreadId);

			if (!m_isAvailable)
			{
				m_getLog().Warn("SLI-Pro: inserted event when already available");
				return;
			}

			m_txTask = null;
			m_isAvailable = false;
		}

		private async void ReadHidReport()
		{
			var startId = Thread.CurrentThread.ManagedThreadId;

			while (m_isAvailable)
			{
				try
				{
					var task = m_device.ReadReportAsync();
					var hidReport = await task;

					//m_getLog().InfoFormat("SLI-Pro: read task completed in thread {0}", Thread.CurrentThread.ManagedThreadId);

					// Ignore cancellation of read on a close.
					if (!m_isAvailable)
					{
						m_getLog().Warn("SLI-Pro: ignoring HidReport with device not available");
						return;
					}

					if ((task.Status == TaskStatus.RanToCompletion) && (hidReport.ReadStatus == HidDeviceData.ReadStatus.Success))
					{
						if (m_rotaryDetector == null)
						{
							ProcessRotarySwitch(RotarySwitch.leftSegment, hidReport.Data);
							ProcessRotarySwitch(RotarySwitch.rightSegment, hidReport.Data);
							ProcessRotarySwitch(RotarySwitch.brightness, hidReport.Data);
						}
						else
						{
							m_rotaryDetector.ProcessHidReport(hidReport.Data);
						}
					}
				}
				catch (Exception e)
				{
					m_getLog().WarnFormat("SLI-Pro: ReadReportAsync() threw {0} in thread {1}", e,
						Thread.CurrentThread.ManagedThreadId);
				}
			}
		}

		private void ProcessRotarySwitch(RotarySwitch rotarySwitch, byte[] rxBuffer)
		{
			int bufferOffset = m_settings.RotarySwitchOffsets[rotarySwitch];

			if ((bufferOffset < 0) || (bufferOffset >= rxBuffer.Length))
				return;

			int newPosition = (int)rxBuffer[bufferOffset];
			ref int previousPosition = ref m_rotarySwitchPositions[(int)rotarySwitch];

			if (newPosition != previousPosition)
			{
				// Swap first in case callback throws.
				(newPosition, previousPosition) = (previousPosition, newPosition);
				m_rotarySwitchChangeCallback(rotarySwitch, newPosition, previousPosition);
			}
		}

		private static void ResetLedHidReport(HidReport hidReport)
		{
			var txBuffer = hidReport.Data;

			// Zero out the buffer.
			Array.Clear(txBuffer, 0, txBuffer.Length);
			hidReport.ReportId = 0;

			txBuffer[LedStateReport.reportTypeIndex] = LedStateReport.reportType;

			// Spaces for the segment displays.
			txBuffer[LedStateReport.gearIndex] = (byte)' ';

			for (uint i = 0; i < Constants.segmentDisplayWidth; ++i)
			{
				txBuffer[LedStateReport.leftSegmentIndex + i] = (byte)' ';
			}

			for (uint i = 0; i < Constants.segmentDisplayWidth; ++i)
			{
				txBuffer[LedStateReport.rightSegmentIndex + i] = (byte)' ';
			}
		}
	}
} // namespace SimElation.SliPro
