/*
 * Output formatters for SLI-F1.
 */

using System;

// ---------------------------------------------------------------------------------------------------------------------------------

namespace SimElation.Simhub.SliPlugin
{
	/// <summary>Output formatters for SLI-F1.</summary>
	public sealed class SliF1Formatters : IOutputFormatters
	{
		/// <inheritdoc/>
		public void LapTime(TimeSpan timeSpan, ref String str, ref uint[] decimalOrPrimeIndexList)
		{
			if (timeSpan.TotalMinutes < 10.0)
			{
				decimalOrPrimeIndexList = s_decimalOrPrimeIndexList02;
				str = String.Format("{0:0}{1:00}{2:0}", (int)timeSpan.TotalMinutes, timeSpan.Seconds, timeSpan.Milliseconds / 100);
			}
			else if (timeSpan.TotalMinutes < 100.0)
			{
				decimalOrPrimeIndexList = s_decimalOrPrimeIndexList1;
				str = String.Format("{0:00}{1:00}", (int)timeSpan.TotalMinutes, timeSpan.Seconds);
			}
			else
			{
				str = "slow";
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
					str = String.Format(" {0:000}", deltaTime * 100.0);
					decimalOrPrimeIndexList = s_decimalOrPrimeIndexList1;
				}
				else if (deltaTime < 100.0)
				{
					str = String.Format("{0,4:F0}", deltaTime * 100.0);
					decimalOrPrimeIndexList = s_decimalOrPrimeIndexList1;
				}
				else if (deltaTime < 1000.0)
				{
					str = String.Format("{0,4:F0}", deltaTime * 10.0);
					decimalOrPrimeIndexList = s_decimalOrPrimeIndexList2;
				}
				else if (deltaTime <= 10000.0)
				{
					str = String.Format("{0,4:F0}", deltaTime);
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
					str = String.Format("{0:000}", deltaTime * 100.0);
					decimalOrPrimeIndexList = s_decimalOrPrimeIndexList1;
				}
				else if (deltaTime > -10.0)
				{
					str = String.Format("{0,4:F0}", deltaTime * 100.0);
					decimalOrPrimeIndexList = s_decimalOrPrimeIndexList1;
				}
				else if (deltaTime > -100.0)
				{
					str = String.Format("{0,4:F0}", deltaTime * 10.0);
					decimalOrPrimeIndexList = s_decimalOrPrimeIndexList2;
				}
				else if (deltaTime > -1000.0)
				{
					str = String.Format("{0,4:F0}", deltaTime);
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
			return "bias";
		}

		/// <inheritdoc/>
		public void BrakeBias(double frontBias, ref String str, ref uint[] decimalOrPrimeIndexList)
		{
			str = String.Format("{0:00}{1:00}", frontBias, 100 - frontBias);
			decimalOrPrimeIndexList = s_decimalOrPrimeIndexList1;
		}

		/// <inheritdoc/>
		public void Fuel(double fuelLevel, double? remainingLaps, ref String str, ref uint[] decimalOrPrimeIndexList)
		{
			// NB round down both the fuel level and estimated laps.

			str = String.Format("{0:00}", Math.Min((int)fuelLevel, 99));
			double value = remainingLaps ?? 0.0;

			if (value != 0.0)
			{
				str += String.Format("{0:00}", Math.Min((int)value, 99));
				decimalOrPrimeIndexList = s_decimalOrPrimeIndexList1;
			}
			else
			{
				str += "  ";
			}
		}

		/// <inheritdoc/>
		public void LapCounter(int totalLaps, int currentLap, int completedLaps, ref String str, ref uint[] decimalOrPrimeIndexList)
		{
			if ((0 != totalLaps) && (currentLap <= 99) && (totalLaps <= 99))
			{
				str = String.Format("{0,2}{1,2}", Math.Min(99, currentLap), Math.Min(99, totalLaps));
				decimalOrPrimeIndexList = s_decimalOrPrimeIndexList1;
			}
			else
			{
				str = String.Format("{0,4}", completedLaps);
			}
		}

		/// <inheritdoc/>
		public void LapsToGo(int remainingLaps, ref String str, ref uint[] decimalOrPrimeIndexList)
		{
			str = String.Format("{0,4}", remainingLaps);
		}

		/// <inheritdoc/>
		public void Position(int currentPosition, int numberOfOpponents, ref String str, ref uint[] decimalOrPrimeIndexList)
		{
			str = String.Format("{0,2}{1,2}", Math.Min(99, currentPosition), Math.Min(99, numberOfOpponents));
			decimalOrPrimeIndexList = s_decimalOrPrimeIndexList1;
		}

		/// <inheritdoc/>
		public void Temperature(String prefix, double temperature, ref String str, ref uint[] decimalOrPrimeIndexList)
		{
			str = String.Format("{0}{1,3}", prefix, Math.Round(temperature));
			decimalOrPrimeIndexList = s_decimalOrPrimeIndexList0;
		}

		/// <inheritdoc/>
		public void Speed(double speed, ref String str, ref uint[] decimalOrPrimeIndexList)
		{
			str = String.Format("{0,4:F0}", speed);
		}

		/// <inheritdoc/>
		public void Rpm(double rpm, ref String str, ref uint[] decimalOrPrimeIndexList)
		{
			str = String.Format("{0:F0}", rpm);

			if (str.Length > 4)
				str = "-";
		}

		/// <inheritdoc/>
		public String OilTemperaturePrefix()
		{
			return "o";
		}

		/// <inheritdoc/>
		public String WaterTemperaturePrefix()
		{
			return "w";
		}

		private readonly static uint[] s_decimalOrPrimeIndexList02 = { 0, 2 };
		private readonly static uint[] s_decimalOrPrimeIndexList0 = { 0 };
		private readonly static uint[] s_decimalOrPrimeIndexList1 = { 1 };
		private readonly static uint[] s_decimalOrPrimeIndexList2 = { 2 };
	}
}
