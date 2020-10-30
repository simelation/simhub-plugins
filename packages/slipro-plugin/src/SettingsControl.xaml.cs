﻿/*
 * Code-behind for plugin settings UI.
 */

using System;
using System.Globalization;
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
			return String.Format("{0} rotary control", ((int)value == SliPro.RotaryDetector.undefinedOffset) ? "Learn" : "Forget");
		}

		public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
		{
			return DependencyProperty.UnsetValue;
		}
	}

	class NotConverter : IValueConverter
	{
		public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
		{
			return !(bool)value;
		}

		public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
		{
			return DependencyProperty.UnsetValue;
		}
	}

	class BrightnessRotaryEnabledConverter : IValueConverter
	{
		public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
		{
			return value == null;
		}

		public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
		{
			return DependencyProperty.UnsetValue;
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
		public static String Title
		{
			get =>
				String.Format("SLI-Pro Plugin {0} Options", Assembly.GetExecutingAssembly().GetName().Version.ToString());
		}

		/// <summary>List of items for the left segment display combobox.</summary>
		public String[] LeftSegmentDisplayComboBoxContents
		{
			get => MakeSegmentDisplayNameList(SliPro.SegmentDisplayPosition.left);
		}

		/// <summary>List of items for the right segment display combobox.</summary>
		public String[] RightSegmentDisplayComboBoxContents
		{
			get => MakeSegmentDisplayNameList(SliPro.SegmentDisplayPosition.right);
		}

		/// <summary>Left segment display previous action name.</summary>
		public static String LeftSegmentDisplayPreviousAction
		{
			get => MakeSegmentDisplayActionName(SliProPlugin.LeftSegmentDisplayPreviousAction);
		}

		/// <summary>Left segment display next action name.</summary>
		public static String LeftSegmentDisplayNextAction
		{
			get => MakeSegmentDisplayActionName(SliProPlugin.LeftSegmentDisplayNextAction);
		}

		/// <summary>Right segment display previous action name.</summary>
		public static String RightSegmentDisplayPreviousAction
		{
			get => MakeSegmentDisplayActionName(SliProPlugin.RightSegmentDisplayPreviousAction);
		}

		/// <summary>Right segment display next action name.</summary>
		public static String RightSegmentDisplayNextAction
		{
			get => MakeSegmentDisplayActionName(SliProPlugin.RightSegmentDisplayNextAction);
		}

		private static String MakeSegmentDisplayActionName(String baseName)
		{
			// NB pluginManager.AddAction() implicitly adds the plugin name to the action but we need to specify it for the xaml.
			return String.Format("{0}.{1}", typeof(SliProPlugin).Name, baseName);
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

		private String[] MakeSegmentDisplayNameList(SliPro.SegmentDisplayPosition segmentDisplayPosition)
		{
			return Plugin.GetSegmentDisplayNameList(segmentDisplayPosition);
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
