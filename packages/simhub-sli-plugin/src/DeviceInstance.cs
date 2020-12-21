/*
 * SimHub SLI plugin.
 */

using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Data;
using System.Windows.Media;
using SimElation.SliDevices;
using SimHub.Plugins.OutputPlugins.Dash.GLCDTemplating;

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

			/// <summary>Settings for a single rotary -> vJoy mapping.</summary>
			public sealed class RotarySwitchMapping : INotifyPropertyChanged
			{
				/// <summary>The rotary switch for this mapping.</summary>
				public int RotarySwitchIndex
				{
					get => m_rotarySwitchIndex;

					set
					{
						m_rotarySwitchIndex = value;
						OnPropertyChanged();
					}
				}

				/// <summary>The number of positions for the rotary switch.</summary>
				public uint NumberOfPositions
				{
					get => m_numberOfPositions;

					set
					{
						m_numberOfPositions = value;
						OnPropertyChanged();
					}
				}

				/// <summary>The vJoy device to map to.</summary>
				public uint VJoyDeviceId
				{
					get => m_vJoyDeviceId;

					set
					{
						m_vJoyDeviceId = value;
						OnPropertyChanged();
					}
				}

				/// <summary>
				/// The vJoy button id to use for the first rotary position. Successive rotary positions increment the id.
				/// </summary>
				public uint FirstVJoyButtonId
				{
					get => m_firstVJoyButtonId;

					set
					{
						m_firstVJoyButtonId = value;
						OnPropertyChanged();
					}
				}

				/// <inheritdoc/>
				public event PropertyChangedEventHandler PropertyChanged;

				/// <inheritdoc/>
				private void OnPropertyChanged([CallerMemberName] string name = null)
				{
					PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
				}

				private int m_rotarySwitchIndex = RotarySwitchDetector.unknownIndex;
				private uint m_numberOfPositions = 12;
				private uint m_vJoyDeviceId = VJoyManager.Instance.GetDefaultDeviceId();
				private uint m_firstVJoyButtonId = 3;
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

			/// <summary>How many RPM LEDs to use for bindable formulas (and not revs), from the left.</summary>
			public uint NumberOfRpmStatusLeds
			{
				get => m_numberOfRpmStatusLeds;

				set
				{
					m_numberOfRpmStatusLeds = value;
					OnPropertyChanged();
				}
			}

			/// <summary>SimHub properties assigned to RPM LEDs.</summary>
			public Led[] RpmStatusLeds { get; set; }

			/// <summary>Left status LEDs.</summary>
			public Led[] LeftStatusLeds { get; set; }

			/// <summary>Right status LEDs.</summary>
			public Led[] RightStatusLeds { get; set; }

			/// <summary>SimHub properties assigned to external LEDs.</summary>
			public Led[] ExternalLeds { get; set; }

			/// <summary>How long to press a vJoy button for, in milliseconds.</summary>
			public int VJoyButtonPulseMs
			{
				get => m_vJoyButtonPulseMs;

				set
				{
					m_vJoyButtonPulseMs = value;
					OnPropertyChanged();
				}
			}

			/// <summary>The set of rotary switch -> vJoy mappings.</summary>
			public ObservableCollection<RotarySwitchMapping> RotarySwitchMappings
			{
				get => m_rotarySwitchMappings;

				set
				{
					m_rotarySwitchMappings = value;
					OnPropertyChanged();
				}
			}

			/// <summary>Default constructor.</summary>
			public Settings()
			{
				BindingOperations.EnableCollectionSynchronization(m_rotarySwitchMappings, m_lock);
			}

			/// <summary>Constructor.</summary>
			/// <param name="sliPluginDeviceDescriptor">Descriptor describing the device.</param>
			public Settings(ISliPluginDeviceDescriptor sliPluginDeviceDescriptor) : this()
			{
				// Construct anything requiring the descriptor.
				Fixup(sliPluginDeviceDescriptor);
			}

			/// <summary>Fixup any dynamic length / device-dependent settings.</summary>
			/// <remarks>
			/// When deserializing, the device descriptor isn't know. So anything "new" added to the config
			/// that is dependent on the device details won't be initialized. Also just make sure dynamic length arrays are the
			/// correct length, in case any mangling of config file happens.
			/// </remarks>
			public void Fixup(ISliPluginDeviceDescriptor sliPluginDeviceDescriptor)
			{
				// In pit-lane LED animation. Default pattern is to alternate based on the colors changing.
				var rpmLeds = PitLaneLeds1;
				Array.Resize(ref rpmLeds, sliPluginDeviceDescriptor.DeviceDescriptor.Constants.RevLedColors.Length);
				PitLaneLeds1 = rpmLeds;

				rpmLeds = PitLaneLeds2;
				Array.Resize(ref rpmLeds, sliPluginDeviceDescriptor.DeviceDescriptor.Constants.RevLedColors.Length);
				PitLaneLeds2 = rpmLeds;

				Color? previousColor = null;
				bool isLed1Set = false;

				for (uint i = 0; i < PitLaneLeds1.Length; ++i)
				{
					var currentColor = sliPluginDeviceDescriptor.DeviceDescriptor.Constants.RevLedColors[i];
					if (previousColor != currentColor)
					{
						previousColor = currentColor;
						isLed1Set ^= true;
					}

					PitLaneLeds1[i] ??= new RpmLed(currentColor, isLed1Set);
					PitLaneLeds2[i] ??= new RpmLed(currentColor, !isLed1Set);
				}

				// RPM LEDs bound to expressions.
				var statusLeds = RpmStatusLeds;
				Array.Resize(ref statusLeds, sliPluginDeviceDescriptor.DeviceDescriptor.Constants.RevLedColors.Length);
				RpmStatusLeds = statusLeds;

				for (uint i = 0; i < RpmStatusLeds.Length; ++i)
				{
					RpmStatusLeds[i] ??=
						new Led()
						{
							SetBrush = new SolidColorBrush(sliPluginDeviceDescriptor.DeviceDescriptor.Constants.RevLedColors[i]),
							EditPropertyName = String.Format("RPM LED {0}", (i + 1))
						};
				}

				// Need a deep copy of default Led arrays.
				// TODO surely there's a better way.
				Led[] CopyStatusLedArray(Led[] sourceArray)
				{
					var targetArray = new Led[sourceArray.Length];

					for (int i = 0; i < sourceArray.Length; ++i)
					{
						var sourceLed = sourceArray[i];
						targetArray[i] ??=
							new Led()
							{
								ExpressionValue = new ExpressionValue(sourceLed.ExpressionValue.Expression,
									sourceLed.ExpressionValue.Interpreter),
								EditPropertyName = sourceLed.EditPropertyName,
								SetBrush = sourceLed.SetBrush.Clone()
							};
					}

					return targetArray;
				}

				statusLeds = LeftStatusLeds;
				Array.Resize(ref statusLeds, sliPluginDeviceDescriptor.LeftStatusLeds.Length);
				LeftStatusLeds = statusLeds;
				LeftStatusLeds = CopyStatusLedArray(sliPluginDeviceDescriptor.LeftStatusLeds);

				statusLeds = RightStatusLeds;
				Array.Resize(ref statusLeds, sliPluginDeviceDescriptor.RightStatusLeds.Length);
				RightStatusLeds = statusLeds;
				RightStatusLeds = CopyStatusLedArray(sliPluginDeviceDescriptor.RightStatusLeds);

				// External LEDs.
				statusLeds = ExternalLeds;
				Array.Resize(ref statusLeds, (int)sliPluginDeviceDescriptor.DeviceDescriptor.Constants.NumberOfExternalLeds);
				ExternalLeds = statusLeds;

				for (uint i = 0; i < ExternalLeds.Length; ++i)
				{
					ExternalLeds[i] ??=
						new Led()
						{
							EditPropertyName = String.Format("External LED {0}", (i + 1))
						};
				}
			}

			private object m_lock = new object();

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

			private uint m_numberOfRpmStatusLeds = 0;

			private int m_vJoyButtonPulseMs = 50;
			private ObservableCollection<RotarySwitchMapping> m_rotarySwitchMappings =
				new ObservableCollection<RotarySwitchMapping>();
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
