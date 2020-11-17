/*
 * SimHub SLI-Pro plugin.
 */

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
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
		/// <summary>In pit garage or pit lane?</summary>
		/// <remarks>
		/// AMS2 note: both these are false when leaving garage!
		/// </remarks>
		public bool m_isInPit;

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
			private readonly SliPro.SegmentDisplayPosition m_position;
			private readonly SegmentDisplay[] m_segmentDisplayList;

			private readonly Timer m_timer;
			private bool m_showName = false;
			private int m_currentIndex = -1;

			public String[] NameList
			{
				get => Array.ConvertAll(m_segmentDisplayList, (SegmentDisplay segmentDisplay) => segmentDisplay.LongName);
			}

			public SegmentDisplayManager(SliPro.SegmentDisplayPosition position, SegmentDisplay[] segmentDisplayList)
			{
				m_position = position;
				m_segmentDisplayList = segmentDisplayList;

				// When timer fires, stop showing segment name.
				m_timer = new Timer((object state) => m_showName = false);
			}

			public void ProcessData(PluginManager pluginManager, NormalizedData normalizedData, SliPro.SliPro sliPro)
			{
				if (ValidateIndex(m_currentIndex) != -1)
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

			public int GetNextIndex()
			{
				int newIndex = ValidateIndex(m_currentIndex + 1);
				return (newIndex == -1) ? 0 : newIndex;
			}

			public int GetPreviousIndex()
			{
				int newIndex = ValidateIndex(m_currentIndex - 1);
				return (newIndex == -1) ? (m_segmentDisplayList.Length - 1) : newIndex;
			}

			public int ValidateIndex(int newIndex)
			{
				return ((newIndex >= 0) && (newIndex < m_segmentDisplayList.Length)) ? newIndex : -1;
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
		}

		private static double? NextCarDelta(List<Opponent> opponentList)
		{
			if ((0 == opponentList.Count) || (opponentList[0].RelativeGapToPlayer == null))
				return null;

			return (double)opponentList[0].RelativeGapToPlayer;
		}

		// Left/right segment managers.
		private readonly Dictionary<SegmentDisplayPosition, SegmentDisplayManager> m_segmentDisplayManagers =
			new Dictionary<SegmentDisplayPosition, SegmentDisplayManager>()
			{
				// Left segment displays.
				{
					SegmentDisplayPosition.left, new SegmentDisplayManager(SliPro.SegmentDisplayPosition.left, new SegmentDisplay[]
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
						})
				},

				// Right segment displays.
				{
					SliPro.SegmentDisplayPosition.right, new SegmentDisplayManager(SliPro.SegmentDisplayPosition.right,
						new SegmentDisplay[]
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
						})
				}
			};

		// LED state.
		private enum LedState
		{
			off,            // LED off.
			on,             // LED on (solid).
			blink           // LED blinking.
		}

		// Function to get LED state from game data.
		private delegate LedState GetLedState(NormalizedData normalizedData);

		private enum BlinkState
		{
			on,             // In blinking on state.
			off             // In blinking off state.
		}

		private class StatusLed
		{
			/// <summary>Constructor.</summary>
			/// <param name="getLedState">Function to get LED state from game data.</param>
			public StatusLed(GetLedState getLedState)
			{
				m_getLedState = getLedState;
			}

			/// <summary>Process game data from SimHub.</summary>
			/// <param name="normalizedData"></param>
			/// <param name="statusLedBlinkIntervalMs"></param>
			/// <returns>true if the LED should be lit.</returns>
			public bool ProcessData(NormalizedData normalizedData, long statusLedBlinkIntervalMs)
			{
				var ledState = m_getLedState(normalizedData);

				if (LedState.blink == ledState)
				{
					return HandleBlink(statusLedBlinkIntervalMs);
				}
				else
				{
					m_stopwatch.Stop();
					return (LedState.on == ledState);
				}
			}

			private bool HandleBlink(long statusLedBlinkIntervalMs)
			{
				bool isOn = true;

				if (!m_stopwatch.IsRunning)
				{
					m_stopwatch.Restart();
					m_blinkState = BlinkState.on;
				}

				switch (m_blinkState)
				{
					case BlinkState.on:
						if (m_stopwatch.ElapsedMilliseconds >= statusLedBlinkIntervalMs)
						{
							m_stopwatch.Restart();
							m_blinkState = BlinkState.off;
							isOn = false;
						}
						break;

					case BlinkState.off:
						if (m_stopwatch.ElapsedMilliseconds >= statusLedBlinkIntervalMs)
						{
							m_stopwatch.Restart();
							m_blinkState = BlinkState.on;
						}
						else
						{
							isOn = false;
						}
						break;
				}

				return isOn;
			}

			private readonly GetLedState m_getLedState;
			private BlinkState m_blinkState;
			private readonly Stopwatch m_stopwatch = new Stopwatch();
		}

		private StatusLed[] m_statusLeds = new StatusLed[(int)Constants.numberOfStatusLeds]
			{
				// Left bank status LEDs.
				new StatusLed((NormalizedData normalizedData) =>
					(normalizedData.m_statusData.Flag_Blue != 0) ? LedState.on : LedState.off),
				new StatusLed((NormalizedData normalizedData) =>
					(normalizedData.m_statusData.Flag_Yellow != 0) ? LedState.on : LedState.off),
				new StatusLed((NormalizedData normalizedData) =>
					(normalizedData.m_statusData.CarSettings_FuelAlertActive != 0) ? LedState.on : LedState.off),

				// Right bank status LEDs.
				new StatusLed((NormalizedData normalizedData) =>
					(normalizedData.m_statusData.ABSActive != 0) ? LedState.on : LedState.off),
				new StatusLed((NormalizedData normalizedData) =>
					(normalizedData.m_statusData.TCActive != 0) ? LedState.on : LedState.off),
				new StatusLed(
					(NormalizedData normalizedData) =>
					{
						LedState ledState = LedState.off;

						// rf2 (at least?) reports DRS available in the pits (in a practise session), so ignore DRS state if in pit.
						if (!normalizedData.m_isInPit)
						{
							// Blink for DRS available; solid for DRS active.
							if (normalizedData.m_statusData.DRSEnabled != 0)
								ledState = LedState.on;
							else if (normalizedData.m_statusData.DRSAvailable != 0)
								ledState = LedState.blink;
						}

						return ledState;
					})
			};

		private BlinkState m_shiftPointBlinkState;
		private readonly Stopwatch m_shiftPointStopwatch = new Stopwatch();

		private BlinkState m_pitLaneRevLightsBlinkState;
		private readonly Stopwatch m_pitLaneRevLightsStopwatch = new Stopwatch();
		private static readonly bool[] pitLaneLeds1 = new bool[(int)Constants.numberOfRevLeds]
			{ true, true, true, true, false, false, false, false, false, true, true, true, true };
		private static readonly bool[] pitLaneLeds2 = new bool[(int)Constants.numberOfRevLeds]
			{ false, false, false, false, true, true, true, true, true, false, false, false, false };

		private readonly NormalizedData m_normalizedData = new NormalizedData();

		/// <summary>Left segment display previous action name.</summary>
		public const String LeftSegmentDisplayPreviousAction = "LeftSegmentDisplayPrevious";

		/// <summary>Left segment display next action name.</summary>
		public const String LeftSegmentDisplayNextAction = "LeftSegmentDisplayNext";

		/// <summary>Right segment display previous action name.</summary>
		public const String RightSegmentDisplayPreviousAction = "RightSegmentDisplayPrevious";

		/// <summary>Right segment display next action name.</summary>
		public const String RightSegmentDisplayNextAction = "RightSegmentDisplayNext";

		/// <summary>Instance of the current plugin manager.</summary>
		public PluginManager PluginManager { get; set; }

		/// <summary>Settings property.</summary>
		public Settings Settings { get => m_settings; }

		/// <summary>Called once after plugins startup. Plugins are rebuilt at game change.</summary>
		/// <param name="pluginManager"></param>
		public void Init(PluginManager pluginManager)
		{
			Logging.Current.InfoFormat("SLI-Pro: initializing plugin in thread {0}", Thread.CurrentThread.ManagedThreadId);

			// Load settings.
			m_settings = this.ReadCommonSettings<Settings>(settingsName, () => new Settings());

			// Watch for some property changes in settings.
			m_settings.PropertyChanged +=
				(object sender, PropertyChangedEventArgs e) =>
				{
					switch (e.PropertyName)
					{
						case nameof(m_settings.LeftSegmentDisplayRotarySwitchIndex):
							if (m_settings.LeftSegmentDisplayRotarySwitchIndex != RotaryDetector.unknownIndex)
							{
								// Changed back to rotary switch controlled, so get the last position from the SLI-Pro and set that.
								m_settings.LeftSegmentDisplayIndex =
									m_sliPro.GetRotarySwitchPosition(m_settings.LeftSegmentDisplayRotarySwitchIndex);
							}
							break;

						case nameof(m_settings.RightSegmentDisplayRotarySwitchIndex):
							if (m_settings.RightSegmentDisplayRotarySwitchIndex != RotaryDetector.unknownIndex)
							{
								// Changed back to rotary switch controlled, so get the last position from the SLI-Pro and set that.
								m_settings.RightSegmentDisplayIndex =
									m_sliPro.GetRotarySwitchPosition(m_settings.RightSegmentDisplayRotarySwitchIndex);
							}
							break;

						case nameof(m_settings.LeftSegmentDisplayIndex):
							// Left segment display has changed, by rotary, button or UI.
							m_segmentDisplayManagers[SegmentDisplayPosition.left].SetByIndex(m_settings.LeftSegmentDisplayIndex,
								m_settings.SegmentNameTimeoutMs);
							break;

						case nameof(m_settings.RightSegmentDisplayIndex):
							// Right segment display has changed, by rotary, button or UI.
							m_segmentDisplayManagers[SegmentDisplayPosition.right].SetByIndex(m_settings.RightSegmentDisplayIndex,
								m_settings.SegmentNameTimeoutMs);
							break;

						default:
							break;
					}
				};

			// I'm assuming Logging.Current changes when the log file rolls over, so pass a function to access it.
			m_sliPro = new SliPro.SliPro(m_settings.SliProSettings, () => Logging.Current, OnRotarySwitchChange);

			// Set initial left/right segment displays.
			// HACK I don't like this. Settings reading is a bit racey; can't get notification of change due to the read
			// as PropertyChanged not yet set!
			m_segmentDisplayManagers[SegmentDisplayPosition.left].SetByIndex(m_settings.LeftSegmentDisplayIndex,
				m_settings.SegmentNameTimeoutMs);

			m_segmentDisplayManagers[SegmentDisplayPosition.right].SetByIndex(m_settings.RightSegmentDisplayIndex,
				m_settings.SegmentNameTimeoutMs);

			int CycleSegmentDisplayAction(SegmentDisplayPosition segmentDisplayPosition, bool isNext)
			{
				var segmentDisplayManager = m_segmentDisplayManagers[segmentDisplayPosition];
				return isNext ? segmentDisplayManager.GetNextIndex() : segmentDisplayManager.GetPreviousIndex();
			}

			void CycleLeftSegmentDisplayAction(bool isNext)
			{
				// Only allow button control if not using rotary.
				if (m_settings.LeftSegmentDisplayRotarySwitchIndex == RotaryDetector.unknownIndex)
					m_settings.LeftSegmentDisplayIndex = CycleSegmentDisplayAction(SegmentDisplayPosition.left, isNext);
			}

			void CycleRightSegmentDisplayAction(bool isNext)
			{
				// Only allow button control if not using rotary.
				if (m_settings.RightSegmentDisplayRotarySwitchIndex == RotaryDetector.unknownIndex)
					m_settings.RightSegmentDisplayIndex = CycleSegmentDisplayAction(SegmentDisplayPosition.right, isNext);
			}

			// Segment display control actions.
			pluginManager.AddAction(LeftSegmentDisplayPreviousAction, GetType(),
				(pluginManager2, buttonName) => CycleLeftSegmentDisplayAction(false));

			pluginManager.AddAction(LeftSegmentDisplayNextAction, GetType(),
				(pluginManager2, buttonName) => CycleLeftSegmentDisplayAction(true));

			pluginManager.AddAction(RightSegmentDisplayPreviousAction, GetType(),
				(pluginManager2, buttonName) => CycleRightSegmentDisplayAction(false));

			pluginManager.AddAction(RightSegmentDisplayNextAction, GetType(),
				(pluginManager2, buttonName) => CycleRightSegmentDisplayAction(true));

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
			return m_segmentDisplayManagers[segmentDisplayPosition].NameList;
		}

		/// <summary>Detect a rotary switch.</summary>
		/// <returns>A task that completes with the discovered rotary switch index.</returns>
		public Task<int> DetectRotary()
		{
			return m_sliPro.DetectRotary();
		}

		/// <summary>
		/// Called one time per game data update, contains all normalized game data,
		/// raw data are intentionally "hidden" under a generic object type (A plugin SHOULD NOT USE IT).
		///
		/// This method is on the critical path, it must execute as fast as possible and avoid throwing any error.
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

					// Status LEDs.
					for (uint i = 0; i < m_statusLeds.Length; ++i)
					{
						m_sliPro.SetStatusLed(i,
							m_statusLeds[i].ProcessData(m_normalizedData, m_settings.StatusLedBlinkIntervalMs));
					}

					// Gear and revs.
					m_sliPro.SetGear(m_normalizedData.m_statusData.Gear);
					if (!HandlePitLane())
						HandleRpms();

					// Left/right segments displays.
					foreach (var segmentDisplayManager in m_segmentDisplayManagers.Values)
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
			m_normalizedData.m_isInPit = (statusData.IsInPitLane != 0) || (statusData.IsInPit != 0);

			m_normalizedData.m_deltaToSessionBest = statusData.DeltaToSessionBest ??
				(double?)pluginManager.GetPropertyValue("PersistantTrackerPlugin.SessionBestLiveDeltaSeconds");

			m_normalizedData.m_deltaToAllTimeBest = statusData.DeltaToAllTimeBest ??
				(double?)pluginManager.GetPropertyValue("PersistantTrackerPlugin.AllTimeBestLiveDeltaSeconds");

			m_normalizedData.m_fuelRemainingLaps = statusData.EstimatedFuelRemaingLaps ??
				(double?)pluginManager.GetPropertyValue("DataCorePlugin.Computed.Fuel_RemainingLaps");
		}

		private void OnRotarySwitchChange(int rotarySwitchIndex, int previousPosition, int newPosition)
		{
			Logging.Current.InfoFormat("SLI-Pro: rotary switch {0} was {1}, now {2}", rotarySwitchIndex, previousPosition,
				newPosition);

			if (rotarySwitchIndex == m_settings.LeftSegmentDisplayRotarySwitchIndex)
			{
				m_settings.LeftSegmentDisplayIndex =
					m_segmentDisplayManagers[SegmentDisplayPosition.left].ValidateIndex(newPosition);
			}
			else if (rotarySwitchIndex == m_settings.RightSegmentDisplayRotarySwitchIndex)
			{
				m_settings.RightSegmentDisplayIndex =
					m_segmentDisplayManagers[SegmentDisplayPosition.right].ValidateIndex(newPosition);
			}
			else if (rotarySwitchIndex == m_settings.SliProSettings.BrightnessRotarySwitchIndex)
			{
				m_settings.SliProSettings.BrightnessLevel = SliPro.SliPro.GetBrightnessLevelFromRotaryPosition(newPosition,
					m_settings.SliProSettings.NumberOfBrightnessRotaryPositions);
			}
		}

		private bool HandlePitLane()
		{
			if (0 == m_settings.PitLaneAnimationSpeedMs)
				return false;

			if (!m_normalizedData.m_isInPit)
			{
				m_pitLaneRevLightsStopwatch.Stop();
				return false;
			}
			else if (!m_pitLaneRevLightsStopwatch.IsRunning)
			{
				m_pitLaneRevLightsStopwatch.Restart();
				m_pitLaneRevLightsBlinkState = BlinkState.on;
			}

			switch (m_pitLaneRevLightsBlinkState)
			{
				case BlinkState.on:
					if (m_pitLaneRevLightsStopwatch.ElapsedMilliseconds >= m_settings.PitLaneAnimationSpeedMs)
					{
						m_pitLaneRevLightsBlinkState = BlinkState.off;
						m_pitLaneRevLightsStopwatch.Restart();
					}
					break;

				case BlinkState.off:
					if (m_pitLaneRevLightsStopwatch.ElapsedMilliseconds >= m_settings.PitLaneAnimationSpeedMs)
					{
						m_pitLaneRevLightsBlinkState = BlinkState.on;
						m_pitLaneRevLightsStopwatch.Restart();
					}
					break;
			}

			m_sliPro.SetRevLeds((BlinkState.on == m_pitLaneRevLightsBlinkState) ? pitLaneLeds1 : pitLaneLeds2);
			return true;
		}

		private void HandleRpms()
		{
			// No shift indicators if in top gear.
			int gear;
			bool isTopGear = int.TryParse(m_normalizedData.m_statusData.Gear, out gear) &&
				(gear >= m_normalizedData.m_statusData.CarSettings_MaxGears);

			double rpmPercent = m_normalizedData.m_statusData.CarSettings_CurrentDisplayedRPMPercent;
			bool isShiftPointActive = !isTopGear && (0 != m_normalizedData.m_statusData.CarSettings_RPMRedLineReached);

			if (!isShiftPointActive)
			{
				m_shiftPointStopwatch.Stop();
			}
			else
			{
				if (!m_shiftPointStopwatch.IsRunning)
				{
					m_shiftPointStopwatch.Restart();
					m_shiftPointBlinkState = BlinkState.on;
				}

				switch (m_shiftPointBlinkState)
				{
					case BlinkState.on:
						if ((m_shiftPointStopwatch.ElapsedMilliseconds >= m_settings.ShiftPointBlinkOnSpeedMs) &&
							(m_settings.ShiftPointBlinkOffSpeedMs > 0))
						{
							m_shiftPointBlinkState = BlinkState.off;
							m_shiftPointStopwatch.Restart();
						}
						break;

					case BlinkState.off:
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
						break;
				}
			}

			double minRpmPercent = (100 * m_normalizedData.m_statusData.CarSettings_MinimumShownRPM) /
				m_normalizedData.m_statusData.CarSettings_MaxRPM;

			m_sliPro.SetRevLeds(minRpmPercent, rpmPercent, 0, SliPro.Constants.numberOfRevLeds);
		}
	}
}
