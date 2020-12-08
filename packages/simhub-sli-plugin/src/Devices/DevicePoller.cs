/*
 * Poller for SLI devices.
 */

using System;
using System.Collections.Generic;
using System.Text;
using HidLibrary;

// ---------------------------------------------------------------------------------------------------------------------------------

namespace SimElation.SliDevices
{
	/// <summary>Poller for SLI devices.</summary>
	public static class DevicePoller
	{
		/// <summary>Poll for devices. Multiple products of a single vendor are supported in one call.</summary>
		/// <param name="vendorId"></param>
		/// <param name="productIds">List of product ids.</param>
		/// <returns>A dictionary keyed by a device's serial number. The value is <see cref="DeviceInfo"/>.</returns>
		public static Dictionary<String, DeviceInfo> Poll(int vendorId, params int[] productIds)
		{
			var hidDevices = HidDevices.Enumerate(vendorId, productIds);
			var deviceSet = new Dictionary<String, DeviceInfo>();

			foreach (var device in hidDevices)
			{
				var deviceInfo = ProcessDevice(device);
				deviceSet.Add(deviceInfo.SerialNumber, deviceInfo);
			}

			return deviceSet;
		}

		private static DeviceInfo ProcessDevice(HidDevice hidDevice)
		{
			// Format up a nice device info string.
			byte[] data;
			String manufacturer = hidDevice.ReadManufacturer(out data) ? Encoding.Unicode.GetString(data).TrimEnd('\0') : "";
			String product = hidDevice.ReadProduct(out data) ? Encoding.Unicode.GetString(data).TrimEnd('\0') : "";
			String serialNumber = hidDevice.ReadSerialNumber(out data) ? Encoding.Unicode.GetString(data).TrimEnd('\0') : "";

			// If for some reason serial number isn't returned, fake one. This is obviously no good for saving state
			// but at least will allow things to work at run time.
			if (serialNumber.Length == 0)
				serialNumber = Guid.NewGuid().ToString();

			String prettyInfo = String.Format("{0}{1}{2}{3}{4}{5}{6}",
				manufacturer, (manufacturer.Length > 0) ? " " : "",
				product, (product.Length > 0) ? " " : "",
				(serialNumber.Length > 0) ? "(" : "",
				serialNumber,
				(serialNumber.Length > 0) ? ")" : "").Trim();
			if (prettyInfo.Length == 0)
				prettyInfo = hidDevice.Description;

			return new DeviceInfo(hidDevice, hidDevice.Attributes.ProductId, serialNumber, prettyInfo);
		}
	}
}
