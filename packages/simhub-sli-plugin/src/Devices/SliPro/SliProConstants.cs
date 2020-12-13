/*
 * Various constant values for the SLI-Pro.
 */

using System.Windows.Media;

// ---------------------------------------------------------------------------------------------------------------------------------

namespace SimElation.SliDevices.Pro
{
	/// <summary>Various constant values for the SLI-Pro.</summary>
	public sealed class Constants : IConstants
	{
		/// <summary>Various compile-time constant values for the SLI-Pro.</summary>
		public static class CompileTime
		{
			/// <summary>Vendor id for an SLI-Pro.</summary>
			public const int vendorId = 0x1dd2;

			/// <summary>Product id for an SLI-Pro.</summary>
			public const int productId = 0x0103;

			/// <summary>The number of characters in each segment display.</summary>
			public const uint segmentDisplayWidth = 6;

			/// <summary>The number of LEDs in the rev display.</summary>
			public const uint numberOfRevLeds = 13;

			/// <summary>The number of status LEDs (3 left, 3 right).</summary>
			public const uint numberOfStatusLeds = 6;

			/// <summary>The number of external LEDs.</summary>
			public const uint numberOfExternalLeds = 5;

			/// <summary>The number of supported rotary switches.</summary>
			public const uint maxNumberOfRotarySwitches = 6;

			/// <summary>The number of supported potentiometers.</summary>
			public const uint maxNumberOfPots = 2;
		}

		/// <inheritdoc/>
		public uint SegmentDisplayWidth { get => CompileTime.segmentDisplayWidth; }

		/// <inheritdoc/>
		public Color[] RevLedColors { get; } =
			new Color[]
			{
				// 13 for the SLI-Pro.
				Colors.LimeGreen,
				Colors.LimeGreen,
				Colors.LimeGreen,
				Colors.LimeGreen,

				Colors.Red,
				Colors.Red,
				Colors.Red,
				Colors.Red,
				Colors.Red,

				Colors.Blue,
				Colors.Blue,
				Colors.Blue,
				Colors.Blue
			};

		/// <inheritdoc/>
		public Color[] LeftStatusLedColors { get; } =
			new Color[]
			{
				Colors.Blue,
				Colors.Yellow,
				Colors.Red
			};

		/// <inheritdoc/>
		public Color[] RightStatusLedColors { get; } =
			new Color[]
			{
				Colors.Red,
				Colors.LimeGreen,
				Colors.Blue
			};

		/// <inheritdoc/>
		public uint NumberOfStatusLeds { get => CompileTime.numberOfStatusLeds; }

		/// <inheritdoc/>
		public uint NumberOfExternalLeds { get => CompileTime.numberOfExternalLeds; }

		/// <inheritdoc/>
		public uint MaxNumberOfRotarySwitches { get => CompileTime.maxNumberOfRotarySwitches; }

		/// <inheritdoc/>
		public uint MaxNumberOfPots { get => CompileTime.maxNumberOfPots; }
	}
}
