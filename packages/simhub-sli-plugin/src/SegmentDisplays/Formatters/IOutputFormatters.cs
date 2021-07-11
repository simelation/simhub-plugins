/*
 * Output formatters interface.
 */

using System;

// ---------------------------------------------------------------------------------------------------------------------------------

namespace SimElation.Simhub.SliPlugin
{
	/// <summary>Output formatters interface.</summary>
	public interface IOutputFormatters
	{
		/// <summary>Format a lap time.</summary>
		/// <remarks>
		/// As "m:ss.ttt" (t=thousandths) when minutes is less than 10, or "mmm.ss.t" (t=tenths) otherwise.
		/// </remarks>
		/// <param name="timeSpan">The lap time.</param>
		/// <param name="str">To receive the formatted string.</param>
		/// <param name="decimalOrPrimeIndexList">To receive the indexes of decimal points/primes.</param>
		void LapTime(TimeSpan timeSpan, ref String str, ref uint[] decimalOrPrimeIndexList);

		/// <summary>Format a delta time.</summary>
		/// <param name="deltaTime">The delta time (in seconds).</param>
		/// <param name="str">To receive the formatted string.</param>
		/// <param name="decimalOrPrimeIndexList">To receive the index of decimal point.</param>
		void DeltaTime(double deltaTime, ref String str, ref uint[] decimalOrPrimeIndexList);

		/// <summary>Short name for brake bias display.</summary>
		String BrakeBiasShortName();

		/// <summary>Format a brake bias display.</summary>
		/// <param name="frontBias">The brake bias as front percentage.</param>
		/// <param name="str">To receive the formatted string.</param>
		/// <param name="decimalOrPrimeIndexList">To receive the index of decimal point.</param>
		void BrakeBias(double frontBias, ref String str, ref uint[] decimalOrPrimeIndexList);

		/// <summary>Format a fuel display.</summary>
		/// <param name="fuelLevel">Current fuel level.</param>
		/// <param name="remainingLaps">Remaining laps (optional; may not be known).</param>
		/// <param name="str">To receive the formatted string.</param>
		/// <param name="decimalOrPrimeIndexList">To receive the index of decimal point.</param>
		void Fuel(double fuelLevel, double? remainingLaps, ref String str, ref uint[] decimalOrPrimeIndexList);

		/// <summary>Format a lap counter display.</summary>
		/// <param name="totalLaps"></param>
		/// <param name="currentLap"></param>
		/// <param name="completedLaps"></param>
		/// <param name="str">To receive the formatted string.</param>
		/// <param name="decimalOrPrimeIndexList">To receive the index of decimal point.</param>
		void LapCounter(int totalLaps, int currentLap, int completedLaps, ref String str, ref uint[] decimalOrPrimeIndexList);

		/// <summary>Format a laps remaining display.</summary>
		/// <param name="remainingLaps"></param>
		/// <param name="str">To receive the formatted string.</param>
		/// <param name="decimalOrPrimeIndexList">To receive the index of decimal point.</param>
		void LapsToGo(int remainingLaps, ref String str, ref uint[] decimalOrPrimeIndexList);

		/// <summary>Format a current position display.</summary>
		/// <param name="currentPosition"></param>
		/// <param name="numberOfOpponents"></param>
		/// <param name="str">To receive the formatted string.</param>
		/// <param name="decimalOrPrimeIndexList">To receive the index of decimal point.</param>
		void Position(int currentPosition, int numberOfOpponents, ref String str, ref uint[] decimalOrPrimeIndexList);

		/// <summary>Format a temperature display.</summary>
		/// <param name="prefix">String to prefix the display with.</param>
		/// <param name="temperature"></param>
		/// <param name="str">To receive the formatted string.</param>
		/// <param name="decimalOrPrimeIndexList">To receive the index of decimal point.</param>
		void Temperature(String prefix, double temperature, ref String str, ref uint[] decimalOrPrimeIndexList);

		/// <summary>Format a speed.</summary>
		/// <param name="speed"></param>
		/// <param name="str">To receive the formatted string.</param>
		/// <param name="decimalOrPrimeIndexList">To receive the index of decimal point.</param>
		void Speed(double speed, ref String str, ref uint[] decimalOrPrimeIndexList);

		/// <summary>Format an RPM.</summary>
		/// <param name="rpm"></param>
		/// <param name="str">To receive the formatted string.</param>
		/// <param name="decimalOrPrimeIndexList">To receive the index of decimal point.</param>
		void Rpm(double rpm, ref String str, ref uint[] decimalOrPrimeIndexList);

		/// <summary>Prefix for oil temperature (e.g. "o" or "Oil").</summary>
		String OilTemperaturePrefix();

		/// <summary>Prefix for oil temperature (e.g. "H2O" or "w").</summary>
		String WaterTemperaturePrefix();
	}
}
