/*
 * SLI-F1 report formats.
 */

// ---------------------------------------------------------------------------------------------------------------------------------

namespace SimElation.SliF1
{
	// Stop complaints about undocumented public members.
#pragma warning disable 1591

	/// <summary>Format of report received from SLI-F1.</summary>
	public static class InputReport
	{
		public const uint rotarySwitchesOffset = 0;
		public const uint rotarySwitchLength = Constants.maxNumberOfRotarySwitches * sizeof(ushort); // uint16 for each rotary.

		public const uint potsOffset = rotarySwitchesOffset + rotarySwitchLength;
		public const uint potsLength = Constants.maxNumberOfPots * sizeof(ushort); // uint16 for each pot.
	}

	/// <summary>Common header for message format to send to SLI-F1.</summary>
	public static class Header
	{
		public const uint reportTypeIndex = 0;
	}

	/// <summary>Format of LED state message to send to SLI-F1.</summary>
	public static class LedStateReport
	{
		public const uint reportTypeIndex = Header.reportTypeIndex;
		public const byte reportType = 0x01;

		public const uint gearIndex = Header.reportTypeIndex + 1;

		public const uint revLed1Index = gearIndex + 1;

		public const uint statusLed1Index = revLed1Index + Constants.numberOfRevLeds;

		public const uint externalLed1Index = statusLed1Index + Constants.numberOfStatusLeds;

		public const uint leftSegmentIndex = externalLed1Index + Constants.numberOfExternalLeds;
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
