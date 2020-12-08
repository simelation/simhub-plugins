/*
 * SLI-F1 report formats.
 */

// ---------------------------------------------------------------------------------------------------------------------------------

namespace SimElation.SliDevices.F1
{
	/// <summary>Input report format from SLI-F1.</summary>
	public sealed class InputReport : IInputReport
	{
		private static class CompileTime
		{
			public const uint rotarySwitchesOffset = 0;
			public const uint rotarySwitchesLength = Constants.CompileTime.maxNumberOfRotarySwitches * sizeof(ushort); // uint16 for each rotary.

			public const uint potsOffset = rotarySwitchesOffset + rotarySwitchesLength;
			public const uint potsLength = Constants.CompileTime.maxNumberOfPots * sizeof(ushort); // uint16 for each pot.
		}

		/// <inheritdoc/>
		public uint RotarySwitchesOffset { get => CompileTime.rotarySwitchesOffset; }

		/// <inheritdoc/>
		public uint RotarySwitchesLength { get => CompileTime.rotarySwitchesLength; }
	}

	/// <summary>Common header for message format to send to SLI-F1.</summary>
	static class Header
	{
		/// <summary>Offset into report format for the type field.</summary>
		public const uint reportTypeIndex = 0;
	}

	/// <summary>LED state report format for sending to SLI-F1.</summary>
	public sealed class LedStateReport : ILedStateReport
	{
		private static class CompileTime
		{
			public const uint reportTypeIndex = Header.reportTypeIndex;
			public const byte reportType = 0x01;
			public const uint gearIndex = Header.reportTypeIndex + 1;
			public const uint revLed1Index = gearIndex + 1;
			public const uint statusLed1Index = revLed1Index + Constants.CompileTime.numberOfRevLeds;
			public const uint externalLed1Index = statusLed1Index + Constants.CompileTime.numberOfStatusLeds;
			public const uint leftDisplaySegmentIndex = externalLed1Index + Constants.CompileTime.numberOfExternalLeds;
			public const uint rightDisplaySegmentIndex = leftDisplaySegmentIndex + Constants.CompileTime.segmentDisplayWidth;
			public const uint length = rightDisplaySegmentIndex + Constants.CompileTime.segmentDisplayWidth;
			public const byte segmentDisplayDecimalOrPrimeBit = 0x80;
		}

		/// <inheritdoc/>
		public uint ReportTypeOffset { get => CompileTime.reportTypeIndex; }

		/// <inheritdoc/>
		public byte ReportType { get => CompileTime.reportType; }

		/// <inheritdoc/>
		public uint GearOffset { get => CompileTime.gearIndex; }

		/// <inheritdoc/>
		public uint RevLed1Offset { get => CompileTime.revLed1Index; }

		/// <inheritdoc/>
		public uint StatusLed1Offset { get => CompileTime.statusLed1Index; }

		/// <inheritdoc/>
		public uint ExternalLed1Offset { get => CompileTime.externalLed1Index; }

		/// <inheritdoc/>
		public uint LeftSegmentDisplayOffset { get => CompileTime.leftDisplaySegmentIndex; }

		/// <inheritdoc/>
		public uint RightSegmentDisplayOffset { get => CompileTime.rightDisplaySegmentIndex; }

		/// <inheritdoc/>
		public uint Length { get => CompileTime.length; }

		/// <inheritdoc/>
		public byte SegmentDisplayDecimalOrPrimeBit { get => CompileTime.segmentDisplayDecimalOrPrimeBit; }
	}

	/// <summary>LED brightness report format for sending to SLI-F1.</summary>
	public sealed class BrightnessReport : IBrightnessReport
	{
		private static class CompileTime
		{
			public const uint reportTypeIndex = Header.reportTypeIndex;
			public const byte reportType = 0x02;
			public const uint brightnessIndex = reportTypeIndex + 1;
			public const uint length = brightnessIndex + 1;
		}

		/// <inheritdoc/>
		public uint ReportTypeOffset { get => CompileTime.reportTypeIndex; }

		/// <inheritdoc/>
		public byte ReportType { get => CompileTime.reportType; }

		/// <inheritdoc/>
		public uint BrightnessOffset { get => CompileTime.brightnessIndex; }

		/// <inheritdoc/>
		public uint Length { get => CompileTime.length; }
	}
}
