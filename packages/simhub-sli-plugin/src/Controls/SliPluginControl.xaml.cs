/*
 * Code-behind for plugin UI.
 */

using System;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using SimHub.Plugins.Styles;

// ---------------------------------------------------------------------------------------------------------------------------------

namespace SimElation.Simhub.SliPlugin
{
	/// <summary>Interaction logic for SliPluginControl.xaml.</summary>
	public partial class SliPluginControl : UserControl
	{
		/// <summary><see cref="Simhub.SliPlugin.SliPlugin"/> accessor.</summary>
		public SliPlugin Plugin { get; }

		/// <summary>Title for UI.</summary>
		public static String Title
		{
			get => String.Format("SLI PLUGIN {0} ({1} build)", Assembly.GetExecutingAssembly().GetName().Version.ToString(),
				Assembly.GetExecutingAssembly().GetCustomAttributes<AssemblyConfigurationAttribute>().
					FirstOrDefault().Configuration);
		}

		/// <summary>Constructor.</summary>
		/// <param name="sliPlugin"></param>
		public SliPluginControl(SliPlugin sliPlugin)
		{
			DataContext = this;
			Plugin = sliPlugin;
			InitializeComponent();
		}

		private void OnHelpClick(object sender, System.Windows.RoutedEventArgs e)
		{
			const String rootUrl = "https://github.com/simelation/simhub-plugins/blob";
			String branch = "master", version = Assembly.GetExecutingAssembly().GetName().Version.ToString();
			int index = version.LastIndexOf(".");

			if (index != -1)
			{
				String tagVersion = version.Substring(0, index);
				branch = String.Format("%40simelation/simhub-sli-plugin%40{0}", tagVersion);
			}

			String url = String.Format("{0}/{1}/packages/sli-plugin/README.md", rootUrl, branch);
			System.Diagnostics.Process.Start(url);
		}

		private void OnAddManagedDevice(object sender, System.Windows.RoutedEventArgs e)
		{
			var deviceInstance = (DeviceInstance)((SHToggleButton)e.Source).DataContext;

			Plugin.AddManagedDevice(deviceInstance);
		}

		private void OnRemoveManagedDevice(object sender, System.Windows.RoutedEventArgs e)
		{
			var deviceInstance = (DeviceInstance)((SHToggleButton)e.Source).DataContext;

			Plugin.RemoveManagedDevice(deviceInstance);
		}
	}

	/// <summary>Convert <see cref="DeviceInstance.IsManaged"/> to a string for the UI.</summary>
	public sealed class IsManagedToStatusConverter : IValueConverter
	{
		/// <inheritdoc/>
		public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
		{
			var isManaged = (bool)value;

			return isManaged ? "Unplugged" : "Unmanaged";
		}

		/// <inheritdoc/>
		public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
		{
			return DependencyProperty.UnsetValue;
		}
	}
}
