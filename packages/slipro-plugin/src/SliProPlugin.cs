/*
 * SimHub SLI-Pro plugin.
 */

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Threading;
using GameReaderCommon;
using SimElation.SliPro;
using SimHub.Plugins;
using Logging = SimHub.Logging;

// ---------------------------------------------------------------------------------------------------------------------------------

namespace SimElation.SimHubIntegration.SliProPlugin
{
	/// <summary>Game data with extras from the PersistantTrackerPlugin.</summary>
	public class NormalizedData
	{
		/// <summary>
		/// <see cref="StatusDataBase.DeltaToSessionBest"/> if non-null,
		/// otherwise PersistantTrackerPlugin.SessionBestLiveDeltaSeconds.
		/// </summary>
		public double? m_deltaToSessionBest;

		/// <summary>
		/// <see cref="StatusDataBase.DeltaToAllTimeBest"/> if non-null,
		/// otherwise PersistantTrackerPlugin.AllTimeBestLiveDeltaSeconds.
		/// </summary>
		public double? m_deltaToAllTimeBest;

		/// <summary>
		/// <see cref="StatusDataBase.EstimatedFuelRemaingLaps"/> if non-null,
		/// otherwise PersistantTrackerPlugin.EstimatedFuelRemaingLaps.
		/// </summary>
		public double? m_fuelRemainingLaps;

		/// <summary>Game data as passed to the plugin's <see cref="IDataPlugin.DataUpdate"/> method.</summary>
		public GameReaderCommon.StatusDataBase m_statusData;
	}

	/// <summary>SimHub SLI-Pro plugin.</summary>
	[PluginDescription("SimElation SLI-Pro SimHub Plugin")]
	[PluginAuthor("SimElation")]
	[PluginName("SLI-Pro Plugin")]
	public class SliProPlugin : IPlugin, IDataPlugin, IWPFSettings
	{
		private const String settingsName = "Settings";

		private Settings m_settings;
		private SliPro.SliPro m_sliPro;

		// Class to manage left/right segment displays.
		private class SegmentDisplayManager
		{
			private SliPro.SegmentDisplayPosition m_position;
			private SegmentDisplay[] m_segmentDisplayList;

			private Timer m_timer;
			private bool m_showName = false;
			private int m_currentIndex = int.MaxValue;

			public String[] NameList
			{
				get => Array.ConvertAll(m_segmentDisplayList, (SegmentDisplay segmentDisplay) => segmentDisplay.LongName);
			}

			public SegmentDisplayManager(SliPro.SegmentDisplayPosition position, SegmentDisplay[] segmentDisplayList)
			{
				m_position = position;
				m_timer = new Timer((object state) => m_showName = false);
				m_segmentDisplayList = segmentDisplayList;
			}

			public void ProcessData(PluginManager pluginManager, NormalizedData normalizedData, SliPro.SliPro sliPro)
			{
				if (m_currentIndex < m_segmentDisplayList.Length)
				{
					var segmentDisplay = m_segmentDisplayList[m_currentIndex];

					if (m_showName)
						segmentDisplay.ShowName(sliPro, m_position);
					else
						segmentDisplay.ProcessData(pluginManager, normalizedData, sliPro, m_position);
				}
				else
				{
					sliPro.SetSegment(m_position, 0, SliPro.Constants.segmentDisplayWidth, "n-a");
				}
			}

			public void SetByIndex(int newIndex, long segmentNameTimeoutMs)
			{
				m_currentIndex = newIndex;

				if (segmentNameTimeoutMs > 0)
				{
					// For future data updates, show the name of the current segment until the timer fires.
					m_timer.Change(segmentNameTimeoutMs, Timeout.Infinite);
					m_showName = true;
				}
			}

			public void SetByName(String longName, long segmentNameTimeoutMs)
			{
				int index =
					Array.FindIndex(m_segmentDisplayList, (SegmentDisplay segmentDisplay) => segmentDisplay.LongName == longName);
				if (index != -1)
					SetByIndex(index, segmentNameTimeoutMs);
			}
		}

		// Left/right segment managers.
		private SegmentDisplayManager[] m_segmentDisplayManagerList;

		private enum BlinkState
		{
			inactive,       // Not in blinking state.
			on,             // In blinking on state.
			off             // In blinking off state.
		}

		private BlinkState m_shiftPointBlinkState = BlinkState.inactive;
		private Stopwatch m_shiftPointStopwatch = new Stopwatch();

		private BlinkState m_pitLaneRevLightsBlinkState = BlinkState.inactive;
		private Stopwatch m_pitLaneRevLightsStopwatch = new Stopwatch();
		private static bool[] pitLaneLeds1 = { true, true, true, true, false, false, false, false, false, true, true, true, true };
		private static bool[] pitLaneLeds2 = { false, false, false, false, true, true, true, true, true };

		private NormalizedData m_normalizedData = new NormalizedData();

		/// <summary>Instance of the current plugin manager.</summary>
		public PluginManager PluginManager { get; set; }

		/// <summary>Settings.</summary>
		public Settings Settings { get => m_settings; }

		/// <summary>Called once after plugins startup. Plugins are rebuilt at game change.</summary>
		/// <param name="pluginManager"></param>
		public void Init(PluginManager pluginManager)
		{
			double? NextCarDelta(List<Opponent> opponentList)
			{
				if ((0 == opponentList.Count) || (opponentList[0].RelativeGapToPlayer == null))
					return null;

				return Math.Abs((double)opponentList[0].RelativeGapToPlayer);
			}

			Logging.Current.InfoFormat("SLI-Pro: initializing plugin in thread {0}", Thread.CurrentThread.ManagedThreadId);

			m_segmentDisplayManagerList = new SegmentDisplayManager[(int)SegmentDisplayPosition.count];

			// Left segment displays.
			m_segmentDisplayManagerList[(int)SegmentDisplayPosition.left] = new SegmentDisplayManager(
				SliPro.SegmentDisplayPosition.left, new SegmentDisplay[]
				{
					new LapsCounterSegmentDisplay(),
					new LapsToGoSegmentDisplay(),
					new PositionSegmentDisplay(),
					new FuelSegmentDisplay(),
					new BrakeBiasSegmentDisplay(),
					new TempSegmentDisplay("Oil", "Oil temperature", "Oil",
						(NormalizedData normalizedData) => normalizedData.m_statusData.OilTemperature),
					new TempSegmentDisplay("H20", "Water temperature", "H20",
						(NormalizedData normalizedData) => normalizedData.m_statusData.WaterTemperature),
				});

			// Right segment displays.
			m_segmentDisplayManagerList[(int)SegmentDisplayPosition.right] = new SegmentDisplayManager(
				SliPro.SegmentDisplayPosition.right, new SegmentDisplay[]
				{
					new LapTimeSegmentDisplay("Currnt", "Current laptime",
						(NormalizedData normalizedData) => normalizedData.m_statusData.CurrentLapTime),
					new LapTimeSegmentDisplay("Last", "Last laptime",
						(NormalizedData normalizedData) => normalizedData.m_statusData.LastLapTime),
					new LapTimeSegmentDisplay("BstSes", "Session best laptime",
						(NormalizedData normalizedData) => normalizedData.m_statusData.BestLapTime),
					new LapTimeSegmentDisplay("BstAll", "All-time best laptime",
						(NormalizedData normalizedData) => normalizedData.m_statusData.AllTimeBest),
					new DeltaSegmentDisplay("DltSeS", "Delta to session best laptime",
						(NormalizedData normalizedData) => normalizedData.m_deltaToSessionBest),
					new DeltaSegmentDisplay("DltAll", "Delta to all-time best laptime",
						(NormalizedData normalizedData) => normalizedData.m_deltaToAllTimeBest),
					new DeltaSegmentDisplay("GapAhd", "Gap to car ahead", (NormalizedData normalizedData) =>
						{
							// Semantics of OpponentAtPosition seem unclear, so peek list of opponents.
							var opponentList = normalizedData.m_statusData.OpponentsAheadOnTrackPlayerClass;
							return NextCarDelta(opponentList);
						}),
					new DeltaSegmentDisplay("GapBhd", "Gap to car behind", (NormalizedData normalizedData) =>
						{
							// Semantics of OpponentAtPosition seem unclear, so peek list of opponents.
							var opponentList = normalizedData.m_statusData.OpponentsBehindOnTrackPlayerClass;
							return NextCarDelta(opponentList);
						})
				});

			// Load settings.
			m_settings = this.ReadCommonSettings<Settings>(settingsName, () => new Settings());

			// I'm assuming Logging.Current changes when the log file rolls over, so pass a function to access it.
			m_sliPro = new SliPro.SliPro(m_settings.SliProSettings, () => Logging.Current, OnRotarySwitchChange);

			// Watch for some property changes in settings.
			m_settings.PropertyChanged +=
				(object sender, PropertyChangedEventArgs e) =>
				{
					void ProcessSegmentDisplayChange(SegmentDisplayPosition segmentDisplayPosition, String segmentDisplay,
						RotarySwitch rotarySwitch)
					{
						if (segmentDisplay != null)
						{
							if (segmentDisplay == Settings.RotaryControlledKey)
							{
								// Reset such that next message from the SLI-Pro tells us which segment to display.
								m_sliPro.ResetRotarySwitchPosition(rotarySwitch);
							}
							else
							{
								m_segmentDisplayManagerList[(int)segmentDisplayPosition].SetByName(segmentDisplay,
									m_settings.SegmentNameTimeoutMs);
							}
						}
					}

					switch (e.PropertyName)
					{
						case nameof(m_settings.LeftSegmentDisplay):
							ProcessSegmentDisplayChange(SegmentDisplayPosition.left, m_settings.LeftSegmentDisplay,
								RotarySwitch.leftSegment);
							break;

						case nameof(m_settings.RightSegmentDisplay):
							ProcessSegmentDisplayChange(SegmentDisplayPosition.right, m_settings.RightSegmentDisplay,
								RotarySwitch.rightSegment);
							break;

						default:
							break;
					}
				};

			Logging.Current.Info("SLI-Pro: initialization complete");
		}

		/// <summary>
		/// Called at plugin manager stop, close/dispose anything needed here. Plugins are rebuilt at game change.
		/// </summary>
		/// <param name="pluginManager"></param>
		public void End(PluginManager pluginManager)
		{
			Logging.Current.InfoFormat("SLI-Pro: shutting down plugin in thread {0}", Thread.CurrentThread.ManagedThreadId);

			m_sliPro.Dispose();

			// Save settings.
			this.SaveCommonSettings(settingsName, m_settings);

			Logging.Current.Info("SLI-Pro: plugin shut down");
		}

		/// <summary>Returns the settings control, return null if no settings control is required.</summary>
		/// <param name="pluginManager"></param>
		/// <returns></returns>
		public System.Windows.Controls.Control GetWPFSettingsControl(PluginManager pluginManager)
		{
			return new SettingsControl(this);
		}

		/// <summary>Get long names of all configured segment displays.</summary>
		/// <param name="segmentDisplayPosition">Which segment display to get the list for.</param>
		/// <returns>Array of strings.</returns>
		public String[] GetSegmentDisplayNameList(SegmentDisplayPosition segmentDisplayPosition)
		{
			return m_segmentDisplayManagerList[(int)segmentDisplayPosition].NameList;
		}

		/// <summary>Set the brightness of the device's LEDs.</summary>
		/// <param name="level">The brightness between 0 (dullest) and 254 (brightest).</param>
		public void SetBrightness(byte level)
		{
			if (m_sliPro.IsAvailable)
				m_sliPro.SendBrightness(level);
		}

		/// <summary>
		/// Learn (if not currently known) or forget (if known) the rotary switch for left/right segment control, or brightness.
		/// </summary>
		/// <param name="rotarySwitch">Which rotary switch to detect/forget.</param>
		public void LearnOrForgetRotary(SliPro.RotarySwitch rotarySwitch)
		{
			if (m_settings.SliProSettings.RotarySwitchOffsets[rotarySwitch] == SliPro.RotaryDetector.undefinedOffset)
				m_sliPro.LearnRotary(rotarySwitch);
			else
				m_sliPro.ForgetRotary(rotarySwitch);
		}

		/// <summary>
		/// Called one time per game data update, contains all normalized game data,
		/// raw data are intentionnally "hidden" under a generic object type (A plugin SHOULD NOT USE IT)
		///
		/// This method is on the critical path, it must execute as fast as possible and avoid throwing any error
		/// </summary>
		/// <param name="pluginManager"></param>
		/// <param name="data"></param>
		public void DataUpdate(PluginManager pluginManager, ref GameData data)
		{
			//Logging.Current.InfoFormat("SLI-Pro: data update in thread {0}", Thread.CurrentThread.ManagedThreadId);

			if (!m_sliPro.IsAvailable)
				return;

			if (data.GameRunning)
			{
				if (!data.GamePaused && (data.NewData != null))
				{
					// Fix up a few potentially missing things.
					NormalizeData(pluginManager, data.NewData);

					// Left bank status LEDs.
					// TODO blink?
					m_sliPro.SetStatusLed(0, m_normalizedData.m_statusData.Flag_Blue != 0);
					m_sliPro.SetStatusLed(1, m_normalizedData.m_statusData.Flag_Yellow != 0);
					m_sliPro.SetStatusLed(2, m_normalizedData.m_statusData.CarSettings_FuelAlertActive != 0);

					// Right bank status LEDs.
					m_sliPro.SetStatusLed(3, m_normalizedData.m_statusData.ABSActive != 0);
					m_sliPro.SetStatusLed(4, m_normalizedData.m_statusData.TCActive != 0);
					// TODO blink for DRS available. Solid for active.
					m_sliPro.SetStatusLed(5, m_normalizedData.m_statusData.DRSAvailable != 0);

					m_sliPro.SetGear(m_normalizedData.m_statusData.Gear);
					bool isInPit = HandlePitLane();
					HandleShiftPoint(isInPit);

					foreach (var segmentDisplayManager in m_segmentDisplayManagerList)
					{
						segmentDisplayManager.ProcessData(pluginManager, m_normalizedData, m_sliPro);
					}
				}
				else
				{
					m_sliPro.ResetLedState();
					m_sliPro.SetTextMessage(data.GameName);
				}
			}
			else
			{
				m_sliPro.ResetLedState();
				m_sliPro.SetTextMessage(m_settings.WelcomeMessage);
			}

			m_sliPro.SendLedState();
		}

		private void NormalizeData(PluginManager pluginManager, StatusDataBase statusData)
		{
			m_normalizedData.m_statusData = statusData;

			if (statusData.DeltaToSessionBest != null)
			{
				m_normalizedData.m_deltaToSessionBest = statusData.DeltaToSessionBest;
			}
			else
			{
				m_normalizedData.m_deltaToSessionBest =
					(double?)pluginManager.GetPropertyValue("PersistantTrackerPlugin.SessionBestLiveDeltaSeconds");
			}

			if (statusData.DeltaToAllTimeBest != null)
			{
				m_normalizedData.m_deltaToAllTimeBest = statusData.DeltaToAllTimeBest;
			}
			else
			{
				m_normalizedData.m_deltaToAllTimeBest =
					(double?)pluginManager.GetPropertyValue("PersistantTrackerPlugin.AllTimeBestLiveDeltaSeconds");
			}

			if (statusData.EstimatedFuelRemaingLaps != null)
			{
				m_normalizedData.m_fuelRemainingLaps = statusData.EstimatedFuelRemaingLaps;
			}
			else
			{
				m_normalizedData.m_fuelRemainingLaps =
					(double?)pluginManager.GetPropertyValue("DataCorePlugin.Computed.Fuel_RemainingLaps");
			}
		}

		private void OnRotarySwitchChange(SliPro.RotarySwitch rotarySwitch, int previousPosition, int newPosition)
		{
			Logging.Current.InfoFormat("SLI-Pro: rotary switch {0} was {1}, now {2}", rotarySwitch, previousPosition, newPosition);

			switch (rotarySwitch)
			{
				case SliPro.RotarySwitch.leftSegment:
					if (m_settings.LeftSegmentDisplay == Settings.RotaryControlledKey)
					{
						m_segmentDisplayManagerList[(int)SegmentDisplayPosition.left].SetByIndex(newPosition,
							m_settings.SegmentNameTimeoutMs);
					}
					break;

				case SliPro.RotarySwitch.rightSegment:
					if (m_settings.RightSegmentDisplay == Settings.RotaryControlledKey)
					{
						m_segmentDisplayManagerList[(int)SegmentDisplayPosition.right].SetByIndex(newPosition,
							m_settings.SegmentNameTimeoutMs);
					}
					break;

				case SliPro.RotarySwitch.brightness:
					// Ignore the brightness control if we are setting one explicitly.
					if (m_settings.SliProSettings.Brightness == null)
						SetBrightness((byte)((254 / 12) * newPosition));
					break;
			}
		}

		private bool HandlePitLane()
		{
			if (0 == m_settings.PitLaneAnimationSpeedMs)
				return false;

			// AMS2 note: both these are false when leaving the car hole!
			bool isInPits = (m_normalizedData.m_statusData.IsInPitLane != 0) || (m_normalizedData.m_statusData.IsInPit != 0);

			// TODO could maybe remove a state here; m_pitLaneRevLightsStopwatch.IsRunning is a thing.
			switch (m_pitLaneRevLightsBlinkState)
			{
				case BlinkState.inactive:
					if (isInPits)
					{
						m_pitLaneRevLightsBlinkState = BlinkState.on;
						m_pitLaneRevLightsStopwatch.Start();
					}
					break;

				case BlinkState.on:
					if (isInPits)
					{
						if (m_pitLaneRevLightsStopwatch.ElapsedMilliseconds >= m_settings.PitLaneAnimationSpeedMs)
						{
							m_pitLaneRevLightsBlinkState = BlinkState.off;
							m_pitLaneRevLightsStopwatch.Restart();
						}
					}
					else
					{
						m_pitLaneRevLightsBlinkState = BlinkState.inactive;
						m_pitLaneRevLightsStopwatch.Stop();
					}
					break;

				case BlinkState.off:
					if (isInPits)
					{
						if (m_pitLaneRevLightsStopwatch.ElapsedMilliseconds >= m_settings.PitLaneAnimationSpeedMs)
						{
							m_pitLaneRevLightsBlinkState = BlinkState.on;
							m_pitLaneRevLightsStopwatch.Restart();
						}
					}
					else
					{
						m_pitLaneRevLightsBlinkState = BlinkState.inactive;
						m_pitLaneRevLightsStopwatch.Stop();
					}
					break;
			}

			bool[] leds = pitLaneLeds1;

			if (isInPits)
			{
				if (BlinkState.off == m_pitLaneRevLightsBlinkState)
					leds = pitLaneLeds2;

				m_sliPro.SetRevLeds(leds);

				return true;
			}

			return false;
		}

		private void HandleShiftPoint(bool isInPit)
		{
			// No shift indicators if in top gear.
			int gear;
			bool isTopGear = int.TryParse(m_normalizedData.m_statusData.Gear, out gear) &&
				(gear >= m_normalizedData.m_statusData.CarSettings_MaxGears);

			double rpmPercent = m_normalizedData.m_statusData.CarSettings_CurrentDisplayedRPMPercent;
			bool isShiftPointActive = !isTopGear && (0 != m_normalizedData.m_statusData.CarSettings_RPMRedLineReached);

			// TODO could maybe remove a state here; m_shiftPointStopwatch.IsRunning is a thing.
			switch (m_shiftPointBlinkState)
			{
				case BlinkState.inactive:
					if (isShiftPointActive)
					{
						m_shiftPointBlinkState = BlinkState.on;
						m_shiftPointStopwatch.Start();
					}
					break;

				case BlinkState.on:
					if (isShiftPointActive)
					{
						if ((m_shiftPointStopwatch.ElapsedMilliseconds >= m_settings.ShiftPointBlinkOnSpeedMs) &&
							(m_settings.ShiftPointBlinkOffSpeedMs > 0))
						{
							m_shiftPointBlinkState = BlinkState.off;
							m_shiftPointStopwatch.Restart();
						}
					}
					else
					{
						m_shiftPointBlinkState = BlinkState.inactive;
						m_shiftPointStopwatch.Stop();
					}
					break;

				case BlinkState.off:
					if (isShiftPointActive)
					{
						if (m_shiftPointStopwatch.ElapsedMilliseconds >= m_settings.ShiftPointBlinkOffSpeedMs)
						{
							m_shiftPointBlinkState = BlinkState.on;
							m_shiftPointStopwatch.Restart();
						}
						else
						{
							// Turn off LEDs when blinking.
							rpmPercent = 0;
						}
					}
					else
					{
						m_shiftPointBlinkState = BlinkState.inactive;
						m_shiftPointStopwatch.Stop();
					}
					break;
			}

			if (!isInPit)
			{
				double minRpmPercent = (100 * m_normalizedData.m_statusData.CarSettings_MinimumShownRPM) /
					m_normalizedData.m_statusData.CarSettings_MaxRPM;

				m_sliPro.SetRevLeds(minRpmPercent, rpmPercent, 0, SliPro.Constants.numberOfRevLeds);
			}
		}
	}
}
