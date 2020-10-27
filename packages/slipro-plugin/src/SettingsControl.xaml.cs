/*
 * Code-behind for plugin settings UI.
 */

using System;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;

// ---------------------------------------------------------------------------------------------------------------------------------

namespace SimElation.SimHubIntegration.SliProPlugin
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

	class RotaryDetectConverter : IValueConverter
	{
		public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
		{
			if ((int)value == SliPro.RotaryDetector.undefinedOffset)
				return String.Format("Learn {0} rotary", parameter);
			else
				return String.Format("Forget {0} rotary", parameter);
		}

		public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
		{
			throw new NotSupportedException();
		}
	}

	/// <summary>Interaction logic for SettingsControl.xaml.</summary>
	public partial class SettingsControl : UserControl
	{
		/// <summary><see cref="SliProPlugin"/> accessor.</summary>
		public SliProPlugin Plugin { get; }

		/// <summary><see cref="Settings"/> accessor.</summary>
		public Settings Settings { get; }

		/// <summary>Title for UI.</summary>
		public String Title
		{
			get =>
				String.Format("SLI-Pro Plugin {0} Options", Assembly.GetExecutingAssembly().GetName().Version.ToString());
		}

		/// <summary>List of items for the left segment display combobox.</summary>
		public String[] LeftSegmentDisplayComboBoxContents
		{
			get => Plugin.GetSegmentDisplayNameList(SliPro.SegmentDisplayPosition.left).
				Append<String>(Settings.RotaryControlledKey).ToArray();
		}

		/// <summary>List of items for the right segment display combobox.</summary>
		public String[] RightSegmentDisplayComboBoxContents
		{
			get => Plugin.GetSegmentDisplayNameList(SliPro.SegmentDisplayPosition.right).
				Append<String>(Settings.RotaryControlledKey).ToArray();
		}

		/// <summary>Constructor.</summary>
		/// <param name="plugin"></param>
		public SettingsControl(SliProPlugin plugin)
		{
			this.DataContext = this;
			this.Plugin = plugin;
			this.Settings = plugin.Settings;
			InitializeComponent();
		}

		private void OnSliProClick(object sender, System.Windows.Input.MouseButtonEventArgs e)
		{
			var point = e.GetPosition((IInputElement)sender);
			var res = GetStatusLedIndex(point);
		}

		private void OnLeftSegmentRotaryClick(object sender, System.Windows.RoutedEventArgs e)
		{
			Plugin.LearnOrForgetRotary(SliPro.RotarySwitch.leftSegment);
		}

		private void OnRightSegmentRotaryClick(object sender, System.Windows.RoutedEventArgs e)
		{
			Plugin.LearnOrForgetRotary(SliPro.RotarySwitch.rightSegment);
		}

		private void OnBrightnessRotaryClick(object sender, System.Windows.RoutedEventArgs e)
		{
			Plugin.LearnOrForgetRotary(SliPro.RotarySwitch.brightness);
		}

		private static bool LedHitTest(Point point, Point center)
		{
			// TODO maybe optimize this and/or look at VisualTreeHelper.HitTest().
			return (point.X >= (center.X - ledRadius)) &&
				(point.X <= (center.X + ledRadius)) &&
				(point.Y >= (center.Y - ledRadius)) &&
				(point.Y <= (center.Y + ledRadius));
		}

		private static int GetStatusLedIndex(Point point)
		{
			for (int i = 0; i < statusLedPositions.Length; ++i)
			{
				if (LedHitTest(point, statusLedPositions[i]))
					return i;
			}

			return -1;
		}

		private static Point[] statusLedPositions = new Point[]
			{
				new Point(30, 204),
				new Point(79, 204),
				new Point(128, 204),
				new Point(608, 204),
				new Point(657, 204),
				new Point(706, 204)
			};

		private const int ledRadius = 19;
	}
}
