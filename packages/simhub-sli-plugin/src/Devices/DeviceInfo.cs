/*
 * Info about a device.
 */

using System;
using HidLibrary;
using Newtonsoft.Json;

// ---------------------------------------------------------------------------------------------------------------------------------

namespace SimElation.SliDevices
{
	/// <summary>Info about a device.</summary>
	public sealed class DeviceInfo
	{
		/// <summary>Constructor.</summary>
		/// <param name="hidDevice">Device handle from HidLibrary.</param>
		/// <param name="productId">Device's product id.</param>
		/// <param name="serialNumber">Device's serial number. Used as unique key.</param>
		/// <param name="prettyInfo">Prettied up device info (e.g. for displaying in UI).</param>
		public DeviceInfo(HidDevice hidDevice, int productId, String serialNumber, String prettyInfo)
		{
			HidDevice = hidDevice;
			// NB caching ProductId separately from HidDevice for serializing to config; upon restart the device might not be
			// plugged in so would be no HidDevice.
			// Also Newtonsoft.Json requires the parameter names to match the property names for this to "just work" (ignoring
			// case). Otherwise we could add a default constructor but the properties would need to be read/write.

			ProductId = productId;
			SerialNumber = serialNumber;
			PrettyInfo = prettyInfo;
		}

		/// <summary>Device handle from HidLibrary.</summary>
		[JsonIgnoreAttribute]
		public HidDevice HidDevice { get; set; }

		/// <summary>Device's product id.</summary>
		public int ProductId { get; }

		/// <summary>Device's serial number. Used as unique key.</summary>
		public String SerialNumber { get; }

		/// <summary>Prettied up device info (e.g. for displaying in UI).</summary>
		public String PrettyInfo { get; }
	}
}
