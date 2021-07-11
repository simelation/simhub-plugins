/*
 * Output formatters for SLI-Pro.
 */

using System;

// ---------------------------------------------------------------------------------------------------------------------------------

namespace SimElation.Simhub.SliPlugin
{
	/// <summary>Output formatters for SLI-Pro.</summary>
	public sealed class SliProFormatters : IOutputFormatters
	{
		/// <inheritdoc/>
		public void LapTime(TimeSpan timeSpan, ref String str, ref uint[] decimalOrPrimeIndexList)
		{
			if (timeSpan.TotalMinutes < 10.0)
			{
				decimalOrPrimeIndexList = s_decimalOrPrimeIndexList0123;
				str = String.Format("{0:0}{1:00}{2:000}", (int)timeSpan.TotalMinutes, timeSpan.Seconds, timeSpan.Milliseconds);
			}
			else if (timeSpan.TotalMinutes < 1000.0)
			{
				decimalOrPrimeIndexList = s_decimalOrPrimeIndexList24;
				str = String.Format("{0,3}{1:00}{2:0}", (int)timeSpan.TotalMinutes, timeSpan.Seconds, timeSpan.Milliseconds / 100);
			}
			else
			{
				str = "slo";
			}
		}

		/// <inheritdoc/>
		public void DeltaTime(double deltaTime, ref String str, ref uint[] decimalOrPrimeIndexList)
		{
			const String overflowStr = "-";

			if (deltaTime >= 0.0)
			{
				if (deltaTime < 1.0)
				{
					str = String.Format("   {0:000}", deltaTime * 100.0);
					decimalOrPrimeIndexList = s_decimalOrPrimeIndexList3;
				}
				else if (deltaTime < 10000.0)
				{
					str = String.Format("{0,6:F0}", deltaTime * 100.0);
					decimalOrPrimeIndexList = s_decimalOrPrimeIndexList3;
				}
				else if (deltaTime < 100000.0)
				{
					str = String.Format("{0,6:F0}", deltaTime * 10.0);
					decimalOrPrimeIndexList = s_decimalOrPrimeIndexList4;
				}
				else if (deltaTime <= 1000000.0)
				{
					str = String.Format("{0,6:F0}", deltaTime);
				}
				else
				{
					str = overflowStr;
				}
			}
			else
			{
				if (deltaTime > -1.0)
				{
					str = String.Format("  {0:000}", deltaTime * 100.0);
					decimalOrPrimeIndexList = s_decimalOrPrimeIndexList3;
				}
				else if (deltaTime > -1000.0)
				{
					str = String.Format("{0,6:F0}", deltaTime * 100.0);
					decimalOrPrimeIndexList = s_decimalOrPrimeIndexList3;
				}
				else if (deltaTime > -10000.0)
				{
					str = String.Format("{0,6:F0}", deltaTime * 10.0);
					decimalOrPrimeIndexList = s_decimalOrPrimeIndexList4;
				}
				else if (deltaTime > -100000.0)
				{
					str = String.Format("{0,6:F0}", deltaTime);
				}
				else
				{
					str = overflowStr;
				}
			}
		}

		/// <inheritdoc/>
		public String BrakeBiasShortName()
		{
			return "bbias";
		}

		/// <inheritdoc/>
		public void BrakeBias(double frontBias, ref String str, ref uint[] decimalOrPrimeIndexList)
		{
			str = String.Format("f{0:00}r{1:00}", frontBias, 100 - frontBias);
		}

		/// <inheritdoc/>
		public void Fuel(double fuelLevel, double? remainingLaps, ref String str, ref uint[] decimalOrPrimeIndexList)
		{
			// NB round down both the fuel level and estimated laps.
			str = String.Format("F{0:00}", Math.Min((int)fuelLevel, 99));
			double value = remainingLaps ?? 0.0;

			if (value != 0.0)
				str += String.Format("L{0:00}", Math.Min((int)value, 99));
			else
				str += "   ";
		}

		/// <inheritdoc/>
		public void LapCounter(int totalLaps, int currentLap, int completedLaps, ref String str, ref uint[] decimalOrPrimeIndexList)
		{
			if ((0 != totalLaps) && (currentLap <= 99) && (totalLaps <= 99))
				str = String.Format("L{0,2}-{1,2}", Math.Min(99, currentLap), Math.Min(99, totalLaps));
			else
				str = String.Format("L{0,5}", completedLaps);
		}

		/// <inheritdoc/>
		public void LapsToGo(int remainingLaps, ref String str, ref uint[] decimalOrPrimeIndexList)
		{
			str = String.Format("Lr{0,4}", remainingLaps);
		}

		/// <inheritdoc/>
		public void Position(int currentPosition, int numberOfOpponents, ref String str, ref uint[] decimalOrPrimeIndexList)
		{
			str = String.Format("P{0,2}-{1,2}", Math.Min(99, currentPosition), Math.Min(99, numberOfOpponents));
		}

		/// <inheritdoc/>
		public void Temperature(String prefix, double temperature, ref String str, ref uint[] decimalOrPrimeIndexList)
		{
			str = String.Format("{0}{1,3}", prefix, Math.Round(temperature));
			decimalOrPrimeIndexList = s_decimalOrPrimeIndexList23;
		}

		/// <inheritdoc/>
		public void Speed(double speed, ref String str, ref uint[] decimalOrPrimeIndexList)
		{
			str = String.Format("{0,6:F0}", speed);
		}

		/// <inheritdoc/>
		public void Rpm(double rpm, ref String str, ref uint[] decimalOrPrimeIndexList)
		{
			str = String.Format("{0,6:F0}", rpm);
		}

		/// <inheritdoc/>
		public String OilTemperaturePrefix()
		{
			return "Oil";
		}

		/// <inheritdoc/>
		public String WaterTemperaturePrefix()
		{
			return "H20";
		}

		private readonly static uint[] s_decimalOrPrimeIndexList0123 = { 0, 1, 2 };
		private readonly static uint[] s_decimalOrPrimeIndexList23 = { 2, 3 };
		private readonly static uint[] s_decimalOrPrimeIndexList24 = { 2, 4 };
		private readonly static uint[] s_decimalOrPrimeIndexList3 = { 3 };
		private readonly static uint[] s_decimalOrPrimeIndexList4 = { 4 };
	}
}
