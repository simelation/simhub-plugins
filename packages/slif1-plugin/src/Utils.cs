/*
 * A few text formatting utils.
 */

using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;
using System.Windows.Media;

// ---------------------------------------------------------------------------------------------------------------------------------

namespace SimElation.SimHubIntegration.SliF1Plugin.Utils
{
	/// <summary>String formatting helpers.</summary>
	public static class Formatters
	{
		/// <summary>Format a delta time.</summary>
		/// <param name="delta">The delta time (in seconds).</param>
		/// <param name="str">To receive the formatted string.</param>
		/// <param name="decimalOrPrimeIndexList">To receive the index of decimal point.</param>
		static public void DeltaTime(double delta, out String str, out uint[] decimalOrPrimeIndexList)
		{
			const String overflowStr = "-";

			if (delta >= 0.0)
			{
				if (delta < 1.0)
				{
					str = String.Format(" {0:000}", delta * 100.0);
					decimalOrPrimeIndexList = new uint[] { 1 };
				}
				else if (delta < 100.0)
				{
					str = String.Format("{0,4:F0}", delta * 100.0);
					decimalOrPrimeIndexList = new uint[] { 1 };
				}
				else if (delta < 1000.0)
				{
					str = String.Format("{0,4:F0}", delta * 10.0);
					decimalOrPrimeIndexList = new uint[] { 2 };
				}
				else if (delta <= 10000.0)
				{
					str = String.Format("{0,4:F0}", delta);
					decimalOrPrimeIndexList = new uint[] { };
				}
				else
				{
					str = overflowStr;
					decimalOrPrimeIndexList = new uint[] { };
				}
			}
			else
			{
				if (delta > -1.0)
				{
					str = String.Format("{0:000}", delta * 100.0);
					decimalOrPrimeIndexList = new uint[] { 1 };
				}
				else if (delta > -10.0)
				{
					str = String.Format("{0,4:F0}", delta * 100.0);
					decimalOrPrimeIndexList = new uint[] { 1 };
				}
				else if (delta > -100.0)
				{
					str = String.Format("{0,4:F0}", delta * 10.0);
					decimalOrPrimeIndexList = new uint[] { 2 };
				}
				else if (delta > -1000.0)
				{
					str = String.Format("{0,4:F0}", delta);
					decimalOrPrimeIndexList = new uint[] { };
				}
				else
				{
					str = overflowStr;
					decimalOrPrimeIndexList = new uint[] { };
				}
			}
		}

		/// <summary>Format a lap time.</summary>
		/// <remarks>
		/// As "m.ss.t" (t=tenths) when minutes is less than 10, or "mm.ss" (t=tenths) otherwise.
		/// </remarks>
		/// <param name="timeSpan">The lap time.</param>
		/// <param name="str">To receive the formatted string.</param>
		/// <param name="decimalOrPrimeIndexList">To receive the indexes of decimal points/primes.</param>
		static public void LapTime(TimeSpan timeSpan, out String str, out uint[] decimalOrPrimeIndexList)
		{
			if (timeSpan.TotalMinutes < 10.0)
			{
				decimalOrPrimeIndexList = new uint[] { 0, 2 };
				str = String.Format("{0:0}{1:00}{2:0}", (int)timeSpan.TotalMinutes, timeSpan.Seconds, timeSpan.Milliseconds / 100);
			}
			else if (timeSpan.TotalMinutes < 100.0)
			{
				decimalOrPrimeIndexList = new uint[] { 1 };
				str = String.Format("{0:00}{1:00}", (int)timeSpan.TotalMinutes, timeSpan.Seconds);
			}
			else
			{
				decimalOrPrimeIndexList = new uint[] { };
				str = "-";
			}
		}
	}

	namespace ValueConverters
	{
		/// <summary>Helper converter for MahApps to allow null, until 2.0.</summary>
		/// <remarks>
		/// See https://github.com/MahApps/MahApps.Metro/issues/3786
		/// </remarks>
		class AllowNullConverter : IValueConverter
		{
			public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
			{
				return value;
			}

			public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
			{
				return value;
			}
		}

		/// <summary>Converter for rotary switch index to text for button.</summary>
		class RotarySwitchIndexConverter : IValueConverter
		{
			public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
			{
				if ((int)value == SliF1.RotaryDetector.unknownIndex)
					return "Learn rotary switch";
				else
					return String.Format("Forget rotary switch {0}", (int)value + 1);
			}

			public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
			{
				return DependencyProperty.UnsetValue;
			}
		}

		/// <summary>Boolean converter for IsEnabled based off rotary switch control enabled.</summary>
		class IsRotarySwitchControlledConverter : IValueConverter
		{
			public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
			{
				int rotarySwitchIndex = (int)value;

				return rotarySwitchIndex != SliF1.RotaryDetector.unknownIndex;
			}

			public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
			{
				return DependencyProperty.UnsetValue;
			}
		}

		/// <summary>Boolean converter for IsEnabled based off rotary switch control not enabled.</summary>
		class IsNotRotarySwitchControlledConverter : IValueConverter
		{
			public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
			{
				int rotarySwitchIndex = (int)value;

				return rotarySwitchIndex == SliF1.RotaryDetector.unknownIndex;
			}

			public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
			{
				return DependencyProperty.UnsetValue;
			}
		}

		/// <summary>Converter for tooltips on something holding a game property key (e.g. LED).</summary>
		class GamePropertyToolTipConverter : IValueConverter
		{
			public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
			{
				var propertyKey = (String)value;

				return (propertyKey == GameProperty.Unassigned) ? "No property assigned. Left click to set." :
					String.Format("Assigned property: {0}. Right-click to clear.", propertyKey);
			}

			public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
			{
				return DependencyProperty.UnsetValue;
			}
		}

		/// <summary>Converter for LED colors to indicate assigned/not assigned.</summary>
		class GamePropertyLedColorConverter : IValueConverter
		{
			public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
			{
				var propertyKey = (String)value;

				return (propertyKey == GameProperty.Unassigned) ? Colors.DimGray.ToString() : Colors.Red.ToString();
			}

			public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
			{
				return DependencyProperty.UnsetValue;
			}
		}
	}
}
