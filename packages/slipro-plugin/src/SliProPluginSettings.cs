/*
 * SimHub SLI-Pro plugin settings.
 */

using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;

// ---------------------------------------------------------------------------------------------------------------------------------

namespace SimElation.SimHubIntegration.SliProPlugin
{
	/// <summary>Settings for the SLI-Pro plugin.</summary>
	public class Settings : INotifyPropertyChanged
	{
		// TODO investigate helpers to reduce boilerplate.

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

		/// <summary>SLI-Pro settings accessor.</summary>
		public SliPro.SliPro.Settings SliProSettings
		{
			get => m_sliProSettings;
		}

		/// <inheritdoc/>
		public event PropertyChangedEventHandler PropertyChanged;

		/// <inheritdoc/>
		protected void OnPropertyChanged([CallerMemberName] string name = null)
		{
			PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
		}

		private int m_leftSegmentDisplayRotarySwitchIndex = SliPro.RotaryDetector.unknownIndex;
		private int m_leftSegmentDisplayIndex = 0;

		private int m_rightSegmentDisplayRotarySwitchIndex = SliPro.RotaryDetector.unknownIndex;
		private int m_rightSegmentDisplayIndex = 0;

		private String m_welcomeMessage = "Sim-Hub";
		private long m_pitLaneAnimationSpeedMs = 250;
		private long m_shiftPointBlinkOnSpeedMs = 100;
		private long m_shiftPointBlinkOffSpeedMs = 50;
		private long m_statusLedBlinkIntervalMs = 500;
		private long m_segmentNameTimeoutMs = 1500;
		private readonly SliPro.SliPro.Settings m_sliProSettings = new SliPro.SliPro.Settings();
	}
}
