/*
 * SLI-Pro report formats.
 */

// ---------------------------------------------------------------------------------------------------------------------------------

namespace SimElation.SliDevices.Pro
{
	/// <summary>Input report format from SLI-Pro.</summary>
	public sealed class InputReport : IInputReport
	{
		private static class CompileTime
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
			public const uint rotarySwitchesLength = Constants.CompileTime.maxNumberOfRotarySwitches * sizeof(ushort); // uint16 for each rotary.
			public const uint potsOffset = rotarySwitchesOffset + rotarySwitchesLength;
			public const uint potsLength = Constants.CompileTime.maxNumberOfPots * sizeof(ushort); // uint16 for each pot.
		}

		/// <inheritdoc/>
		public uint RotarySwitchesOffset { get => CompileTime.rotarySwitchesOffset; }

		/// <inheritdoc/>
		public uint RotarySwitchesLength { get => CompileTime.rotarySwitchesLength; }
	}

	/// <summary>Common header for message format to send to SLI-Pro.</summary>
	static class Header
	{
		/// <summary>Offset into report format for the type field.</summary>
		public const uint reportTypeOffset = 0;
	}

	/// <summary>LED state report format for sending to SLI-Pro.</summary>
	public sealed class LedStateReport : ILedStateReport
	{
		private static class CompileTime
		{
			public const uint reportTypeOffset = Header.reportTypeOffset;
			public const byte reportType = 0x01;
			public const uint gearOffset = Header.reportTypeOffset + 1;
			public const uint revLed1Offset = gearOffset + 1;
			public const uint statusLed1Offset = revLed1Offset + Constants.CompileTime.numberOfRevLeds;
			public const uint externalLed1Offset = statusLed1Offset + Constants.CompileTime.numberOfStatusLeds;
			public const uint leftDisplaySegmentOffset = externalLed1Offset + Constants.CompileTime.numberOfExternalLeds;
			public const uint rightDisplaySegmentOffset = leftDisplaySegmentOffset + Constants.CompileTime.segmentDisplayWidth;
			public const uint length = rightDisplaySegmentOffset + Constants.CompileTime.segmentDisplayWidth;
			public const byte segmentDisplayDecimalOrPrimeBit = 0x80;
		}

		/// <inheritdoc/>
		public uint ReportTypeOffset { get => CompileTime.reportTypeOffset; }

		/// <inheritdoc/>
		public byte ReportType { get => CompileTime.reportType; }

		/// <inheritdoc/>
		public uint GearOffset { get => CompileTime.gearOffset; }

		/// <inheritdoc/>
		public uint RevLed1Offset { get => CompileTime.revLed1Offset; }

		/// <inheritdoc/>
		public uint StatusLed1Offset { get => CompileTime.statusLed1Offset; }

		/// <inheritdoc/>
		public uint ExternalLed1Offset { get => CompileTime.externalLed1Offset; }

		/// <inheritdoc/>
		public uint LeftSegmentDisplayOffset { get => CompileTime.leftDisplaySegmentOffset; }

		/// <inheritdoc/>
		public uint RightSegmentDisplayOffset { get => CompileTime.rightDisplaySegmentOffset; }

		/// <inheritdoc/>
		public uint Length { get => CompileTime.length; }

		/// <inheritdoc/>
		public byte SegmentDisplayDecimalOrPrimeBit { get => CompileTime.segmentDisplayDecimalOrPrimeBit; }
	}

	/// <summary>LED brightness report format for sending to SLI-Pro.</summary>
	public sealed class BrightnessReport : IBrightnessReport
	{
		private static class CompileTime
		{
			public const uint reportTypeOffset = Header.reportTypeOffset;
			public const byte reportType = 0x02;
			public const uint brightnessOffset = reportTypeOffset + 1;
			public const uint length = brightnessOffset + 1;
		}

		/// <inheritdoc/>
		public uint ReportTypeOffset { get => CompileTime.reportTypeOffset; }

		/// <inheritdoc/>
		public byte ReportType { get => CompileTime.reportType; }

		/// <inheritdoc/>
		public uint BrightnessOffset { get => CompileTime.brightnessOffset; }

		/// <inheritdoc/>
		public uint Length { get => CompileTime.length; }
	}
}
