/*
 * SLI-F1 interface.
 */

using System;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using HidLibrary;

// ---------------------------------------------------------------------------------------------------------------------------------

namespace SimElation.SliF1
{
	/// <summary>Various constant values for the SLI-F1.</summary>
	public static class Constants
	{
		/// <summary>Vendor id for an SLI-F1.</summary>
		public const int vendorId = 0x1dd2;

		/// <summary>Product id for an SLI-F1.</summary>
		public const int productId = 0x1110;

		/// <summary>The number of characters in each segment display.</summary>
		public const uint segmentDisplayWidth = 4;

		/// <summary>The number of LEDs in the rev display.</summary>
		public const uint numberOfRevLeds = 15;

		/// <summary>The number of status LEDs (3 left, 3 right).</summary>
		public const uint numberOfStatusLeds = 6;

		/// <summary>The number of external LEDs.</summary>
		public const uint numberOfExternalLeds = 5;

		/// <summary>The number of supported rotary switches.</summary>
		/// <remarks>
		/// The 8th rotary is mapped to controller buttons. Looks like it's at offset 17 in the report but we don't need it.
		/// </remarks>
		public const uint maxNumberOfRotarySwitches = 7;

		/// <summary>The number of supported potentiometers.</summary>
		public const uint maxNumberOfPots = 1;
	}

	/// <summary>The segment display positions.</summary>
	public enum SegmentDisplayPosition
	{
		/// <summary>The left segment display.</summary>
		left,

		/// <summary>The right segment display.</summary>
		right
	}

	/// <summary>
	/// Interface to control an SLI-F1 board (http://www.leobodnar.com/shop/index.php?main_page=product_info&products_id=184/).
	/// </summary>
	/// <remarks>
	/// Provides control over all LEDs and segment displays, and detection of rotary switches
	/// for changing brightness and the left/right segment displays.
	/// </remarks>
	public class SliF1 : IDisposable, INotifyPropertyChanged
	{
		/// <summary>Public settings for the SLI-F1.</summary>
		public class Settings : INotifyPropertyChanged
		{
			/// <summary>Index of the rotary switch for brightness control.</summary>
			public int BrightnessRotarySwitchIndex
			{
				get => m_brightnessRotarySwitchIndex;

				set
				{
					m_brightnessRotarySwitchIndex = value;
					OnPropertyChanged();
				}
			}

			/// <summary>Number of positions in brightness rotary switch.</summary>
			public int NumberOfBrightnessRotaryPositions
			{
				get => m_numberOfBrightnessRotaryPositions;

				set
				{
					m_numberOfBrightnessRotaryPositions = value;
					OnPropertyChanged();
				}
			}

			/// <summary>Brightness property.</summary>
			public byte BrightnessLevel
			{
				get => m_brightnessLevel;

				set
				{
					m_brightnessLevel = value;
					OnPropertyChanged();
				}
			}

			/// <inheritdoc/>
			public event PropertyChangedEventHandler PropertyChanged;

			/// <inheritdoc/>
			protected void OnPropertyChanged([CallerMemberName] string name = null)
			{
				PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
			}

			private int m_brightnessRotarySwitchIndex = RotaryDetector.unknownIndex;

			private const int defaultNumberOfBrightnessRotaryPositions = 12;
			private int m_numberOfBrightnessRotaryPositions = defaultNumberOfBrightnessRotaryPositions;
			private byte m_brightnessLevel =
				SliF1.GetBrightnessLevelFromRotaryPosition(-1, defaultNumberOfBrightnessRotaryPositions);
		}

		/// <summary>Delegate to access a <see cref="log4net.ILog"/> object for logging purposes.</summary>
		/// <returns><see cref="log4net.ILog"/> object.</returns>
		public delegate log4net.ILog GetLog();

		/// <summary>Delegate to call when a rotary switch changes position.</summary>
		/// <param name="rotarySwitch">The rotary which has changed.</param>
		/// <param name="previousPosition">Its previous position. May be -1 on startup.</param>
		/// <param name="newPosition">Its new position, indexed from 0.</param>
		public delegate void RotarySwitchChangeCallback(int rotarySwitch, int previousPosition, int newPosition);

		/// <inheritdoc/>
		public event PropertyChangedEventHandler PropertyChanged;

		/// <inheritdoc/>
		protected void OnPropertyChanged([CallerMemberName] string name = null)
		{
			PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
		}

		/// <summary>Nice device status string to show in a UI.</summary>
		public String Status
		{
			get => m_status;

			set
			{
				if (m_status != value)
				{
					m_status = value;
					OnPropertyChanged();
				}
			}
		}

		private readonly Settings m_settings;
		private readonly GetLog m_getLog;
		private readonly RotarySwitchChangeCallback m_rotarySwitchChangeCallback;

		private CancellationTokenSource m_devicePollTaskCancellation = new CancellationTokenSource();
		private Task m_devicePollTask = null;
		private HidDevice m_device;
		private String m_deviceInfo;
		private Task<bool> m_txTask = null;

		private HidReport m_ledHidReport = new HidReport((int)LedStateReport.length + 1);
		private HidReport m_prevLedHidReport = new HidReport((int)LedStateReport.length + 1);
		private readonly HidReport m_brightnessHidReport = new HidReport((int)BrightnessReport.length + 1);

		private readonly int[] m_rotarySwitchPositions = new int[(int)Constants.maxNumberOfRotarySwitches]
			{ -1, -1, -1, -1, -1, -1, -1 };
		private RotaryDetector m_rotaryDetector = null;

		private bool m_isAvailable = false;
		private String m_status = "Initializing";
		private bool m_isDisposed = false;

		/// <summary>Constructor.</summary>
		/// <param name="settings">Settings for the SLI-F1.</param>
		/// <param name="getLog">Function to use for logging.</param>
		/// <param name="rotarySwitchChangeCallback">Callback for when a rotary switch changes position.</param>
		public SliF1(Settings settings, GetLog getLog, RotarySwitchChangeCallback rotarySwitchChangeCallback)
		{
			getLog().InfoFormat("SLI-F1: constructing in thread {0}", Thread.CurrentThread.ManagedThreadId);

			m_settings = settings;
			m_getLog = getLog;
			m_rotarySwitchChangeCallback = rotarySwitchChangeCallback;

			m_settings.PropertyChanged +=
				(object sender, PropertyChangedEventArgs e) =>
				{
					switch (e.PropertyName)
					{
						case nameof(m_settings.BrightnessRotarySwitchIndex):
							if (m_settings.BrightnessRotarySwitchIndex != RotaryDetector.unknownIndex)
							{
								// Changing to rotary switch control, so dig out position of the rotary switch and set the level.
								m_settings.BrightnessLevel = GetBrightnessLevelFromRotaryPosition(
									GetRotarySwitchPosition(m_settings.BrightnessRotarySwitchIndex),
									m_settings.NumberOfBrightnessRotaryPositions);
							}
							else if (IsAvailable)
							{
								SendBrightness(m_settings.BrightnessLevel);
							}
							break;

						case nameof(m_settings.BrightnessLevel):
							if (IsAvailable)
								SendBrightness(m_settings.BrightnessLevel);
							break;

						default:
							break;

					}
				};

			PollForDevices();
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
				m_getLog().InfoFormat("SLI-F1: Dispose() from thread {0}", Thread.CurrentThread.ManagedThreadId);

				if (m_devicePollTask != null)
				{
					m_devicePollTaskCancellation.Cancel();
					// Not disposing of m_devicePollTask as possibly we should await it after requesting cancellation,
					// but can't in Dispose().
					// TODO what is the correct thing to do?
					m_devicePollTask = null;
				}

				if (m_isAvailable)
				{
					ResetLedState();
					SendLedState();

					m_device.CloseDevice();
					m_device.Dispose();
					m_device = null;
					m_isAvailable = false;
					Status = "Disposed";
				}

				if (m_rotaryDetector != null)
				{
					m_rotaryDetector.Dispose();
					m_rotaryDetector = null;
				}
			}

			m_isDisposed = true;
		}

		/// <summary>Is the SLI-F1 available?</summary>
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
				m_getLog().Warn("SLI-F1: WriteReport() failed");
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
			if (ledIndex >= Constants.numberOfStatusLeds)
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
			if (ledIndex >= Constants.numberOfExternalLeds)
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
					m_getLog().Warn("SLI-F1: skipping write; previous hasn't completed");
					return;
				}

				try
				{
					await m_txTask;
				}
				catch (Exception e)
				{
					m_getLog().Warn("SLI-F1: WriteAsync() failed {0}", e);
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
		/// <param name="timeoutMs">How long to attempt to detect the rotary for, in milliseconds.</param>
		/// <returns>A waitable task.</returns>
		public Task<int> DetectRotary(int timeoutMs = 5000)
		{
			var task = new TaskCompletionSource<int>();

			m_getLog().InfoFormat("SLI-F1: detecting rotary in thread {0}...", Thread.CurrentThread.ManagedThreadId);

			if (m_rotaryDetector != null)
				m_rotaryDetector.Dispose();

			m_rotaryDetector = new RotaryDetector(timeoutMs,
				(int rotarySwitchIndex) =>
				{
					if (rotarySwitchIndex == RotaryDetector.unknownIndex)
					{
						m_getLog().InfoFormat("SLI-F1: no rotary detected in thread {0}", Thread.CurrentThread.ManagedThreadId);
					}
					else
					{
						m_getLog().InfoFormat("SLI-F1: detected rotary {0} in thread {1}", rotarySwitchIndex,
							Thread.CurrentThread.ManagedThreadId);
					}

					m_rotaryDetector.Dispose();
					m_rotaryDetector = null;

					task.SetResult(rotarySwitchIndex);
				});

			return task.Task;
		}

		/// <summary>Get the current position of a rotary switch.</summary>
		/// <param name="rotarySwitchIndex"></param>
		/// <returns>The current position of the rotary, or -1 if unknown.</returns>
		public int GetRotarySwitchPosition(int rotarySwitchIndex)
		{
			if ((rotarySwitchIndex >= 0) && (rotarySwitchIndex < m_rotarySwitchPositions.Length))
				return m_rotarySwitchPositions[rotarySwitchIndex];
			else
				return -1;
		}

		/// <summary>Calculate brightness level from rotary position.</summary>
		/// <param name="position"></param>
		/// <param name="numberOfRotaryPositions"></param>
		/// <returns>Brightness level, or a default if rotary is in an unknown position.</returns>
		public static byte GetBrightnessLevelFromRotaryPosition(int position, int numberOfRotaryPositions)
		{
			if (position < 0)
				return 100;

			--numberOfRotaryPositions;
			position = Math.Min(position, numberOfRotaryPositions);

			return (byte)((254.0 / numberOfRotaryPositions) * position);
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

		private async void PollForDevices()
		{
			do
			{
				try
				{
					m_devicePollTask = Task.Delay(1000, m_devicePollTaskCancellation.Token);

					await m_devicePollTask;
				}
				catch (Exception e)
				{
					m_getLog().InfoFormat("SLI-F1: exception {0} in {1}", e, nameof(PollForDevices));
					return;
				}
			}
			while (!PollForDevicesOnce());
		}

		private bool PollForDevicesOnce()
		{
			if (m_device != null)
				return true;

			Status = "Polling for devices...";
			m_getLog().Info("SLI-F1: searching for device");

			// TODO multiple device support, I guess.
			m_device = HidDevices.Enumerate(Constants.vendorId, Constants.productId).FirstOrDefault();

			if (m_device == null)
			{
				// Try again on next timer.
				m_getLog().Warn("SLI-F1: no device");
				Status = "Polling for devices: not found";
				return false;
			}

			m_getLog().InfoFormat("SLI-F1: {0} found at {1}", m_device.Description, m_device.DevicePath);

			// Format up a nice device info string.
			byte[] data;
			String manufacturer = m_device.ReadManufacturer(out data) ? Encoding.Unicode.GetString(data).TrimEnd('\0') : "";
			String product = m_device.ReadProduct(out data) ? Encoding.Unicode.GetString(data).TrimEnd('\0') : "";
			String serial = m_device.ReadSerialNumber(out data) ? Encoding.Unicode.GetString(data).TrimEnd('\0') : "";
			m_deviceInfo = String.Format("{0}{1}{2}{3}{4}{5}{6}",
				manufacturer, (manufacturer.Length > 0) ? " " : "",
				product, (product.Length > 0) ? " " : "",
				(serial.Length > 0) ? "(" : "",
				serial,
				(serial.Length > 0) ? ")" : "").Trim();
			if (m_deviceInfo.Length == 0)
				m_deviceInfo = m_device.Description;

			// Marshal events back to the initial (ui) thread.
			var syncContext = SynchronizationContext.Current;
			m_device.Inserted += () => syncContext.Post((_) => OnInserted(), null);
			m_device.Removed += () => syncContext.Post((_) => OnRemoved(), null);
			m_device.MonitorDeviceEvents = true;
			m_device.OpenDevice();

			return true;
		}

		private void OnInserted()
		{
			if (m_isAvailable)
			{
				m_getLog().Warn("SLI-F1: ignoring inserted event when already available");
				return;
			}

			m_getLog().Info("SLI-F1: inserted event");

			// Initialize both tx buffers for report type 1.
			ResetLedHidReport(m_ledHidReport);
			ResetLedHidReport(m_prevLedHidReport);

			m_isAvailable = true;
			Status = String.Format("{0}: available", m_deviceInfo);

			// Explicit brightness set in config, so use that (and ignore any rotary).
			if (m_settings.BrightnessRotarySwitchIndex == RotaryDetector.unknownIndex)
				SendBrightness(m_settings.BrightnessLevel);

			// Start reading reports from the board.
			ReadHidReport();
		}

		private void OnRemoved()
		{
			m_getLog().InfoFormat("SLI-F1: removed event thread {0}", Thread.CurrentThread.ManagedThreadId);

			if (!m_isAvailable)
			{
				m_getLog().Warn("SLI-F1: removed event when not available");
				return;
			}

			// NB no need to CloseDevice() here; we can just wait for an OnInserted() event rather than poll.

			m_txTask = null;
			m_isAvailable = false;
			Status = String.Format("{0}: removed", m_deviceInfo);
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

					//m_getLog().InfoFormat("SLI-F1: read task completed in thread {0}", Thread.CurrentThread.ManagedThreadId);

					// Ignore cancellation of read on a close.
					if (!m_isAvailable)
					{
						m_getLog().Warn("SLI-F1: ignoring HidReport with device not available");
						return;
					}

					if ((task.Status == TaskStatus.RanToCompletion) && (hidReport.ReadStatus == HidDeviceData.ReadStatus.Success))
					{
						if (m_rotaryDetector == null)
						{
							for (int rotarySwitchIndex = 0; rotarySwitchIndex < m_rotarySwitchPositions.Length; ++rotarySwitchIndex)
							{
								ProcessRotarySwitch(rotarySwitchIndex, hidReport.Data);
							}
						}
						else
						{
							m_rotaryDetector.ProcessHidReport(hidReport.Data);
						}
					}
				}
				catch (Exception e)
				{
					m_getLog().WarnFormat("SLI-F1: ReadReportAsync() threw {0} in thread {1}", e,
						Thread.CurrentThread.ManagedThreadId);
				}
			}
		}

		private void ProcessRotarySwitch(int rotarySwitchIndex, byte[] rxBuffer)
		{
			// TODO should probably read ushorts...
			int bufferOffset = (int)InputReport.rotarySwitchesOffset + (rotarySwitchIndex * 2);

			if ((bufferOffset < 0) || (bufferOffset >= rxBuffer.Length))
				return;

			int newPosition = (int)rxBuffer[bufferOffset];
			ref int previousPosition = ref m_rotarySwitchPositions[(int)rotarySwitchIndex];

			if (newPosition != previousPosition)
			{
				// Swap first in case callback throws.
				(newPosition, previousPosition) = (previousPosition, newPosition);
				m_rotarySwitchChangeCallback(rotarySwitchIndex, newPosition, previousPosition);
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
} // namespace SimElation.SliF1
