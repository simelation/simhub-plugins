/*
 * Various constant values for the SLI-F1.
 */

using System.Windows.Media;

// ---------------------------------------------------------------------------------------------------------------------------------

namespace SimElation.SliDevices.F1
{
	/// <summary>Various constant values for the SLI-F1.</summary>
	public sealed class Constants : IConstants
	{
		/// <summary>Various compile-time constant values for the SLI-F1.</summary>
		public static class CompileTime
		{
			/// <summary>Product id for an SLI-F1.</summary>
			public const int productId = 0x1110;

			/// <summary>The number of characters in each segment display.</summary>
			public const uint segmentDisplayWidth = 4;

			/// <summary>The number of LEDs in the rev display.</summary>
			public const uint numberOfRevLeds = 15;

			/// <summary>The number of status LEDs (3 left, 3 right).</summary>
			public const uint numberOfStatusLeds = 6;

			/// <summary>The number of external LEDs.</summary>
			public const uint numberOfExternalLeds = 5;

			/// <summary>The number of supported rotary switches.</summary>
			/// <remarks>
			/// The 8th rotary is mapped to controller buttons. Looks like it's at offset 17 in the report but we don't need it.
			/// </remarks>
			public const uint maxNumberOfRotarySwitches = 7;

			/// <summary>The number of supported potentiometers.</summary>
			public const uint maxNumberOfPots = 1;
		}

		/// <inheritdoc/>
		public uint SegmentDisplayWidth { get => CompileTime.segmentDisplayWidth; }

		/// <inheritdoc/>
		public Color[] RevLedColors { get; } =
			new Color[]
			{
				// 15 for the SLI-F1.
				Colors.LimeGreen,
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
				Colors.Blue,
				Colors.Blue
			};

		/// <inheritdoc/>
		public Color[] LeftStatusLedColors { get; } =
			new Color[]
			{
				Colors.Yellow,
				Colors.Blue,
				Colors.Red
			};

		/// <inheritdoc/>
		public Color[] RightStatusLedColors { get; } =
			new Color[]
			{
				Colors.Yellow,
				Colors.Blue,
				Colors.Red
			};

		/// <inheritdoc/>
		public uint NumberOfStatusLeds { get => CompileTime.numberOfStatusLeds; }

		/// <inheritdoc/>
		public uint NumberOfExternalLeds { get => CompileTime.numberOfExternalLeds; }

		/// <summary>The number of supported rotary switches.</summary>
		/// <remarks>
		/// The 8th rotary is mapped to controller buttons. Looks like it's at offset 17 in the report but we don't need it.
		/// </remarks>
		public uint MaxNumberOfRotarySwitches { get => CompileTime.maxNumberOfRotarySwitches; }

		/// <inheritdoc/>
		public uint MaxNumberOfPots { get => CompileTime.maxNumberOfPots; }
	}
}
