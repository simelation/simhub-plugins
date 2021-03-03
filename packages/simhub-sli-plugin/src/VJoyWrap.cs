/*
 * Wrap over vJoyInterfaceWrap itself to make it an optional dependency.
 */

using vJoyInterfaceWrap;

// ---------------------------------------------------------------------------------------------------------------------------------

namespace SimElation
{
	/// <summary>vJoyInterfaceWrap wrap.</summary>
	public class VJoyWrap
	{
		/// <summary />
		public VJoyWrap()
		{
			m_vJoy = new vJoy();
		}

		/// <summary />
		public bool DriverMatch(ref uint dllVersion, ref uint driverVersion)
		{
			return m_vJoy.DriverMatch(ref dllVersion, ref driverVersion);
		}

		/// <summary />
		public VjdStat GetVJDStatus(uint id)
		{
			return m_vJoy.GetVJDStatus(id);
		}

		/// <summary />
		public bool AcquireVJD(uint id)
		{
			return m_vJoy.AcquireVJD(id);
		}

		/// <summary />
		public bool SetBtn(bool isSet, uint id, uint buttonId)
		{
			return m_vJoy.SetBtn(isSet, id, buttonId);
		}

		private readonly vJoy m_vJoy;
	}
}
