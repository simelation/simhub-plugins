/*
 * LED brightness report format for sending to a device.
 */

// ---------------------------------------------------------------------------------------------------------------------------------

namespace SimElation.SliDevices
{
	/// <summary>LED brightness report format for sending to a device.</summary>
	public interface IBrightnessReport
	{
		/// <summary>Offset into the report for the type field.</summary>
		uint ReportTypeOffset { get; }

		/// <summary>Value for the type field.</summary>
		byte ReportType { get; }

		/// <summary>Offset into the report for the brightness field.</summary>
		uint BrightnessOffset { get; }

		/// <summary>The report length.</summary>
		uint Length { get; }
	}
}
