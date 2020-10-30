/*
 * SLI-Pro report formats.
 */

// ---------------------------------------------------------------------------------------------------------------------------------

namespace SimElation.SliPro
{
	// Stop complaints about undocumented public members.
#pragma warning disable 1591

	/// <summary>Format of report received from SLI-Pro.</summary>
	public static class InputReport
	{
		public const uint button0to7Offset = 0;
		public const uint button0to7Length = 1;

		public const uint button8to15Offset = button0to7Offset + button0to7Length;
		public const uint button8to15Length = 1;

		public const uint button16to23Offset = button8to15Offset + button8to15Length;
		public const uint button16to23Length = 1;

		public const uint button24to31Offset = button16to23Offset + button16to23Length;
		public const uint button24to31Length = 1;

		public const uint rotarySwitchesOffset = button24to31Offset + button24to31Length;
		public const uint rotarySwitchLength = 6 * 2; // uint16 for each rotary (6 rotaries).

		public const uint potsOffset = rotarySwitchesOffset + rotarySwitchLength;
		public const uint potsLength = 2 * 2; // uint16 for each pot (2 pots).
	}

	/// <summary>Common header for message format to send to SLI-Pro.</summary>
	public static class Header
	{
		public const uint reportTypeIndex = 0;
	}

	/// <summary>Format of LED state message to send to SLI-Pro.</summary>
	public static class LedStateReport
	{
		public const uint reportTypeIndex = Header.reportTypeIndex;
		public const byte reportType = 0x01;

		public const uint gearIndex = Header.reportTypeIndex + 1;

		public const uint revLed1Index = gearIndex + 1;

		public const uint statusLed1Index = revLed1Index + Constants.numberOfRevLeds;
		public const uint statusLedCount = 6;

		public const uint externalLed1Index = statusLed1Index + statusLedCount;
		public const uint externalLedCount = 5;

		public const uint leftSegmentIndex = externalLed1Index + externalLedCount;
		public const uint rightSegmentIndex = leftSegmentIndex + Constants.segmentDisplayWidth;

		public const uint length = rightSegmentIndex + Constants.segmentDisplayWidth;

		// To set . or ' for segment display character.
		public const byte segmentDecimalOrPrimeBit = 0x80;
	}

	/// <summary>Format of brightness message.</summary>
	public static class BrightnessReport
	{
		public const uint reportTypeIndex = Header.reportTypeIndex;
		public const byte reportType = 0x02;

		public const uint brightnessIndex = reportTypeIndex + 1;

		public const uint length = brightnessIndex + 1;
	}

#pragma warning restore 1591
}
