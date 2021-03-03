/*
 * vJoy device handling.
 */

using System;
using System.Collections.ObjectModel;
using System.Threading;
using System.Threading.Tasks;
using System.Reflection;
using Logging = SimHub.Logging;

// ---------------------------------------------------------------------------------------------------------------------------------

namespace SimElation
{
	/// <summary>vJoy device manager.</summary>
	public class VJoyManager
	{
		/// <summary>Singleton instance.</summary>
		public static VJoyManager Instance { get => m_instance.Value; }

		/// <summary>Do the vJoy driver and dll versions match sufficiently?</summary>
		public bool IsValidVersion { get => m_isValidVersion; }

		/// <summary>Is the vJoy dll version the known bad one that crashes?</summary>
		/// <remarks>
		/// See http://vjoystick.sourceforge.net/site/index.php/forum/4-Help/1171-vjoy-crashes-development
		/// </remarks>
		public bool IsBadVersion { get => m_isBadVersion; }

		/// <summary>The vJoy dll version.</summary>
		public uint DllVersion { get => m_dllVersion; }

		/// <summary>The vJoy driver version.</summary>
		public uint DriverVersion { get => m_driverVersion; }

		/// <summary>The current list of available vJoy device ids.</summary>
		public ObservableCollection<uint> DeviceIds { get => GetDeviceIds(); }

		/// <summary>Pulse a vJoy device's button.</summary>
		/// <param name="vJoyDeviceId"></param>
		/// <param name="buttonId"></param>
		/// <param name="pulseMs"></param>
		/// <returns>A Task that resolves to a boolean as to whether the press, pause, release was successful.</returns>
		public Task<bool> PulseButton(uint vJoyDeviceId, uint buttonId, int pulseMs)
		{
			return GetDevice(vJoyDeviceId)?.PulseButton(buttonId, pulseMs);
		}

		/// <summary>Get a default device id (when user adds a new mapping from the UI).</summary>
		/// <returns>First vjoy device id that's available, or 1.</returns>
		public uint GetDefaultDeviceId()
		{
			var deviceIds = DeviceIds;

			return (deviceIds.Count == 0) ? firstDeviceId : deviceIds[0];
		}

		/// <summary>Instance of a vJoy device.</summary>
		private class Device
		{
			/// <summary>Constructor.</summary>
			/// <param name="vJoy"></param>
			/// <param name="id">This device's id, indexed from 1.</param>
			public Device(VJoyWrap vJoy, uint id)
			{
				m_vJoy = vJoy;
				m_id = id;
			}

			/// <summary>Pulse a button.</summary>
			/// <param name="buttonId"></param>
			/// <param name="pulseMs"></param>
			/// <returns>A Task that resolves to a boolean as to whether the press, pause, release was successful.</returns>
			public async Task<bool> PulseButton(uint buttonId, int pulseMs)
			{
				bool res = false;

				try
				{
					if (!m_hasAcquired)
					{
						if (m_vJoy.AcquireVJD(m_id))
							m_hasAcquired = true;
						else
							return false;
					}

					// Press button.
					if (m_vJoy.SetBtn(true, m_id, buttonId))
					{
						// Wait then release.
						await Task.Delay(pulseMs);
						m_vJoy.SetBtn(false, m_id, buttonId);

						res = true;
					}
				}
				catch (Exception)
				{
				}

				return res;
			}

			/// <summary>Get the VjdStat state.</summary>
			/// <returns></returns>
			public VjdStat GetState()
			{
				return m_vJoy.GetVJDStatus(m_id);
			}

			private delegate void SetButtonFn(bool isSet);

			private readonly uint m_id;
			private readonly VJoyWrap m_vJoy;
			private bool m_hasAcquired = false;
		}

		private VJoyManager()
		{
			try
			{
				// Note extra level of indirection here. VJoyManager needs to work if vJoyInterfaceWrap isn't available, so wrap
				// the calls we need to make to it in VJoyWrap.cs.
				m_vJoy = new VJoyWrap();
			}
			catch (Exception e)
			{
				m_vJoy = null;
				Logging.Current.InfoFormat("{0}: no vjoy found by thread {1}, {2}", Assembly.GetExecutingAssembly().GetName().Name,
					Thread.CurrentThread.ManagedThreadId, e.Message);
			}

			m_isValidVersion = (m_vJoy != null) && m_vJoy.DriverMatch(ref m_dllVersion, ref m_driverVersion);

			// 0x216 is definitely bad. 0x218 or later seem OK. Can't find 0x217 to test so assume bad.
			m_isBadVersion = m_dllVersion < 0x218;
			m_isAvailable = m_isValidVersion && !m_isBadVersion;

			if (m_isAvailable)
			{
				for (uint i = firstDeviceId; i < m_instances.Length; ++i)
				{
					// NB doesn't appear to be a way to enumerate using HidLibrary and get back to the vJoy device id.
					// Since there's a max of 16, just try them all via the vJoy interface.
					m_instances[i] = new Device(m_vJoy, i);
				}
			}
		}

		private ObservableCollection<uint> GetDeviceIds()
		{
			m_deviceIds.Clear();

			if (m_isAvailable)
			{
				for (uint i = firstDeviceId; i < m_instances.Length; ++i)
				{
					try
					{
						var state = m_instances[i].GetState();

						switch (state)
						{
							case VjdStat.VJD_STAT_OWN:
							case VjdStat.VJD_STAT_FREE:
								m_deviceIds.Add(i);
								break;

							default:
								break;
						}
					}
					catch (Exception)
					{
					}
				}
			}

			return m_deviceIds;
		}

		private Device GetDevice(uint id)
		{
			return (m_isAvailable && (id >= 1) && (id < m_instances.Length)) ? m_instances[id] : null;
		}

		private const uint firstDeviceId = 1;   // NB vJoy API starts device ids at 1.
		private const uint maxDevices = 16;

		private readonly VJoyWrap m_vJoy;
		private uint m_dllVersion = 0;
		private uint m_driverVersion = 0;
		private readonly bool m_isValidVersion;
		private readonly bool m_isBadVersion;
		private readonly bool m_isAvailable;
		private readonly Device[] m_instances = new Device[maxDevices + firstDeviceId];
		private readonly ObservableCollection<uint> m_deviceIds = new ObservableCollection<uint>();

		private static readonly Lazy<VJoyManager> m_instance = new Lazy<VJoyManager>(() => new VJoyManager());
	}
}
