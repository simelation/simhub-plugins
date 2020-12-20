/*
 * An SLI device instance.
 */

using System;
using System.ComponentModel;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using HidLibrary;

// ---------------------------------------------------------------------------------------------------------------------------------

namespace SimElation.SliDevices
{
	/// <summary>Interface to an SLI device (SLI-Pro or SLI-F1).</summary>
	/// <remarks>
	/// Provides control over all LEDs and segment displays, and detection of rotary switches
	/// for changing brightness and the left/right segment displays.
	/// </remarks>
	public class Device : IDisposable, INotifyPropertyChanged
	{
		/// <summary>The segment display positions.</summary>
		public enum SegmentDisplayPosition
		{
			/// <summary>The left segment display.</summary>
			left,

			/// <summary>The right segment display.</summary>
			right
		}

		/// <summary>Settings for the SLI device.</summary>
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

			/// <summary>Brightness level.</summary>
			public byte BrightnessLevel
			{
				get => m_brightnessLevel;

				set
				{
					// Note that sometimes, BrightnessLevel is set to the same value to reset the device's brightness,
					// so there's no != value check here.
					m_brightnessLevel = value;
					OnPropertyChanged();
				}
			}

			/// <summary>Calculate brightness level from rotary switch position.</summary>
			/// <param name="position"></param>
			/// <param name="numberOfRotaryPositions"></param>
			/// <returns>Brightness level, or a default if rotary is in an unknown position.</returns>
			public static byte GetBrightnessLevelFromRotarySwitchPosition(int position, int numberOfRotaryPositions)
			{
				if (position < 0)
					return 100;

				--numberOfRotaryPositions;
				position = Math.Min(position, numberOfRotaryPositions);

				// NB SLI-Pro C interface docs say max of 254 for some reason, even though 255 also works...
				return (byte)((254.0 / numberOfRotaryPositions) * position);
			}

			/// <inheritdoc/>
			public event PropertyChangedEventHandler PropertyChanged;

			/// <inheritdoc/>
			protected void OnPropertyChanged([CallerMemberName] string name = null)
			{
				PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
			}

			private const int defaultNumberOfBrightnessRotaryPositions = 12;

			private int m_brightnessRotarySwitchIndex = RotarySwitchDetector.unknownIndex;
			private int m_numberOfBrightnessRotaryPositions = defaultNumberOfBrightnessRotaryPositions;
			private byte m_brightnessLevel =
				GetBrightnessLevelFromRotarySwitchPosition(-1, defaultNumberOfBrightnessRotaryPositions);
		}

		/// <summary>Descriptor for the device's class (report formats, constants, etc.).</summary>
		public IDeviceDescriptor DeviceDescriptor { get; }

		/// <summary>Delegate to access a <see cref="log4net.ILog"/> object for logging purposes.</summary>
		/// <returns><see cref="log4net.ILog"/> object.</returns>
		public delegate log4net.ILog GetLog();

		/// <summary>Delegate to call when a rotary switch changes position.</summary>
		/// <param name="rotarySwitch">The rotary which has changed.</param>
		/// <param name="previousPosition">Its previous position. May be -1 on startup.</param>
		/// <param name="newPosition">Its new position, indexed from 0.</param>
		public delegate void RotarySwitchChangeCallback(int rotarySwitch, int previousPosition, int newPosition);

		/// <summary>Is the SLI device available?</summary>
		/// <remarks>
		/// This is just an accessor to HidDevice.IsConnected. Inserted/Removed events will manually trigger
		/// an OnPropertyChanged() for this property.
		/// </remarks>
		public bool IsAvailable { get => m_hidDevice.IsConnected; }

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

		/// <inheritdoc/>
		public event PropertyChangedEventHandler PropertyChanged;

		/// <inheritdoc/>
		protected void OnPropertyChanged([CallerMemberName] string name = null)
		{
			PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
		}

		private bool m_isDisposed = false;

		private readonly HidDevice m_hidDevice;
		private readonly Settings m_settings;
		private readonly GetLog m_getLog;
		private readonly RotarySwitchChangeCallback m_rotarySwitchChangeCallback;

		private readonly SynchronizationContext m_syncContext = SynchronizationContext.Current;
		private Task<bool> m_txTask = null;

		private HidReport m_ledHidReport;
		private HidReport m_prevLedHidReport;
		private readonly HidReport m_brightnessHidReport;

		private readonly int[] m_rotarySwitchPositions;
		private RotarySwitchDetector m_rotarySwitchDetector = null;

		private const String initializingStatus = "Initializing";
		private const String unpluggedStatus = "Unplugged";
		private const String availableStatus = "Available";

		private String m_status = initializingStatus;

		/// <summary>Constructor.</summary>
		/// <param name="deviceDescriptor">Descriptor for the device's class (report formats, constants, etc.).</param>
		/// <param name="deviceInfo">Info about this device instance (serial number, etc.).</param>
		/// <param name="settings">Settings for the SLI device.</param>
		/// <param name="getLog">Function to use for logging.</param>
		/// <param name="rotarySwitchChangeCallback">Callback for when a rotary switch changes position.</param>
		public Device(IDeviceDescriptor deviceDescriptor, DeviceInfo deviceInfo, Settings settings, GetLog getLog,
			RotarySwitchChangeCallback rotarySwitchChangeCallback)
		{
			getLog().InfoFormat("{0}: constructing in thread {1}", Assembly.GetExecutingAssembly().GetName().Name,
				Thread.CurrentThread.ManagedThreadId);

			DeviceDescriptor = deviceDescriptor;
			m_hidDevice = deviceInfo.HidDevice;
			m_settings = settings;
			m_getLog = getLog;
			m_rotarySwitchChangeCallback = rotarySwitchChangeCallback;

			m_ledHidReport = new HidReport((int)DeviceDescriptor.LedStateReport.Length + 1);
			m_prevLedHidReport = new HidReport((int)DeviceDescriptor.LedStateReport.Length + 1);
			m_brightnessHidReport = new HidReport((int)DeviceDescriptor.BrightnessReport.Length + 1);
			m_rotarySwitchPositions = new int[(int)DeviceDescriptor.Constants.MaxNumberOfRotarySwitches];

			// Can't initialize these above as MaxNumberOfRotarySwitches isn't constant.
			// TODO C# 8.0 fix probably; static interface members.
			for (int i = 0; i < m_rotarySwitchPositions.Length; ++i)
			{
				m_rotarySwitchPositions[i] = -1;
			}

			m_settings.PropertyChanged +=
				(object sender, PropertyChangedEventArgs e) =>
				{
					switch (e.PropertyName)
					{
						case nameof(m_settings.BrightnessRotarySwitchIndex):
							MaybeSetBrightnessFromRotarySwitchPosition();
							break;

						case nameof(m_settings.BrightnessLevel):
							if (IsAvailable)
								SendBrightness(m_settings.BrightnessLevel);
							break;

						default:
							break;
					}
				};

			// Marshal HID events back to the initial (ui) thread.
			// NB adding an inserted handler seems to implicitly invoke OpenDevice() if not open and we'll get the Inserted event.
			// However, if we're already open, we won't get the inserted event. Lovely.
			// Also, if we've explictly CloseDevice()d, then re-opening (either with OpenDevice() or by adding an Inserted handler)
			// doesn't fire the event!
			// Hance we don't explicitly CloseDevice() when disposing this object as then we can't determine whether
			// to wait for the inserted event or manually do initialization.
			bool wasOpen = m_hidDevice.IsOpen;
			m_hidDevice.Inserted += InsertedHandler;
			m_hidDevice.Removed += RemovedHandler;
			m_hidDevice.MonitorDeviceEvents = true;

			if (m_hidDevice.IsConnected)
			{
				if (wasOpen)
					InsertedHandler();
			}
			else
			{
				Status = unpluggedStatus;
			}
		}

		/// <summary>Dispose.</summary>
		/// <remarks>
		/// If open, clears all the LEDs and closes the device.
		/// </remarks>
		public void Dispose()
		{
			Dispose(true);
		}

		/// <summary>Reset the internal LED state.</summary>
		/// <remarks>
		/// No write to the device is performed; <see cref="SendLedState"/> would need to be called.
		/// Additionally, brightness is not altered.
		/// </remarks>
		public void ResetLedState()
		{
			ResetLedHidReport(m_ledHidReport);
		}

		/// <summary>Possibly set brightness level if configured for rotary switch control.</summary>
		/// <returns>true if <see cref="Settings.BrightnessLevel"/> was set (due to rotary switch being configured).</returns>
		private bool MaybeSetBrightnessFromRotarySwitchPosition()
		{
			if (m_settings.BrightnessRotarySwitchIndex == RotarySwitchDetector.unknownIndex)
				return false;

			// Changing to rotary switch control, so dig out position of the rotary switch and set the level
			// (BrightnessLevel changed will fire later to actually send the level to the device).
			m_settings.BrightnessLevel = Settings.GetBrightnessLevelFromRotarySwitchPosition(
				GetRotarySwitchPosition(m_settings.BrightnessRotarySwitchIndex), m_settings.NumberOfBrightnessRotaryPositions);

			return true;
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
			txBuffer[DeviceDescriptor.BrightnessReport.ReportTypeOffset] = DeviceDescriptor.BrightnessReport.ReportType;
			txBuffer[DeviceDescriptor.BrightnessReport.BrightnessOffset] = level;

			// Not going to worry about async for brightness control!
			if (!m_hidDevice.WriteReport(m_brightnessHidReport))
				m_getLog().WarnFormat("{0}: WriteReport() failed", Assembly.GetExecutingAssembly().GetName().Name);
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
			int substringLength = Math.Min(str.Length / 2, (int)DeviceDescriptor.Constants.SegmentDisplayWidth);
			String left = str.Substring(0, substringLength);
			String right = str.Substring(str.Length - substringLength);

			m_ledHidReport.Data[DeviceDescriptor.LedStateReport.GearOffset] =
				(byte)(((left.Length + right.Length) != str.Length) ? str[substringLength] : ' ');

			SetSegment(SegmentDisplayPosition.left, left.PadLeft((int)DeviceDescriptor.Constants.SegmentDisplayWidth));
			SetSegment(SegmentDisplayPosition.right, right.PadRight((int)DeviceDescriptor.Constants.SegmentDisplayWidth));
		}

		/// <summary>Set the contents of a segment display.</summary>
		/// <param name="segmentDisplayPosition">The left or right segment display.</param>
		/// <param name="str">The string to display. If shorter than the display size, it is (right) space padded.</param>
		/// <param name="decimalOrPrimeIndexList">Optional indexes for decimal/prime indicators to set.</param>
		public void SetSegment(SegmentDisplayPosition segmentDisplayPosition, String str, params uint[] decimalOrPrimeIndexList)
		{
			SetSegment(segmentDisplayPosition, 0, (uint)str.Length, str, decimalOrPrimeIndexList);
		}

		/// <summary>Set the contents of a segment display.</summary>
		/// <param name="segmentDisplayPosition">The left or right segment display.</param>
		/// <param name="firstSegmentIndex">0-index of the first character in the display (NOT in <paramref name="str"/> to set.
		/// </param>
		/// <param name="numberOfCharacters">The number of characters to set.</param>
		/// <param name="str">The string to display. If shorter than <paramref name="numberOfCharacters"/>, it is padded.</param>
		/// <param name="decimalOrPrimeIndexList">
		/// Optional indexes for decimal/prime indicators to set. Index 0 is the first character in
		/// the segment display, not the <paramref name="firstSegmentIndex">th</paramref> character.
		/// </param>
		public void SetSegment(SegmentDisplayPosition segmentDisplayPosition, uint firstSegmentIndex, uint numberOfCharacters,
			String str, params uint[] decimalOrPrimeIndexList)
		{
			switch (segmentDisplayPosition)
			{
				case SegmentDisplayPosition.left:
					InternalSetSegment(DeviceDescriptor.LedStateReport.LeftSegmentDisplayOffset,
						DeviceDescriptor.Constants.SegmentDisplayWidth, firstSegmentIndex, numberOfCharacters, str,
						decimalOrPrimeIndexList);
					break;

				case SegmentDisplayPosition.right:
					InternalSetSegment(DeviceDescriptor.LedStateReport.RightSegmentDisplayOffset,
						DeviceDescriptor.Constants.SegmentDisplayWidth, firstSegmentIndex, numberOfCharacters, str,
						decimalOrPrimeIndexList);
					break;
			}
		}

		/// <summary>Set the gear indicator.</summary>
		/// <param name="value"></param>
		public void SetGear(String value)
		{
			m_ledHidReport.Data[DeviceDescriptor.LedStateReport.GearOffset] =
				(byte)((value.Length == 1) ? Char.ToLower(value[0]) : '-');
		}

		/// <summary>Set some rev LEDs based on an rpm %.</summary>
		/// <remarks>
		/// The state for any rev LEDs outside of the range specified by <paramref name="firstLedIndex"/> and
		/// <paramref name="maxNumberOfLeds"/> is not altered.
		/// </remarks>
		/// <param name="minRpmPercent">The rpm % at which the first rev LED should light.</param>
		/// <param name="rpmPercent">The rpm %.</param>
		/// <param name="firstLedIndex">The 0-based index of the first rev LED to set.</param>
		/// <param name="maxNumberOfLeds">The number of rev LEDs to light when at 100% <paramref name="rpmPercent"/>,
		/// or null for all.</param>
		/// <returns>
		/// false if <paramref name="firstLedIndex"/> + <paramref name="maxNumberOfLeds"/> is out of range,
		/// <paramref name="minRpmPercent"/> is >= 100, <paramref name="rpmPercent"/> is > 100.</returns>
		public bool SetRevLeds(double minRpmPercent, double rpmPercent, uint firstLedIndex = 0,
			uint? maxNumberOfLeds = null)
		{
			uint numberOfLeds = (uint)DeviceDescriptor.Constants.RevLedColors.Length;

			if (firstLedIndex >= numberOfLeds)
				return false;

			uint maxNumberOfLeds2;

			if (maxNumberOfLeds != null)
			{
				if ((firstLedIndex + maxNumberOfLeds) > numberOfLeds)
					return false;

				maxNumberOfLeds2 = (uint)maxNumberOfLeds;
			}
			else
			{
				maxNumberOfLeds2 = numberOfLeds - firstLedIndex;
			}

			if ((minRpmPercent >= 100) || (rpmPercent > 100))
				return false;

			uint numberOfSetLeds = 0;

			if (rpmPercent >= minRpmPercent)
			{
				double val = ((rpmPercent - minRpmPercent) * maxNumberOfLeds2) / (100 - minRpmPercent);
				numberOfSetLeds = (uint)Math.Round(val, MidpointRounding.AwayFromZero);
			}

			for (uint i = firstLedIndex; i < (firstLedIndex + maxNumberOfLeds2); ++i)
			{
				m_ledHidReport.Data[DeviceDescriptor.LedStateReport.RevLed1Offset + i] =
					(byte)(((i - firstLedIndex) < numberOfSetLeds) ? 1 : 0);
			}

			return true;
		}

		/// <summary>Set ALL rev LEDs based on an array of bools.</summary>
		/// <remarks>
		/// The <paramref name="ledStates"/> array can contain fewer elements than the number of rev LEDs;
		/// the remaining LEDs will be cleared.
		/// </remarks>
		/// <param name="ledStates"></param>
		public void SetRevLeds(bool[] ledStates)
		{
			for (int i = 0; i < DeviceDescriptor.Constants.RevLedColors.Length; ++i)
			{
				m_ledHidReport.Data[DeviceDescriptor.LedStateReport.RevLed1Offset + i] =
					(byte)(((i < ledStates.Length) && ledStates[i]) ? 1 : 0);
			}
		}

		/// <summary>Set an individual rev LED.</summary>
		/// <param name="ledIndex">The 0-based index of the rev LED's state to set.</param>
		/// <param name="isSet"></param>
		/// <returns>false if <paramref name="ledIndex"/> is out of range, otherwise true.</returns>
		public bool SetRevLed(uint ledIndex, bool isSet)
		{
			if (ledIndex >= DeviceDescriptor.Constants.RevLedColors.Length)
				return false;

			m_ledHidReport.Data[DeviceDescriptor.LedStateReport.RevLed1Offset + ledIndex] = (byte)(isSet ? 1 : 0);

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
			if (ledIndex >= DeviceDescriptor.Constants.NumberOfStatusLeds)
				return false;

			m_ledHidReport.Data[DeviceDescriptor.LedStateReport.StatusLed1Offset + ledIndex] = (byte)(isSet ? 1 : 0);

			return true;
		}

		/// <summary>Set an external LED.</summary>
		/// <param name="ledIndex">The 0-based index of the external LED's state to set.</param>
		/// <param name="isSet"></param>
		/// <returns>false if <paramref name="ledIndex"/> is out of range, otherwise true.</returns>
		public bool SetExternalLed(uint ledIndex, bool isSet)
		{
			if (ledIndex >= DeviceDescriptor.Constants.NumberOfExternalLeds)
				return false;

			m_ledHidReport.Data[DeviceDescriptor.LedStateReport.ExternalLed1Offset + ledIndex] = (byte)(isSet ? 1 : 0);

			return true;
		}

		/// <summary>Send current LED state to the device.</summary>
		/// <remarks>
		/// This call assumes <see cref="IsAvailable"/> is true.
		/// </remarks>
		public async void SendLedState()
		{
			if (m_txTask != null)
			{
				if (!m_txTask.IsCompleted)
				{
					m_getLog().DebugFormat("{0}: skipping write; previous hasn't completed",
						Assembly.GetExecutingAssembly().GetName().Name);
					return;
				}

				try
				{
					await m_txTask;
				}
				catch (Exception e)
				{
					m_getLog().WarnFormat("{0}: WriteAsync() failed {1}", Assembly.GetExecutingAssembly().GetName().Name, e);
				}
			}

			if (Buffer.Equals(m_ledHidReport.Data, m_prevLedHidReport.Data))
				return;

			m_txTask = m_hidDevice.WriteReportAsync(m_ledHidReport);

			// Swap over buffers. Don't clear the new buffer in case it we end up not needing to send it if is
			// the same as the previous (pretty unlikely in game though).
			(m_prevLedHidReport, m_ledHidReport) = (m_ledHidReport, m_prevLedHidReport);
		}

		/// <summary>Initiate a detection for a rotary switch.</summary>
		/// <remarks>
		/// The object will monitor received reports from the device for changes.
		/// </remarks>
		/// <param name="timeoutMs">How long to attempt to detect the rotary for, in milliseconds.</param>
		/// <returns>
		/// A waitable task that resolves to the index of a detected rotary or
		/// <see cref="RotarySwitchDetector.unknownIndex"/> if none is detected.
		/// </returns>
		public Task<int> DetectRotary(int timeoutMs = 5000)
		{
			var task = new TaskCompletionSource<int>();

			m_getLog().InfoFormat("{0}: detecting rotary in thread {1}...", Assembly.GetExecutingAssembly().GetName().Name,
				Thread.CurrentThread.ManagedThreadId);

			if (m_rotarySwitchDetector != null)
				m_rotarySwitchDetector.Dispose();

			m_rotarySwitchDetector = new RotarySwitchDetector(timeoutMs,
				(int rotarySwitchIndex) =>
				{
					if (rotarySwitchIndex == RotarySwitchDetector.unknownIndex)
					{
						m_getLog().InfoFormat("{0}: no rotary detected in thread {1}",
							Assembly.GetExecutingAssembly().GetName().Name, Thread.CurrentThread.ManagedThreadId);
					}
					else
					{
						m_getLog().InfoFormat("{0}: detected rotary {1} in thread {2}",
							Assembly.GetExecutingAssembly().GetName().Name, rotarySwitchIndex,
							Thread.CurrentThread.ManagedThreadId);
					}

					m_rotarySwitchDetector.Dispose();
					m_rotarySwitchDetector = null;

					task.SetResult(rotarySwitchIndex);
				},
				DeviceDescriptor);

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
				m_getLog().InfoFormat("{0}: Dispose() from thread {1}", Assembly.GetExecutingAssembly().GetName().Name,
					Thread.CurrentThread.ManagedThreadId);

				if (IsAvailable)
				{
					ResetLedState();
					SendLedState();

					// NB see note in constructor about why we don't m_hidDevice.CloseDevice() here!
					m_hidDevice.MonitorDeviceEvents = false;
					m_hidDevice.Inserted -= InsertedHandler;
					m_hidDevice.Removed -= RemovedHandler;

					Status = "Disposed";
				}

				if (m_rotarySwitchDetector != null)
				{
					m_rotarySwitchDetector.Dispose();
					m_rotarySwitchDetector = null;
				}
			}

			m_isDisposed = true;
		}

		private void InternalSetSegment(uint reportOffset, uint segmentLength, uint firstSegmentIndex, uint numberOfCharacters,
			String str, params uint[] decimalOrPrimeIndexList)
		{
			for (uint i = firstSegmentIndex; i < segmentLength; ++i)
			{
				int stringIndex = (int)(i - firstSegmentIndex);
				m_ledHidReport.Data[reportOffset + i] = (byte)((stringIndex < str.Length) ? str[stringIndex] : ' ');
			}

			foreach (uint i in decimalOrPrimeIndexList)
			{
				if (i < segmentLength)
					m_ledHidReport.Data[reportOffset + i] |= DeviceDescriptor.LedStateReport.SegmentDisplayDecimalOrPrimeBit;
			}
		}

		private void InsertedHandler()
		{
			void OnInserted()
			{
				m_getLog().InfoFormat("{0}: inserted event", Assembly.GetExecutingAssembly().GetName().Name);

				// Initialize both tx buffers for report type 1.
				ResetLedHidReport(m_ledHidReport);
				ResetLedHidReport(m_prevLedHidReport);

				Status = availableStatus;
				OnPropertyChanged(nameof(IsAvailable));

				// Set initial brightness.
				if (!MaybeSetBrightnessFromRotarySwitchPosition())
				{
					// Explicit brightness set in config, so use that.
					SendBrightness(m_settings.BrightnessLevel);
				}

				// Start reading reports from the board.
				ReadHidReport();
			}

			m_syncContext.Post((_) => OnInserted(), null);
		}

		private void RemovedHandler()
		{
			void OnRemoved()
			{
				m_getLog().InfoFormat("{0}: removed event thread {1}", Assembly.GetExecutingAssembly().GetName().Name,
					Thread.CurrentThread.ManagedThreadId);

				// NB no need to CloseDevice() here; we can just wait for an OnInserted() event rather than poll.

				m_txTask = null;
				Status = unpluggedStatus;
				OnPropertyChanged(nameof(IsAvailable));
			}

			m_syncContext.Post((_) => OnRemoved(), null);
		}

		private async void ReadHidReport()
		{
			var startId = Thread.CurrentThread.ManagedThreadId;

			while (IsAvailable)
			{
				try
				{
					var task = m_hidDevice.ReadReportAsync();
					var hidReport = await task;

					// Ignore cancellation of read on a close.
					if (!IsAvailable)
					{
						m_getLog().WarnFormat("{0}: ignoring HidReport with device not available",
							Assembly.GetExecutingAssembly().GetName().Name);
						return;
					}

					if ((task.Status == TaskStatus.RanToCompletion) && (hidReport.ReadStatus == HidDeviceData.ReadStatus.Success))
					{
						if (m_rotarySwitchDetector == null)
						{
							for (int rotarySwitchIndex = 0; rotarySwitchIndex < m_rotarySwitchPositions.Length; ++rotarySwitchIndex)
							{
								ProcessRotarySwitch(rotarySwitchIndex, hidReport.Data);
							}
						}
						else
						{
							m_rotarySwitchDetector.ProcessHidReport(hidReport.Data);
						}
					}
				}
				catch (Exception e)
				{
					m_getLog().WarnFormat("{0}: ReadReportAsync() threw {1} in thread {2}",
						Assembly.GetExecutingAssembly().GetName().Name, e, Thread.CurrentThread.ManagedThreadId);
				}
			}
		}

		private void ProcessRotarySwitch(int rotarySwitchIndex, byte[] rxBuffer)
		{
			// Should probably read ushorts but only 12 position rotary switches are supported (I think?).
			int bufferOffset = (int)DeviceDescriptor.InputReport.RotarySwitchesOffset + (rotarySwitchIndex * 2);

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

		private void ResetLedHidReport(HidReport hidReport)
		{
			var txBuffer = hidReport.Data;

			// Zero out the buffer.
			Array.Clear(txBuffer, 0, txBuffer.Length);
			hidReport.ReportId = 0;

			txBuffer[DeviceDescriptor.LedStateReport.ReportTypeOffset] = DeviceDescriptor.LedStateReport.ReportType;

			// Spaces for the segment displays.
			txBuffer[DeviceDescriptor.LedStateReport.GearOffset] = (byte)' ';

			for (uint i = 0; i < DeviceDescriptor.Constants.SegmentDisplayWidth; ++i)
			{
				txBuffer[DeviceDescriptor.LedStateReport.LeftSegmentDisplayOffset + i] = (byte)' ';
			}

			for (uint i = 0; i < DeviceDescriptor.Constants.SegmentDisplayWidth; ++i)
			{
				txBuffer[DeviceDescriptor.LedStateReport.RightSegmentDisplayOffset + i] = (byte)' ';
			}
		}
	}
}
