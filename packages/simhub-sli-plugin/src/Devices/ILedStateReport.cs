/*
 * LED state report format for sending to a device.
 */

// ---------------------------------------------------------------------------------------------------------------------------------

namespace SimElation.SliDevices
{
	/// <summary>LED state report format for sending to a device.</summary>
	public interface ILedStateReport
	{
		/// <summary>Offset into the report for the type field.</summary>
		uint ReportTypeOffset { get; }

		/// <summary>Value for the type field.</summary>
		byte ReportType { get; }

		/// <summary>Offset into the report for the gear number field.</summary>
		uint GearOffset { get; }

		/// <summary>Offset into the report for the first rev LED.</summary>
		uint RevLed1Offset { get; }

		/// <summary>Offset into the report for the first status LED.</summary>
		uint StatusLed1Offset { get; }

		/// <summary>Offset into the report for the first external LED.</summary>
		uint ExternalLed1Offset { get; }

		/// <summary>Offset into the report for the first character of the left segment display.</summary>
		uint LeftSegmentDisplayOffset { get; }

		/// <summary>Offset into the report for the first character of the right segment display.</summary>
		uint RightSegmentDisplayOffset { get; }

		/// <summary>The report length.</summary>
		uint Length { get; }

		/// <summary>Value to OR with segment display character to set . or '</summary>
		byte SegmentDisplayDecimalOrPrimeBit { get; }
	}
}
