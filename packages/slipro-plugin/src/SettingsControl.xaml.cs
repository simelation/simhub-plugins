/*
 * Code-behind for plugin settings UI.
 */

using System;
using System.Reflection;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using MahApps.Metro.Controls.Dialogs;
using MahApps.Metro.SimpleChildWindow;
using SimElation.SliPro;
using SimHub.Plugins.OutputPlugins.Dash.WPFUI;

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
			get => String.Format("SLI-PRO PLUGIN {0}", Assembly.GetExecutingAssembly().GetName().Version.ToString());
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

		/// <summary>Peek current left segment display action name.</summary>
		public static String PeekCurrentLeftSegmentDisplayAction
		{
			get => MakeSegmentDisplayActionName(SliProPlugin.PeekCurrentLeftSegmentDisplayAction);
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

		/// <summary>Peek current right segment display action name.</summary>
		public static String PeekCurrentRightSegmentDisplayAction
		{
			get => MakeSegmentDisplayActionName(SliProPlugin.PeekCurrentRightSegmentDisplayAction);
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
			DataContext = this;
			Plugin = plugin;
			Settings = plugin.Settings;
			InitializeComponent();
		}

		private String[] MakeSegmentDisplayNameList(SliPro.SegmentDisplayPosition segmentDisplayPosition)
		{
			return Plugin.GetSegmentDisplayNameList(segmentDisplayPosition);
		}

		private void OnHelpClick(object sender, System.Windows.RoutedEventArgs e)
		{
			const String rootUrl = "https://github.com/simelation/simhub-plugins/blob";
			String branch = "master", version = Assembly.GetExecutingAssembly().GetName().Version.ToString();
			int index = version.LastIndexOf(".");

			if (index != -1)
			{
				String tagVersion = version.Substring(0, index);
				branch = String.Format("%40simelation/simhub-slipro-plugin%40{0}", tagVersion);
			}

			String url = String.Format("{0}/{1}/packages/slipro-plugin/README.md", rootUrl, branch);
			System.Diagnostics.Process.Start(url);
		}

		private async void OnExternalLedClick(object sender, System.Windows.RoutedEventArgs e)
		{
			// sender.DataContext is the bound GameProperty in settings.
			var propertiesPicker = new PropertiesPicker();

			// TODO highlight currently selected item??
			await ChildWindowManager.ShowChildWindowAsync(Window.GetWindow(this), propertiesPicker);

			var property = propertiesPicker.Result;
			if (property == null)
				return;

			((sender as Button).DataContext as GameProperty).Key = property.Key;
		}

		private void OnExternalLedRightClick(object sender, System.Windows.RoutedEventArgs e)
		{
			// Clear down any assigned property key.
			((sender as Button).DataContext as GameProperty).Key = GameProperty.Unassigned;
		}

		private async void OnLeftSegmentRotaryClick(object sender, System.Windows.RoutedEventArgs e)
		{
			Settings.LeftSegmentDisplayRotarySwitchIndex =
				await DetectOrForgetRotary(Settings.LeftSegmentDisplayRotarySwitchIndex, "left segment display control");
		}

		private async void OnRightSegmentRotaryClick(object sender, System.Windows.RoutedEventArgs e)
		{
			Settings.RightSegmentDisplayRotarySwitchIndex =
				await DetectOrForgetRotary(Settings.RightSegmentDisplayRotarySwitchIndex, "right segment display control");
		}

		private async void OnBrightnessRotaryClick(object sender, System.Windows.RoutedEventArgs e)
		{
			Settings.SliProSettings.BrightnessRotarySwitchIndex =
				await DetectOrForgetRotary(Settings.SliProSettings.BrightnessRotarySwitchIndex, "brightness level");
		}

		private Task<int> DetectOrForgetRotary(int rotarySwitchIndex, String type)
		{
			return (rotarySwitchIndex == RotaryDetector.unknownIndex) ?
				DetectRotary(type) : Task.FromResult<int>(RotaryDetector.unknownIndex);
		}

		private async Task<int> DetectRotary(String type)
		{
			int rotarySwitchIndex = RotaryDetector.unknownIndex;

			try
			{
				var dialog = await DialogCoordinator.Instance.ShowProgressAsync(this,
					String.Format("Detecting rotary switch for {0}...", type),
					"Change the position of a rotary switch", true,
					new MetroDialogSettings()
					{
						AnimateShow = false,
						AnimateHide = false
					});

				try
				{
					// TODO I suppose the actual rotary detection code should be cancelleable and cancelling the dialog should
					// trigger that really. For now we'll just close the dialog and the detection code will silently timeout.
					dialog.Canceled += async (sender, eventArgs) => await dialog.CloseAsync();
					dialog.SetIndeterminate();

					rotarySwitchIndex = await Plugin.DetectRotary();

					// Just ignore detection result if cancelled.
					if (!dialog.IsCanceled)
					{
						dialog.SetProgress(1);
						dialog.SetMessage((rotarySwitchIndex == RotaryDetector.unknownIndex) ?
							"No rotary detected" : String.Format("Detected rotary switch {0}", rotarySwitchIndex + 1));

						// Wait a bit to display detection feedback.
						await Task.Delay(2000);
					}
				}
				finally
				{
					if (dialog.IsOpen)
						_ = dialog.CloseAsync();
				}
			}
			catch (Exception)
			{
			}

			return rotarySwitchIndex;
		}

		private void OnSliProClick(object sender, System.Windows.Input.MouseButtonEventArgs e)
		{
			//var point = e.GetPosition((IInputElement)sender);
			//var res = GetStatusLedIndex(point);
		}
		/*
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

				private readonly static Point[] statusLedPositions = new Point[]
					{
								new Point(30, 204),
								new Point(79, 204),
								new Point(128, 204),
								new Point(608, 204),
								new Point(657, 204),
								new Point(706, 204)
					};

				private const int ledRadius = 19;
		*/
	}
}
