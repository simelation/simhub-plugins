/*
 * SimHub SLI plugin.
 */

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Data;
using GameReaderCommon;
using SimHub.Plugins;
using SimHub.Plugins.OutputPlugins.Dash.TemplatingCommon;
using Logging = SimHub.Logging;

// ---------------------------------------------------------------------------------------------------------------------------------

namespace SimElation.Simhub.SliPlugin
{
	/// <summary>SimHub SLI plugin.</summary>
	/// <remarks>
	/// Note the name of the class seems to form the base name for the settings file. A separate attribute would be nicer!
	/// </remarks>
	[PluginDescription("SimElation SimHub Plugin for Leo Bodnar SLI-Pro and SLI-F1")]
	[PluginAuthor("SimElation")]
	[PluginName("SLI Plugin")]
	public sealed class SliPlugin : IPlugin, IDataPlugin, IWPFSettings
	{
		/// <inheritdoc/>
		public PluginManager PluginManager { get; set; }

		/// <summary>Set of devices either being managed or plugged in but unmanaged.</summary>
		public ObservableCollection<DeviceInstance> DeviceInstances { get => m_deviceInstances; }

		/// <summary>ncalc interpreter.</summary>
		public NCalcEngineBase Interpreter { get; } = new NCalcEngineBase();

		/// <summary>Called once after plugins startup. Plugins are rebuilt at game change.</summary>
		/// <param name="pluginManager"></param>
		public void Init(PluginManager pluginManager)
		{
			Logging.Current.InfoFormat("{0}: initializing plugin in thread {1}", Assembly.GetExecutingAssembly().GetName().Name,
				Thread.CurrentThread.ManagedThreadId);

			BindingOperations.EnableCollectionSynchronization(m_deviceInstances, m_lock);

			// Load settings.
			var serializedSettings = this.ReadCommonSettings<SerializedSettings>(settingsName, () => new SerializedSettings());

			foreach (var serializedDeviceInstance in serializedSettings.DeviceInstances)
			{
				serializedDeviceInstance.DeviceSettings.Fixup(
					SliPluginDeviceDescriptors.Instance.Dictionary[serializedDeviceInstance.DeviceInfo.ProductId]);

				DeviceInstances.Add(
					new DeviceInstance()
					{
						DeviceInfo = serializedDeviceInstance.DeviceInfo,
						DeviceSettings = serializedDeviceInstance.DeviceSettings,
						// Anything in serialized settings was managed.
						IsManaged = true
					});

				// Now we have to wait for the device polling to find this device and create a ManagedDevice for it.
			}

			// Initiate device polling.
			PollForDevices();

			Logging.Current.InfoFormat("{0}: initialization complete", Assembly.GetExecutingAssembly().GetName().Name);
		}

		/// <summary>
		/// Called at plugin manager stop, close/dispose anything needed here. Plugins are rebuilt at game change.
		/// </summary>
		/// <param name="pluginManager"></param>
		public void End(PluginManager pluginManager)
		{
			Logging.Current.InfoFormat("{0}: shutting down plugin in thread {1}", Assembly.GetExecutingAssembly().GetName().Name,
				Thread.CurrentThread.ManagedThreadId);

			// Cancel device polling.
			if (m_devicePollTask != null)
			{
				m_devicePollTaskCancellation.Cancel();
				// Not disposing of m_devicePollTask as possibly we should await it after requesting cancellation,
				// but can't in Dispose().
				// TODO what is the correct thing to do?
				m_devicePollTask = null;
			}

			// Build serialized form of settings.
			var serialzedSettings = new SerializedSettings();

			// Dispose any managed devices and save settings for serialization.
			foreach (var deviceInstance in DeviceInstances)
			{
				if (deviceInstance.ManagedDevice != null)
					deviceInstance.ManagedDevice.Dispose();

				if (deviceInstance.IsManaged)
				{
					serialzedSettings.DeviceInstances.Add(
						new SerializedDeviceInstance()
						{
							DeviceInfo = deviceInstance.DeviceInfo,
							DeviceSettings = deviceInstance.DeviceSettings
						});
				}
			}

			// Save settings.
			this.SaveCommonSettings(settingsName, serialzedSettings);

			Logging.Current.InfoFormat("{0}: plugin shut down", Assembly.GetExecutingAssembly().GetName().Name);
		}

		/// <inheritdoc/>
		public System.Windows.Controls.Control GetWPFSettingsControl(PluginManager pluginManager)
		{
			return new SliPluginControl(this);
		}

		/// <summary>Start managing a device. Called when the UI manage setting is toggled.</summary>
		/// <param name="deviceInstance"></param>
		public void AddManagedDevice(DeviceInstance deviceInstance)
		{
			// Sanity checking.
			if (-1 == DeviceInstances.IndexOf(deviceInstance))
			{
				Logging.Current.WarnFormat("{0}: {1} can't find device {2}", Assembly.GetExecutingAssembly().GetName().Name,
					nameof(AddManagedDevice), deviceInstance.DeviceInfo.SerialNumber);
				return;
			}

			if (deviceInstance.IsManaged)
			{
				Logging.Current.WarnFormat("{0}: {1} device {2} is already managed", Assembly.GetExecutingAssembly().GetName().Name,
					nameof(AddManagedDevice), deviceInstance.DeviceInfo.SerialNumber);
				return;
			}

			deviceInstance.IsManaged = true;
			CreateManagedDevice(deviceInstance);
		}

		/// <summary>Stop managing a device. Called when the UI manage setting is toggled.</summary>
		/// <remarks>If the device is still plugged in, it will remain in the UI such that it can be managed again.</remarks>
		/// <param name="deviceInstance"></param>
		public void RemoveManagedDevice(DeviceInstance deviceInstance)
		{
			// Sanity checking.
			if (-1 == DeviceInstances.IndexOf(deviceInstance))
			{
				Logging.Current.WarnFormat("{0}: {1} can't find device {2}", Assembly.GetExecutingAssembly().GetName().Name,
					nameof(AddManagedDevice), deviceInstance.DeviceInfo.SerialNumber);
				return;
			}

			if (!deviceInstance.IsManaged)
			{
				Logging.Current.WarnFormat("{0}: {1} device {2} is already unmanaged",
					Assembly.GetExecutingAssembly().GetName().Name, nameof(AddManagedDevice),
					deviceInstance.DeviceInfo.SerialNumber);
				return;
			}

			if (deviceInstance.ManagedDevice != null)
			{
				deviceInstance.ManagedDevice.Dispose();
				deviceInstance.ManagedDevice = null;
			}

			deviceInstance.IsManaged = false;
		}

		/// <summary>
		/// Called one time per game data update, contains all normalized game data,
		/// raw data are intentionally "hidden" under a generic object type (A plugin SHOULD NOT USE IT).
		///
		/// This method is on the critical path, it must execute as fast as possible and avoid throwing any error.
		/// </summary>
		/// <param name="pluginManager"></param>
		/// <param name="gameData"></param>
		public void DataUpdate(PluginManager pluginManager, ref GameData gameData)
		{
			// TODO need locks over iterating Devices.

			if (gameData.GameRunning)
			{
				if (!gameData.GamePaused && (gameData.NewData != null))
				{
					// Fix up a few potentially missing things.
					m_normalizedData.Populate(pluginManager, gameData.NewData);

					foreach (var deviceInstance in DeviceInstances)
					{
						deviceInstance.ManagedDevice?.ProcessGameData(pluginManager, m_normalizedData);
					}
				}
				else
				{
					foreach (var deviceInstance in DeviceInstances)
					{
						deviceInstance.ManagedDevice?.ProcessPausedState(gameData, true);
					}
				}
			}
			else
			{
				foreach (var deviceInstance in DeviceInstances)
				{
					deviceInstance.ManagedDevice?.ProcessPausedState(gameData, false);
				}
			}
		}

		// Polling loop for devices.
		private async void PollForDevices()
		{
			while (true)
			{
				try
				{
					m_devicePollTask = Task.Delay(1000, m_devicePollTaskCancellation.Token);
					await m_devicePollTask;
					m_devicePollTask = null;

					PollForDevicesOnce();
				}
				catch (Exception e)
				{
					Logging.Current.InfoFormat("{0}: exception {1} in {2}", Assembly.GetExecutingAssembly().GetName().Name, e,
						nameof(PollForDevices));
					return;
				}
			}
		}

		// Poll for devices.
		private void PollForDevicesOnce()
		{
			Logging.Current.DebugFormat("{0}: polling for devices...", Assembly.GetExecutingAssembly().GetName().Name);

			var deviceInfoSet = SliDevices.DevicePoller.Poll(vendorId, SliDevices.DeviceDescriptors.Instance.ProductIds);

			// We might modify the DeviceInstances set, so don't walk it directly.
			foreach (var deviceInstance in DeviceInstances.ToList())
			{
				SliDevices.DeviceInfo deviceInfo;
				var isInDeviceSet = deviceInfoSet.TryGetValue(deviceInstance.DeviceInfo.SerialNumber, out deviceInfo);

				if (!deviceInstance.IsManaged)
				{
					// Remove any unmanaged and now unplugged devices from our known set.
					if (!isInDeviceSet)
					{
						Logging.Current.InfoFormat("{0}: unmanaged device {1} was unplugged",
							Assembly.GetExecutingAssembly().GetName().Name, deviceInstance.DeviceInfo.PrettyInfo);

						DeviceInstances.Remove(deviceInstance);
						continue;
					}
				}

				if (isInDeviceSet)
				{
					if (deviceInstance.DeviceInfo.HidDevice == null)
					{
						Logging.Current.InfoFormat("{0}: managed device {1} was plugged in",
							Assembly.GetExecutingAssembly().GetName().Name, deviceInstance.DeviceInfo.PrettyInfo);

						// This was a saved device that's just been plugged back in. Got a handle, can now create ManagedDevice.
						deviceInstance.DeviceInfo.HidDevice = deviceInfo.HidDevice;
						CreateManagedDevice(deviceInstance);
					}

					// No need to process this device below.
					deviceInfoSet.Remove(deviceInstance.DeviceInfo.SerialNumber);
				}
			}

			// Add new devices.
			foreach (var deviceInfo in deviceInfoSet.Values)
			{
				switch (deviceInfo.ProductId)
				{
					case SliDevices.Pro.Constants.CompileTime.productId:
					case SliDevices.F1.Constants.CompileTime.productId:
						Logging.Current.InfoFormat("{0}: unmanaged device {1} was plugged in",
							Assembly.GetExecutingAssembly().GetName().Name, deviceInfo.PrettyInfo);

						AddNewDevice(deviceInfo);
						break;

					default:
						// Shouldn't get here.
						Logging.Current.InfoFormat("{0}: found unknown device with product id {1}",
							Assembly.GetExecutingAssembly().GetName().Name, deviceInfo.ProductId);
						break;
				}
			}
		}

		private void AddNewDevice(SliDevices.DeviceInfo deviceInfo)
		{
			// Add to available set.
			DeviceInstances.Add(
				new DeviceInstance()
				{
					DeviceInfo = deviceInfo,
					DeviceSettings =
						new DeviceInstance.Settings(SliPluginDeviceDescriptors.Instance.Dictionary[deviceInfo.ProductId])
				});
		}

		private void CreateManagedDevice(DeviceInstance deviceInstance)
		{
			if (deviceInstance.ManagedDevice != null)
			{
				Logging.Current.WarnFormat("{0}: {1} device {2} already has a {3}", Assembly.GetExecutingAssembly().GetName().Name,
					nameof(AddManagedDevice), deviceInstance.DeviceInfo.SerialNumber, nameof(ManagedDevice));
				return;
			}

			deviceInstance.ManagedDevice = new ManagedDevice(this, deviceInstance);
		}

		private class SerializedDeviceInstance
		{
			public SliDevices.DeviceInfo DeviceInfo { get; set; }
			public DeviceInstance.Settings DeviceSettings { get; set; }
		}

		private class SerializedSettings
		{
			public List<SerializedDeviceInstance> DeviceInstances { get; set; } = new List<SerializedDeviceInstance>();
		}

		// Bodnar vendor id.
		private const int vendorId = 0x1dd2;

		// Base for settings file name.
		private const String settingsName = "Settings";

		// Set of devices.
		private readonly object m_lock = new object();
		private readonly ObservableCollection<DeviceInstance> m_deviceInstances = new ObservableCollection<DeviceInstance>();

		// Device polling.
		private readonly CancellationTokenSource m_devicePollTaskCancellation = new CancellationTokenSource();
		private Task m_devicePollTask = null;

		// "Fluffed up" data populated on a game update.
		private readonly NormalizedData m_normalizedData = new NormalizedData();
	}
}
