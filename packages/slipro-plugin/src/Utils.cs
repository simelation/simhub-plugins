/*
 * A few text formatting utils.
 */

using System;

// ---------------------------------------------------------------------------------------------------------------------------------

namespace SimElation.SimHubIntegration.SliProPlugin.Utils
{
	/// <summary>String formatting helpers.</summary>
	public static class Formatters
	{
		/// <summary>Format a delta time.</summary>
		/// <remarks>
		/// As "sss.ttt" where sss=seconds, ttt=thousandths.
		/// </remarks>
		/// <param name="delta">The delta time (in seconds).</param>
		/// <param name="str">To receive the formatted string.</param>
		/// <param name="decimalOrPrimeIndexList">To receive the index of decimal point.</param>
		static public void DeltaTime(double delta, out String str, out uint[] decimalOrPrimeIndexList)
		{
			decimalOrPrimeIndexList = new uint[] { 2 };
			str = String.Format("{0,3}{1:000}", (int)delta, Math.Abs((delta * 1000) % 1000));
		}

		/// <summary>Format a lap time.</summary>
		/// <remarks>
		/// As "m:ss.ttt" (t=thousandths) when minutes is less than 10, or "mmm.ss.t" (t=tenths) otherwise.
		/// </remarks>
		/// <param name="timeSpan">The lap time.</param>
		/// <param name="str">To receive the formatted string.</param>
		/// <param name="decimalOrPrimeIndexList">To receive the indexes of decimal points/primes.</param>
		static public void LapTime(TimeSpan timeSpan, out String str, out uint[] decimalOrPrimeIndexList)
		{
			if (timeSpan.Minutes <= 9)
			{
				decimalOrPrimeIndexList = new uint[] { 0, 1, 2 };
				str = String.Format("{0:0}{1:00}{2:000}", timeSpan.Minutes, timeSpan.Seconds, timeSpan.Milliseconds);
			}
			else
			{
				decimalOrPrimeIndexList = new uint[] { 2, 4 };
				str = String.Format("{0:000}{1:00}{2:0}", timeSpan.Minutes, timeSpan.Seconds, timeSpan.Milliseconds / 100);
			}
		}
	}
}
