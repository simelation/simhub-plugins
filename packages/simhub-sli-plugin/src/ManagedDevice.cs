/*
 * An SLI device managed by the plugin.
 */

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using GameReaderCommon;
using SimElation.SliDevices;
using SimHub;
using SimHub.Plugins;

// ---------------------------------------------------------------------------------------------------------------------------------

namespace SimElation.Simhub.SliPlugin
{
	/// <summary>An SLI device managed by the plugin.</summary>
	public sealed class ManagedDevice : IDisposable, INotifyPropertyChanged
	{
		/// <summary>Info about the device (serial number, pretty string to display in UI, etc.).</summary>
		public DeviceInfo DeviceInfo { get => m_deviceInfo; }

		/// <summary>A device status string for the UI.</summary>
		public String Status { get => m_device.Status; }

		/// <summary>Is the device available (plugged in)?</summary>
		public bool IsAvailable { get => m_device.IsAvailable; }

		/// <inheritdoc/>
		public event PropertyChangedEventHandler PropertyChanged;

		/// <summary>Constructor.</summary>
		public ManagedDevice(SliPlugin sliPlugin, DeviceInstance deviceInstance)
		{
			m_sliPlugin = sliPlugin;
			m_settings = deviceInstance.DeviceSettings;
			m_deviceInfo = deviceInstance.DeviceInfo;
			m_device = new Device(DeviceDescriptors.Instance.Dictionary[m_deviceInfo.ProductId], m_deviceInfo, m_settings,
				() => Logging.Current, OnRotarySwitchChange);

			// Prefix for all actions; used to clear them when we are disposed.
			m_actionNamePrefix = deviceInstance.MakeActionName("");

			// Segment displays.
			var outputFormatters =
				SliPluginDeviceDescriptors.Instance.Dictionary[deviceInstance.DeviceInfo.ProductId].OutputFormatters;
			m_segmentDisplayManagers =
				new Dictionary<Device.SegmentDisplayPosition, SegmentDisplayManager>()
				{
					// Left segment displays.
					{
						Device.SegmentDisplayPosition.left,
						new SegmentDisplayManager(sliPlugin.PluginManager, deviceInstance, Device.SegmentDisplayPosition.left,
							outputFormatters,
							new SegmentDisplay[]
							{
								new LapsCounterSegmentDisplay(),
								new LapsToGoSegmentDisplay(),
								new PositionSegmentDisplay(),
								new FuelSegmentDisplay(),
								new BrakeBiasSegmentDisplay(outputFormatters.BrakeBiasShortName()),
								new TempSegmentDisplay("Oil", "Oil temperature", outputFormatters.OilTemperaturePrefix(),
									(NormalizedData normalizedData) => normalizedData.StatusData.OilTemperature),
								new TempSegmentDisplay("H20", "Water temperature", outputFormatters.WaterTemperaturePrefix(),
									(NormalizedData normalizedData) => normalizedData.StatusData.WaterTemperature),
								new SpeedSegmentDisplay("Spd"),
								new RpmSegmentDisplay("rPM")
							})
					},

					// Right segment displays.
					{
						Device.SegmentDisplayPosition.right,
						new SegmentDisplayManager(sliPlugin.PluginManager, deviceInstance, Device.SegmentDisplayPosition.right,
							outputFormatters,
							new SegmentDisplay[]
							{
								new LapTimeSegmentDisplay("Currnt", "Current laptime",
									(NormalizedData normalizedData) => normalizedData.StatusData.CurrentLapTime),
								new LapTimeSegmentDisplay("Last", "Last laptime",
									(NormalizedData normalizedData) => normalizedData.StatusData.LastLapTime),
								new LapTimeSegmentDisplay("BstSes", "Session best laptime",
									(NormalizedData normalizedData) => normalizedData.StatusData.BestLapTime),
								new LapTimeSegmentDisplay("BstAll", "All-time best laptime",
									(NormalizedData normalizedData) => normalizedData.StatusData.AllTimeBest),
								new DeltaSegmentDisplay("DltSeS", "Delta to session best laptime",
									(NormalizedData normalizedData) => normalizedData.DeltaToSessionBest),
								new DeltaSegmentDisplay("DltAll", "Delta to all-time best laptime",
									(NormalizedData normalizedData) => normalizedData.DeltaToAllTimeBest),
								new DeltaSegmentDisplay("GapAhd", "Gap to car ahead",
									(NormalizedData normalizedData) =>
									{
										var opponentList = normalizedData.StatusData.OpponentsAheadOnTrackPlayerClass;
										return NextCarDelta(opponentList);
									}),
								new DeltaSegmentDisplay("GapBhd", "Gap to car behind",
									(NormalizedData normalizedData) =>
									{
										var opponentList = normalizedData.StatusData.OpponentsBehindOnTrackPlayerClass;
										return NextCarDelta(opponentList);
									}),
								new SpeedSegmentDisplay("Spd"),
								new RpmSegmentDisplay("rPM")
							})
					}
				};

			// Watch for some property changes in settings.
			// TODO something better. https://github.com/StephenCleary/CalculatedProperties maybe? 
			m_settings.PropertyChanged +=
				(object sender, PropertyChangedEventArgs e) =>
				{
					switch (e.PropertyName)
					{
						case nameof(m_settings.LeftSegmentDisplayRotarySwitchIndex):
							if (m_settings.LeftSegmentDisplayRotarySwitchIndex != RotarySwitchDetector.unknownIndex)
							{
								// Changed back to rotary switch controlled, so get the last position from the SLI and set that.
								m_settings.LeftSegmentDisplayIndex =
									m_device.GetRotarySwitchPosition(m_settings.LeftSegmentDisplayRotarySwitchIndex);
							}
							break;

						case nameof(m_settings.RightSegmentDisplayRotarySwitchIndex):
							if (m_settings.RightSegmentDisplayRotarySwitchIndex != RotarySwitchDetector.unknownIndex)
							{
								// Changed back to rotary switch controlled, so get the last position from the SLI and set that.
								m_settings.RightSegmentDisplayIndex =
									m_device.GetRotarySwitchPosition(m_settings.RightSegmentDisplayRotarySwitchIndex);
							}
							break;

						case nameof(m_settings.LeftSegmentDisplayIndex):
							// Left segment display has changed, by rotary, button or UI.
							m_segmentDisplayManagers[Device.SegmentDisplayPosition.left].SetByIndex(
								m_settings.LeftSegmentDisplayIndex, m_settings.SegmentNameTimeoutMs);
							break;

						case nameof(m_settings.RightSegmentDisplayIndex):
							// Right segment display has changed, by rotary, button or UI.
							m_segmentDisplayManagers[Device.SegmentDisplayPosition.right].SetByIndex(
								m_settings.RightSegmentDisplayIndex, m_settings.SegmentNameTimeoutMs);
							break;

						default:
							break;
					}
				};

			// Set initial left/right segment displays.
			// HACK I don't like this. Settings reading is a bit racey; can't get notification of change due to the read
			// as PropertyChanged not yet set!
			m_segmentDisplayManagers[Device.SegmentDisplayPosition.left].SetByIndex(m_settings.LeftSegmentDisplayIndex,
				m_settings.SegmentNameTimeoutMs);

			m_segmentDisplayManagers[Device.SegmentDisplayPosition.right].SetByIndex(m_settings.RightSegmentDisplayIndex,
				m_settings.SegmentNameTimeoutMs);

			// Propogate some child properties.
			m_device.PropertyChanged +=
				(object sender, PropertyChangedEventArgs e) =>
				{
					switch (e.PropertyName)
					{
						case nameof(m_device.Status):
							OnPropertyChanged(nameof(Status));
							break;

						case nameof(m_device.IsAvailable):
							OnPropertyChanged(nameof(IsAvailable));
							break;

						default:
							break;
					}
				};

			int CycleSegmentDisplayAction(Device.SegmentDisplayPosition segmentDisplayPosition, bool isNext)
			{
				var segmentDisplayManager = m_segmentDisplayManagers[segmentDisplayPosition];
				return isNext ? segmentDisplayManager.GetNextIndex() : segmentDisplayManager.GetPreviousIndex();
			}

			void CycleLeftSegmentDisplayAction(bool isNext)
			{
				// Only allow button control if not using rotary.
				if (m_settings.LeftSegmentDisplayRotarySwitchIndex == RotarySwitchDetector.unknownIndex)
					m_settings.LeftSegmentDisplayIndex = CycleSegmentDisplayAction(Device.SegmentDisplayPosition.left, isNext);
			}

			void CycleRightSegmentDisplayAction(bool isNext)
			{
				// Only allow button control if not using rotary.
				if (m_settings.RightSegmentDisplayRotarySwitchIndex == RotarySwitchDetector.unknownIndex)
					m_settings.RightSegmentDisplayIndex = CycleSegmentDisplayAction(Device.SegmentDisplayPosition.right, isNext);
			}

			// Segment display control actions. Leaving these globally available; maybe something else will want to fire them.
			sliPlugin.PluginManager.AddAction<SliPlugin>(deviceInstance.LeftSegmentDisplayPreviousActionName,
				(_, __) => CycleLeftSegmentDisplayAction(false));

			sliPlugin.PluginManager.AddAction<SliPlugin>(deviceInstance.LeftSegmentDisplayNextActionName,
				(_, __) => CycleLeftSegmentDisplayAction(true));

			sliPlugin.PluginManager.AddAction<SliPlugin>(deviceInstance.RightSegmentDisplayPreviousActionName,
				(_, __) => CycleRightSegmentDisplayAction(false));

			sliPlugin.PluginManager.AddAction<SliPlugin>(deviceInstance.RightSegmentDisplayNextActionName,
				(_, __) => CycleRightSegmentDisplayAction(true));

			// Segment display peek actions. Note passing true for hidden so these actions aren't available globally.
			sliPlugin.PluginManager.AddAction<SliPlugin>(deviceInstance.LeftSegmentDisplayPeekCurrentActionName,
				(_, __) => m_segmentDisplayManagers[Device.SegmentDisplayPosition.left].PeekName(true),
				(_, __) => m_segmentDisplayManagers[Device.SegmentDisplayPosition.left].PeekName(false),
				true);

			sliPlugin.PluginManager.AddAction<SliPlugin>(deviceInstance.RightSegmentDisplayPeekCurrentActionName,
				(_, __) => m_segmentDisplayManagers[Device.SegmentDisplayPosition.right].PeekName(true),
				(_, __) => m_segmentDisplayManagers[Device.SegmentDisplayPosition.right].PeekName(false),
				true);
		}

		/// <inheritdoc/>
		public void Dispose()
		{
			if (m_isDisposed)
				return;

			// Clear actions for this device.
			m_sliPlugin.PluginManager.ClearActions(m_sliPlugin.GetType(), m_actionNamePrefix);

			m_device.Dispose();

			m_isDisposed = true;
		}

		/// <summary>Initiate a detection for a rotary switch.</summary>
		/// <remarks>
		/// The object will monitor received reports from the device for changes.
		/// </remarks>
		/// <returns>
		/// A waitable task that resolves to the index of a detected rotary or
		/// <see cref="RotarySwitchDetector.unknownIndex"/> if none is detected.
		/// </returns>
		public Task<int> DetectRotary()
		{
			return m_device.DetectRotary();
		}

		/// <summary>Get friendly UI names of all configured segment displays.</summary>
		/// <param name="segmentDisplayPosition">Which segment display to get the list for.</param>
		/// <returns>Array of <see cref="SegmentDisplayManager.SegmentDisplayMode"/>.</returns>
		public SegmentDisplayManager.SegmentDisplayMode[] GetSegmentDisplayNameList(
			Device.SegmentDisplayPosition segmentDisplayPosition)
		{
			return m_segmentDisplayManagers[segmentDisplayPosition].FriendlyNameList;
		}

		/// <summary>Called from <see cref="SliPlugin.DataUpdate"/> when a game is running and not paused.</summary>
		/// <param name="pluginManager"></param>
		/// <param name="normalizedData"></param>
		public void ProcessGameData(PluginManager pluginManager, NormalizedData normalizedData)
		{
			uint i = 0;

			// RPM status LEDs.
			foreach (var led in m_settings.RpmStatusLeds)
			{
				if (i >= m_settings.NumberOfRpmStatusLeds)
					break;

				m_device.SetRevLed(i++, led.ProcessGameData(m_sliPlugin.Interpreter, m_settings.StatusLedBlinkIntervalMs));
			}

			// Left status LEDs.
			i = 0;

			foreach (var led in m_settings.LeftStatusLeds)
			{
				m_device.SetStatusLed(i++, led.ProcessGameData(m_sliPlugin.Interpreter, m_settings.StatusLedBlinkIntervalMs));
			}

			// Right status LEDs.
			foreach (var led in m_settings.RightStatusLeds)
			{
				m_device.SetStatusLed(i++, led.ProcessGameData(m_sliPlugin.Interpreter, m_settings.StatusLedBlinkIntervalMs));
			}

			// External LEDs.
			i = 0;

			foreach (var led in m_settings.ExternalLeds)
			{
				m_device.SetExternalLed(i++, led.ProcessGameData(m_sliPlugin.Interpreter, m_settings.StatusLedBlinkIntervalMs));
			}

			// Gear and revs.
			m_device.SetGear(normalizedData.StatusData.Gear);
			if (!HandlePitLane(normalizedData))
				HandleRpms(normalizedData);

			// Left/right segment displays.
			foreach (var segmentDisplayManager in m_segmentDisplayManagers.Values)
			{
				segmentDisplayManager.ProcessData(pluginManager, normalizedData, m_device);
			}

			m_device.SendLedState();
		}

		/// <summary>Called from <see cref="SliPlugin.DataUpdate"/> when a game is not running or paused.</summary>
		/// <param name="gameData"></param>
		/// <param name="isGameRunning">true if a game is running, but paused.</param>
		public void ProcessPausedState(GameData gameData, bool isGameRunning)
		{
			m_device.ResetLedState();
			m_device.SetTextMessage(isGameRunning ? gameData.GameName : m_settings.WelcomeMessage);
			m_device.SendLedState();
		}

		private static double? NextCarDelta(List<Opponent> opponentList)
		{
			// Semantics of OpponentAtPosition seem unclear, so using list of opponents.
			if ((0 == opponentList.Count) || (opponentList[0].RelativeGapToPlayer == null))
				return null;

			return (double)opponentList[0].RelativeGapToPlayer;
		}

		/// <summary>Set RPM leds for pit lane animation if in pits.</summary>
		/// <param name="normalizedData"></param>
		/// <returns>true if the RPM leds have been set, and <see cref="HandleRpms"/> should not be called.</returns>
		private bool HandlePitLane(NormalizedData normalizedData)
		{
			if (0 == m_settings.PitLaneAnimationSpeedMs)
				return false;

			if (!normalizedData.IsInPit)
			{
				m_pitLaneRevLightsStopwatch.Stop();
				return false;
			}
			else if (!m_pitLaneRevLightsStopwatch.IsRunning)
			{
				m_pitLaneRevLightsStopwatch.Restart();
				m_pitLaneRevLightsBlinkState = Led.BlinkState.on;
			}

			switch (m_pitLaneRevLightsBlinkState)
			{
				case Led.BlinkState.on:
					if (m_pitLaneRevLightsStopwatch.ElapsedMilliseconds >= m_settings.PitLaneAnimationSpeedMs)
					{
						m_pitLaneRevLightsBlinkState = Led.BlinkState.off;
						m_pitLaneRevLightsStopwatch.Restart();
					}
					break;

				case Led.BlinkState.off:
					if (m_pitLaneRevLightsStopwatch.ElapsedMilliseconds >= m_settings.PitLaneAnimationSpeedMs)
					{
						m_pitLaneRevLightsBlinkState = Led.BlinkState.on;
						m_pitLaneRevLightsStopwatch.Restart();
					}
					break;
			}

			// Don't like converting every time but it's only whilst in pits...
			var pitLaneLeds = (Led.BlinkState.on == m_pitLaneRevLightsBlinkState) ?
				m_settings.PitLaneLeds1 : m_settings.PitLaneLeds2;
			m_device.SetRevLeds(Array.ConvertAll(pitLaneLeds, (rpmLed => rpmLed.IsSet)));

			return true;
		}

		private void HandleRpms(NormalizedData normalizedData)
		{
			// No shift indicators if in top gear.
			int gear;
			bool isTopGear = int.TryParse(normalizedData.StatusData.Gear, out gear) &&
				(gear >= normalizedData.StatusData.CarSettings_MaxGears);

			double rpmPercent = normalizedData.StatusData.CarSettings_CurrentDisplayedRPMPercent;
			bool isShiftPointActive = !isTopGear && (0 != normalizedData.StatusData.CarSettings_RPMRedLineReached);

			if (!isShiftPointActive)
			{
				m_shiftPointStopwatch.Stop();
			}
			else
			{
				if (!m_shiftPointStopwatch.IsRunning)
				{
					m_shiftPointStopwatch.Restart();
					m_shiftPointBlinkState = Led.BlinkState.on;
				}

				switch (m_shiftPointBlinkState)
				{
					case Led.BlinkState.on:
						if ((m_shiftPointStopwatch.ElapsedMilliseconds >= m_settings.ShiftPointBlinkOnSpeedMs) &&
							(m_settings.ShiftPointBlinkOffSpeedMs > 0))
						{
							m_shiftPointBlinkState = Led.BlinkState.off;
							m_shiftPointStopwatch.Restart();
						}
						break;

					case Led.BlinkState.off:
						if (m_shiftPointStopwatch.ElapsedMilliseconds >= m_settings.ShiftPointBlinkOffSpeedMs)
						{
							m_shiftPointBlinkState = Led.BlinkState.on;
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

			double minRpmPercent = (100 * normalizedData.StatusData.CarSettings_MinimumShownRPM) /
				normalizedData.StatusData.CarSettings_MaxRPM;
			m_device.SetRevLeds(minRpmPercent, rpmPercent, m_settings.NumberOfRpmStatusLeds);
		}

		private void OnRotarySwitchChange(int rotarySwitchIndex, int previousPosition, int newPosition)
		{
			if (rotarySwitchIndex == m_settings.LeftSegmentDisplayRotarySwitchIndex)
			{
				// Note we don't need to check the position is a valid index; SegmentDisplayManager handles it.
				m_settings.LeftSegmentDisplayIndex = newPosition;
			}
			else if (rotarySwitchIndex == m_settings.RightSegmentDisplayRotarySwitchIndex)
			{
				// Note we don't need to check the position is a valid index; SegmentDisplayManager handles it.
				m_settings.RightSegmentDisplayIndex = newPosition;
			}
			else if (rotarySwitchIndex == m_settings.BrightnessRotarySwitchIndex)
			{
				m_settings.BrightnessLevel = Device.Settings.GetBrightnessLevelFromRotarySwitchPosition(newPosition,
					m_settings.NumberOfBrightnessRotaryPositions);
			}
			else if (-1 != previousPosition)
			{
				// Above check to ignore startup initial read.
				//lock (m_settings.m_lock)
				{
					foreach (var rotarySwitchMapping in m_settings.RotarySwitchMappings)
					{
						if (rotarySwitchMapping.RotarySwitchIndex == rotarySwitchIndex)
						{
							ProcessVJoyMappingRotarySwitchChange(rotarySwitchMapping, newPosition);
						}
					}
				}
			}
		}

		private async void ProcessVJoyMappingRotarySwitchChange(DeviceInstance.Settings.RotarySwitchMapping rotarySwitchMapping,
			int newPosition)
		{
			uint buttonId = (uint)(newPosition) + rotarySwitchMapping.FirstVJoyButtonId;

			if (!await VJoyManager.Instance.PulseButton(rotarySwitchMapping.VJoyDeviceId, buttonId, m_settings.VJoyButtonPulseMs))
			{
				Logging.Current.WarnFormat(
					"{0}: failed to pulse button {1} on vJoy device {2} after rotary switch {3} changed to position {4}",
					Assembly.GetExecutingAssembly().GetName().Name, buttonId, rotarySwitchMapping.VJoyDeviceId,
					RotarySwitchDetector.RotarySwitchIndexToUiValue(rotarySwitchMapping.RotarySwitchIndex), newPosition);
			}
		}

		/// <inheritdoc/>
		private void OnPropertyChanged([CallerMemberName] string name = null)
		{
			PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
		}

		private bool m_isDisposed = false;

		private readonly SliPlugin m_sliPlugin;
		private readonly DeviceInstance.Settings m_settings;
		private readonly DeviceInfo m_deviceInfo;
		private readonly Device m_device;
		private readonly String m_actionNamePrefix;

		// Left/right segment managers.
		private readonly Dictionary<Device.SegmentDisplayPosition, SegmentDisplayManager> m_segmentDisplayManagers;

		private Led.BlinkState m_shiftPointBlinkState;
		private readonly Stopwatch m_shiftPointStopwatch = new Stopwatch();

		private Led.BlinkState m_pitLaneRevLightsBlinkState;
		private readonly Stopwatch m_pitLaneRevLightsStopwatch = new Stopwatch();
	}
}
