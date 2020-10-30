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

		/// <summary>Is the left segment display rotary controlled?</summary>
		public bool IsLeftSegmentDisplayRotaryControlled
		{
			get => m_isLeftSegmentDisplayRotaryControlled;

			set
			{
				if (m_isLeftSegmentDisplayRotaryControlled != value)
				{
					m_isLeftSegmentDisplayRotaryControlled = value;
					OnPropertyChanged();
				}
			}
		}

		/// <summary>Is the right segment display rotary controlled?</summary>
		public bool IsRightSegmentDisplayRotaryControlled
		{
			get => m_isRightSegmentDisplayRotaryControlled;

			set
			{
				if (m_isRightSegmentDisplayRotaryControlled != value)
				{
					m_isRightSegmentDisplayRotaryControlled = value;
					OnPropertyChanged();
				}
			}
		}

		/// <summary>Left segment display mode.</summary>
		public int LeftSegmentDisplayIndex
		{
			get => m_leftSegmentDisplayIndex;

			set
			{
				if (m_leftSegmentDisplayIndex != value)
				{
					m_leftSegmentDisplayIndex = value;
					OnPropertyChanged();
				}
			}
		}

		/// <summary>Right segment display mode.</summary>
		public int RightSegmentDisplayIndex
		{
			get => m_rightSegmentDisplayIndex;

			set
			{
				if (m_rightSegmentDisplayIndex != value)
				{
					m_rightSegmentDisplayIndex = value;
					OnPropertyChanged();
				}
			}
		}

		/// <summary>Text to display using the segment displays when SimHub is running but no game is.</summary>
		public String WelcomeMessage
		{
			get => m_welcomeMessage;

			set
			{
				if (m_welcomeMessage != value)
				{
					m_welcomeMessage = value;
					OnPropertyChanged();
				}
			}
		}

		/// <summary>The rate in milliseconds at which to animate the RPM LEDs when in the pitlane. 0 to disable.</summary>
		public long PitLaneAnimationSpeedMs
		{
			get => m_pitLaneAnimationSpeedMs;

			set
			{
				if (m_pitLaneAnimationSpeedMs != value)
				{
					m_pitLaneAnimationSpeedMs = value;
					OnPropertyChanged();
				}
			}
		}

		/// <summary>How long in milliseconds to light the RPM LEDs when at or above the shift threshold.</summary>
		public long ShiftPointBlinkOnSpeedMs
		{
			get => m_shiftPointBlinkOnSpeedMs;

			set
			{
				if (m_shiftPointBlinkOnSpeedMs != value)
				{
					m_shiftPointBlinkOnSpeedMs = value;
					OnPropertyChanged();
				}
			}
		}

		/// <summary>How long in milliseconds to blank the RPM LEDs when at or above the shift threshold.</summary>
		public long ShiftPointBlinkOffSpeedMs
		{
			get => m_shiftPointBlinkOffSpeedMs;

			set
			{
				if (m_shiftPointBlinkOffSpeedMs != value)
				{
					m_shiftPointBlinkOffSpeedMs = value;
					OnPropertyChanged();
				}
			}
		}

		/// <summary>How long to display what information a segment display will show after changing mode.</summary>
		public long SegmentNameTimeoutMs
		{
			get => m_segmentNameTimeoutMs;

			set
			{
				if (m_segmentNameTimeoutMs != value)
				{
					m_segmentNameTimeoutMs = value;
					OnPropertyChanged();
				}
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

		private bool m_isLeftSegmentDisplayRotaryControlled = true;
		private bool m_isRightSegmentDisplayRotaryControlled = true;
		private int m_leftSegmentDisplayIndex = -1;
		private int m_rightSegmentDisplayIndex = -1;
		private String m_welcomeMessage = "Sim-Hub";
		private long m_pitLaneAnimationSpeedMs = 250;
		private long m_shiftPointBlinkOnSpeedMs = 100;
		private long m_shiftPointBlinkOffSpeedMs = 50;
		private long m_segmentNameTimeoutMs = 1500;
		private SliPro.SliPro.Settings m_sliProSettings = new SliPro.SliPro.Settings();
	}
}
