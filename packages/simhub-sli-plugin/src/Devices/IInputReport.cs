/*
 * Input report from device.
 */

namespace SimElation.SliDevices
{
	/// <summary>Input report format from a device.</summary>.
	public interface IInputReport
	{
		/// <summary>Offset in input report to the rotary switch data.</summary>
		uint RotarySwitchesOffset { get; }

		/// <summary>Number of bytes in the input report containing rotary switch data.</summary>
		uint RotarySwitchesLength { get; }
	}
}
