/*
 * SimHub SLI plugin.
 */

using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Media;
using SimElation.SliDevices;

// ---------------------------------------------------------------------------------------------------------------------------------

// Ambiguous reference in cref attribute.
#pragma warning disable CS0419

namespace SimElation.Simhub.SliPlugin
{
	/// <summary>An instance of a device to display in the UI.</summary>
	/// <remarks>
	/// May or may not be managed by the plugin. If it is managed by the plugin, it may not be plugged in (device info
	/// came from saved settings).
	/// </remarks>
	public sealed class DeviceInstance : INotifyPropertyChanged
	{
		/// <summary>Settings for the SLI plugin.</summary>
		public sealed class Settings : Device.Settings
		{
			/// <summary>An RPM LED.</summary>
			public sealed class RpmLed
			{
				/// <summary>Constructor.</summary>
				/// <param name="setColor">Color for the set LED.</param>
				/// <param name="isSet">Is the LED lit?</param>
				public RpmLed(Color setColor, bool isSet)
				{
					SetBrush = new SolidColorBrush(setColor);
					IsSet = isSet;
				}

				/// <summary>Brush to paint the LED when lit?</summary>
				/// <remarks>No JsonIgnoreAttribute as saving the color out to config file simplifies reload.</remarks>
				public Brush SetBrush { get; set; } = Brushes.Red;

				/// <summary>Get or set if the LED is lit.</summary>
				public bool IsSet { get; set; } = false;
			}

			/// <summary>The index of the rotary switch used to control the left segment display.</summary>
			public int LeftSegmentDisplayRotarySwitchIndex
			{
				get => m_leftSegmentDisplayRotarySwitchIndex;

				set
				{
					m_leftSegmentDisplayRotarySwitchIndex = value;
					OnPropertyChanged();
				}
			}

			/// <summary>Current left segment display mode.</summary>
			public int LeftSegmentDisplayIndex
			{
				get => m_leftSegmentDisplayIndex;

				set
				{
					m_leftSegmentDisplayIndex = value;
					OnPropertyChanged();
				}
			}

			/// <summary>The index of the rotary switch used to control the right segment display.</summary>
			public int RightSegmentDisplayRotarySwitchIndex
			{
				get => m_rightSegmentDisplayRotarySwitchIndex;

				set
				{
					m_rightSegmentDisplayRotarySwitchIndex = value;
					OnPropertyChanged();
				}
			}

			/// <summary>Current right segment display mode.</summary>
			public int RightSegmentDisplayIndex
			{
				get => m_rightSegmentDisplayIndex;

				set
				{
					m_rightSegmentDisplayIndex = value;
					OnPropertyChanged();
				}
			}

			/// <summary>SimHub properties assigned to external LEDs.</summary>
			public Led[] ExternalLeds { get; set; }

			/// <summary>Text to display using the segment displays when SimHub is running but no game is.</summary>
			public String WelcomeMessage
			{
				get => m_welcomeMessage;

				set
				{
					m_welcomeMessage = value;
					OnPropertyChanged();
				}
			}

			/// <summary>The rate in milliseconds at which to animate the RPM LEDs when in the pitlane. 0 to disable.</summary>
			public long PitLaneAnimationSpeedMs
			{
				get => m_pitLaneAnimationSpeedMs;

				set
				{
					m_pitLaneAnimationSpeedMs = value;
					OnPropertyChanged();
				}
			}

			/// <summary>How long in milliseconds to light the RPM LEDs when at or above the shift threshold.</summary>
			public long ShiftPointBlinkOnSpeedMs
			{
				get => m_shiftPointBlinkOnSpeedMs;

				set
				{
					m_shiftPointBlinkOnSpeedMs = value;
					OnPropertyChanged();
				}
			}

			/// <summary>How long in milliseconds to blank the RPM LEDs when at or above the shift threshold.</summary>
			public long ShiftPointBlinkOffSpeedMs
			{
				get => m_shiftPointBlinkOffSpeedMs;

				set
				{
					m_shiftPointBlinkOffSpeedMs = value;
					OnPropertyChanged();
				}
			}

			/// <summary>How long in milliseconds to light/blank a status LED when appropriate.</summary>
			public long StatusLedBlinkIntervalMs
			{
				get => m_statusLedBlinkIntervalMs;

				set
				{
					m_statusLedBlinkIntervalMs = value;
					OnPropertyChanged();
				}
			}

			/// <summary>How long to display what information a segment display will show after changing mode.</summary>
			public long SegmentNameTimeoutMs
			{
				get => m_segmentNameTimeoutMs;

				set
				{
					m_segmentNameTimeoutMs = value;
					OnPropertyChanged();
				}
			}

			/// <summary>In pit-lane animation LEDs, pattern 1.</summary>
			public RpmLed[] PitLaneLeds1 { get; set; }

			/// <summary>In pit-lane animation LEDs, pattern 2.</summary>
			public RpmLed[] PitLaneLeds2 { get; set; }

			/// <summary>Left status LEDs.</summary>
			public Led[] LeftStatusLeds { get; set; }

			/// <summary>Right status LEDs.</summary>
			public Led[] RightStatusLeds { get; set; }

			/// <summary>Default constructor.</summary>
			public Settings()
			{
				// Needed for settings reading (when no device descriptor is available).
			}

			/// <summary>Constructor.</summary>
			/// <param name="descriptor">Descriptor describing the device.</param>
			public Settings(IDescriptor descriptor) : this()
			{
				// In pit-lane LED animation. Default pattern is to alternate based on the colors changing.
				PitLaneLeds1 = new RpmLed[descriptor.Constants.RevLedColors.Length];
				PitLaneLeds2 = new RpmLed[descriptor.Constants.RevLedColors.Length];

				Color? previousColor = null;
				bool isLed1Set = false;

				for (uint i = 0; i < PitLaneLeds1.Length; ++i)
				{
					var currentColor = descriptor.Constants.RevLedColors[i];
					if (previousColor != currentColor)
					{
						previousColor = currentColor;
						isLed1Set ^= true;
					}

					PitLaneLeds1[i] = new RpmLed(currentColor, isLed1Set);
					PitLaneLeds2[i] = new RpmLed(currentColor, !isLed1Set);
				}

				// Status LEDs.
				Led MakeStatusLed(Color color, String title, String formula)
				{
					var statusLed = new Led(color);
					statusLed.BindingData.Mode = SimHub.Plugins.OutputPlugins.GraphicalDash.Models.BindingMode.Formula;
					statusLed.BindingData.Formula = new SimHub.Plugins.OutputPlugins.Dash.GLCDTemplating.ExpressionValue(
						formula, SimHub.Plugins.OutputPlugins.Dash.GLCDTemplating.Interpreter.NCalc);
					statusLed.BindingData.TargetPropertyName = title;

					return statusLed;
				}

				LeftStatusLeds = new Led[descriptor.Constants.NumberOfStatusLeds / 2];

				LeftStatusLeds[0] = MakeStatusLed(Colors.Yellow, "Left status LED 1",
					String.Format("if ([Flag_Yellow], {0}, {1})", (int)Led.State.on, (int)Led.State.off));
				LeftStatusLeds[1] = MakeStatusLed(Colors.Blue, "Left status LED 2",
					String.Format("if ([Flag_Blue], {0}, {1})", (int)Led.State.on, (int)Led.State.off));
				LeftStatusLeds[2] = MakeStatusLed(Colors.Red, "Left status LED 3",
					String.Format("if ([CarSettings_FuelAlertActive], {0}, {1})", (int)Led.State.blink, (int)Led.State.off));

				RightStatusLeds = new Led[descriptor.Constants.NumberOfStatusLeds / 2];

				RightStatusLeds[0] = MakeStatusLed(Colors.Yellow, "Right status LED 1",
					String.Format("if ([ABSActive], {0}, {1})", (int)Led.State.on, (int)Led.State.off));
				RightStatusLeds[1] = MakeStatusLed(Colors.Blue, "Right status LED 2",
					String.Format("if ([TCActive], {0}, {1})", (int)Led.State.on, (int)Led.State.off));
				RightStatusLeds[2] = MakeStatusLed(Colors.Red, "Right status LED 3",
					// Flash for DRS available (to get attention!), solid for DRS on.
					// NB rf2 (at least?) reports DRS available in the pits (in a practise session), so ignore DRS state if in pit.
					String.Format("if (([IsInPitLane] || [IsInPit]), {2}, if ([DRSEnabled], {0}, if ([DRSAvailable], {1}, {2})))",
						(int)Led.State.on, (int)Led.State.blink, (int)Led.State.off));

				// External LEDs.
				ExternalLeds = new Led[descriptor.Constants.NumberOfExternalLeds];
				for (uint i = 0; i < ExternalLeds.Length; ++i)
				{
					ExternalLeds[i] = new Led();
					ExternalLeds[i].BindingData.TargetPropertyName = String.Format("External LED {0}", (i + 1));
				}
			}

			private int m_leftSegmentDisplayRotarySwitchIndex = RotarySwitchDetector.unknownIndex;
			private int m_leftSegmentDisplayIndex = 0;

			private int m_rightSegmentDisplayRotarySwitchIndex = RotarySwitchDetector.unknownIndex;
			private int m_rightSegmentDisplayIndex = 0;

			private String m_welcomeMessage = "Sim-Hub";
			private long m_pitLaneAnimationSpeedMs = 250;
			private long m_shiftPointBlinkOnSpeedMs = 100;
			private long m_shiftPointBlinkOffSpeedMs = 50;
			private long m_statusLedBlinkIntervalMs = 500;
			private long m_segmentNameTimeoutMs = 1500;
		}

		/// <summary>Info about the device (serial number, pretty string to display in UI, etc.).</summary>
		public DeviceInfo DeviceInfo { get; set; }

		/// <summary>The device's settings.</summary>
		public Settings DeviceSettings { get; set; }

		// TODO this needs to be in some view thing I suppose. It ain't data.
		/// <summary>Whether the device view is expanded in the UI.</summary>
		/// <remarks>
		/// Defaults to collapsed if device isn't managed, and expanded when managed.
		/// </remarks>
		public bool IsExpanded
		{
			get => m_isExpanded;

			set
			{
				if (m_isExpanded != value)
				{
					m_isExpanded = value;
					OnPropertyChanged();
				}
			}
		}

		/// <summary>Is the device managed by the plugin (user has toggled on the "manage" button)?</summary>
		/// <remarks>
		/// Note that IsManaged is separate from ManagedDevice. IsManaged can be true on startup after reading settings,
		/// but the device is unplugged (in which case ManagedDevice won't exist).
		/// </remarks>
		public bool IsManaged
		{
			get => m_isManaged;

			set
			{
				if (m_isManaged != value)
				{
					m_isManaged = value;
					OnPropertyChanged();

					// Set expanded state when changing the "managed" toggle. User can still expand/collapse after that
					// independently.
					IsExpanded = IsManaged;
				}
			}
		}

		/// <summary>The ManagedDevice for when the device is plugged in and managed by the plugin, otherwise null.</summary>
		public ManagedDevice ManagedDevice
		{
			get => m_managedDevice;

			set
			{
				if (m_managedDevice != value)
				{
					m_managedDevice = value;
					OnPropertyChanged();

					// HACK when ManagedDevice changes, combo box list does.
					// TODO move these to Settings??
					OnPropertyChanged(nameof(LeftSegmentDisplayComboBoxContents));
					OnPropertyChanged(nameof(RightSegmentDisplayComboBoxContents));
				}
			}
		}

		/// <summary>List of items for the left segment display combobox.</summary>
		public SegmentDisplayManager.SegmentDisplayMode[] LeftSegmentDisplayComboBoxContents
		{
			get => GetSegmentDisplayNameList(SliDevices.Device.SegmentDisplayPosition.left);
		}

		/// <summary>List of items for the right segment display combobox.</summary>
		public SegmentDisplayManager.SegmentDisplayMode[] RightSegmentDisplayComboBoxContents
		{
			get => GetSegmentDisplayNameList(SliDevices.Device.SegmentDisplayPosition.right);
		}

		/// <summary>Left segment display previous action name.</summary>
		/// <remarks>
		/// Fully qualified version is required for use in xaml / ActionName on a control.
		/// Simpler version can be used by the SimHub API (<see cref="SimHub.Plugins.PluginManager.AddAction"/>).
		/// </remarks>
		public String LeftSegmentDisplayPreviousActionName
		{
			get => MakeActionName("LeftSegmentDisplayPrevious");
		}

		/// <summary>Left segment display previous action name.</summary>
		/// <remarks>
		/// Fully qualified version is required for use in xaml / ActionName on a control.
		/// Simpler version can be used by the SimHub API (<see cref="SimHub.Plugins.PluginManager.AddAction"/>).
		/// </remarks>
		public String LeftSegmentDisplayPreviousActionNameFQ
		{
			get => MakeActionNameFQ(LeftSegmentDisplayPreviousActionName);
		}

		/// <summary>Left segment display next action name.</summary>
		/// <remarks>
		/// Fully qualified version is required for use in xaml / ActionName on a control.
		/// Simpler version can be used by the SimHub API (<see cref="SimHub.Plugins.PluginManager.AddAction"/>).
		/// </remarks>
		public String LeftSegmentDisplayNextActionName
		{
			get => MakeActionName("LeftSegmentDisplayNext");
		}

		/// <summary>Left segment display next action name.</summary>
		/// <remarks>
		/// Fully qualified version is required for use in xaml / ActionName on a control.
		/// Simpler version can be used by the SimHub API (<see cref="SimHub.Plugins.PluginManager.AddAction"/>).
		/// </remarks>
		public String LeftSegmentDisplayNextActionNameFQ
		{
			get => MakeActionNameFQ(LeftSegmentDisplayNextActionName);
		}

		/// <summary>Peek current left segment display action name.</summary>
		/// <remarks>
		/// Fully qualified version is required for use in xaml / ActionName on a control.
		/// Simpler version can be used by the SimHub API (<see cref="SimHub.Plugins.PluginManager.AddAction"/>).
		/// </remarks>
		public String LeftSegmentDisplayPeekCurrentActionName
		{
			get => MakeActionName("LeftSegmentDisplayPeekCurrent");
		}

		/// <summary>Peek current left segment display action name.</summary>
		/// <remarks>
		/// Fully qualified version is required for use in xaml / ActionName on a control.
		/// Simpler version can be used by the SimHub API (<see cref="SimHub.Plugins.PluginManager.AddAction"/>).
		/// </remarks>
		public String LeftSegmentDisplayPeekCurrentActionNameFQ
		{
			get => MakeActionNameFQ(LeftSegmentDisplayPeekCurrentActionName);
		}

		/// <summary>Right segment display next action name.</summary>
		/// <remarks>
		/// Fully qualified version is required for use in xaml / ActionName on a control.
		/// Simpler version can be used by the SimHub API (<see cref="SimHub.Plugins.PluginManager.AddAction"/>).
		/// </remarks>
		public String RightSegmentDisplayNextActionName
		{
			get => MakeActionName("RightSegmentDisplayNext");
		}

		/// <summary>Right segment display next action name.</summary>
		/// <remarks>
		/// Fully qualified version is required for use in xaml / ActionName on a control.
		/// Simpler version can be used by the SimHub API (<see cref="SimHub.Plugins.PluginManager.AddAction"/>).
		/// </remarks>
		public String RightSegmentDisplayNextActionNameFQ
		{
			get => MakeActionNameFQ(RightSegmentDisplayNextActionName);
		}

		/// <summary>Peek current right segment display action name.</summary>
		/// <remarks>
		/// Fully qualified version is required for use in xaml / ActionName on a control.
		/// Simpler version can be used by the SimHub API (<see cref="SimHub.Plugins.PluginManager.AddAction"/>).
		/// </remarks>
		public String RightSegmentDisplayPeekCurrentActionName
		{
			get => MakeActionName("RightSegmentDisplayPeekCurrent");
		}

		/// <summary>Peek current right segment display action name.</summary>
		/// <remarks>
		/// Fully qualified version is required for use in xaml / ActionName on a control.
		/// Simpler version can be used by the SimHub API (<see cref="SimHub.Plugins.PluginManager.AddAction"/>).
		/// </remarks>
		public String RightSegmentDisplayPeekCurrentActionNameFQ
		{
			get => MakeActionNameFQ(RightSegmentDisplayPeekCurrentActionName);
		}

		/// <summary>Right segment display previous action name.</summary>
		/// <remarks>
		/// Fully qualified version is required for use in xaml / ActionName on a control.
		/// Simpler version can be used by the SimHub API (<see cref="SimHub.Plugins.PluginManager.AddAction"/>).
		/// </remarks>
		public String RightSegmentDisplayPreviousActionName
		{
			get => MakeActionName("RightSegmentDisplayPrevious");
		}

		/// <summary>Right segment display previous action name.</summary>
		/// <remarks>
		/// Fully qualified version is required for use in xaml / ActionName on a control.
		/// Simpler version can be used by the SimHub API (<see cref="SimHub.Plugins.PluginManager.AddAction"/>).
		/// </remarks>
		public String RightSegmentDisplayPreviousActionNameFQ
		{
			get => MakeActionNameFQ(RightSegmentDisplayPreviousActionName);
		}

		/// <summary>Make an action name for use by <see cref="SimHub.Plugins.PluginManager.AddAction"/>.</summary>
		/// <remarks>
		/// <see cref="SimHub.Plugins.PluginManager.AddAction"/> implicitly adds the plugin name to the action but we need
		/// to specify it in xaml (a control's ActionName) hence we have <see cref="MakeActionName"/>
		/// (to pass to <see cref="SimHub.Plugins.PluginManager.AddAction"/>) and <see cref="MakeActionNameFQ"/> (for xaml).
		/// The serial number of the device is included in the formatted action name, so that multiple devices can support
		/// separate actions.
		/// </remarks>
		/// <param name="baseName"></param>
		public String MakeActionName(String baseName)
		{
			return String.Format("{0}.{1}", DeviceInfo.SerialNumber, baseName);
		}

		/// <summary>Make a fully qualified action name.</summary>
		/// <remarks>
		/// <see cref="SimHub.Plugins.PluginManager.AddAction"/> implicitly adds the plugin name to the action but we need
		/// to specify it in xaml (a control's ActionName) hence we have <see cref="MakeActionName"/>
		/// (to pass to <see cref="SimHub.Plugins.PluginManager.AddAction"/>) and <see cref="MakeActionNameFQ"/> (for xaml).
		/// The serial number of the device is included in the formatted action name, so that multiple devices can support
		/// separate actions.
		/// </remarks>
		/// <param name="actionName">The return value from <see cref="MakeActionName"/>.</param>
		/// <returns></returns>
		public String MakeActionNameFQ(String actionName)
		{
			return String.Format("{0}.{1}", typeof(SliPlugin).Name, actionName);
		}

		/// <inheritdoc/>
		public event PropertyChangedEventHandler PropertyChanged;

		/// <inheritdoc/>
		private void OnPropertyChanged([CallerMemberName] string name = null)
		{
			PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
		}

		private SegmentDisplayManager.SegmentDisplayMode[] GetSegmentDisplayNameList(
			Device.SegmentDisplayPosition segmentDisplayPosition)
		{
			// TODO try to remove this, ideally. Move segment list to settings, I guess.
			if (ManagedDevice != null)
				return ManagedDevice.GetSegmentDisplayNameList(segmentDisplayPosition);
			else
				return new SegmentDisplayManager.SegmentDisplayMode[] { };
		}

		private bool m_isExpanded = false;
		private bool m_isManaged = false;
		private ManagedDevice m_managedDevice = null;
	}
}

// Ambiguous reference in cref attribute.
#pragma warning restore CS0419
