/*
 * Code-behind for plugin settings UI.
 */

using System;
using System.Reflection;
using System.Windows;
using System.Windows.Controls;

// ---------------------------------------------------------------------------------------------------------------------------------

namespace SimElation.SimHubIntegration.SliProPlugin
{
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
				String.Format("SLI-PRO PLUGIN {0}", Assembly.GetExecutingAssembly().GetName().Version.ToString());
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

		private void OnHelpClick(object sender, System.Windows.RoutedEventArgs e)
		{
			const String rootUrl = "https://github.com/simelation/simhub-plugins/blob";
			String url, version = Assembly.GetExecutingAssembly().GetName().Version.ToString();
			int index = version.LastIndexOf(".");

			if (index == -1)
			{
				url = String.Format("{0}/master/README.md", rootUrl);
			}
			else
			{
				String tagVersion = version.Substring(0, index);
				url = String.Format("{0}/%40simelation/simhub-slipro-plugin%40{1}/packages/slipro-plugin/README.md",
					rootUrl, tagVersion);
			}

			System.Diagnostics.Process.Start(url);
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
