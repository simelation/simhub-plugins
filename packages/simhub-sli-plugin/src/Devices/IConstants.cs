/*
 * Interface for various device-specific constant values.
 */

using System.Windows.Media;

// ---------------------------------------------------------------------------------------------------------------------------------

namespace SimElation.SliDevices
{
	/// <summary>Interface for various device-specific constant values.</summary>
	public interface IConstants
	{
		/// <summary>The number of characters in each segment display.</summary>
		uint SegmentDisplayWidth { get; }

		/// <summary>Colors of the rev LED array. Length of this is therefore the number of rev LEDs.</summary>
		Color[] RevLedColors { get; }

		/// <summary>The number of status LEDs (3 left, 3 right).</summary>
		uint NumberOfStatusLeds { get; }

		/// <summary>The number of external LEDs.</summary>
		uint NumberOfExternalLeds { get; }

		/// <summary>The number of supported rotary switches.</summary>
		uint MaxNumberOfRotarySwitches { get; }

		/// <summary>The number of supported potentiometers.</summary>
		uint MaxNumberOfPots { get; }
	}
}
