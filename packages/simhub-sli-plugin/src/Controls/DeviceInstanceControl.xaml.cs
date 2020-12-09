/*
 * Code-behind for DeviceInstanceControl.
 */

using System;
using System.Globalization;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Media;
using MahApps.Metro.Controls.Dialogs;
using SimElation.SliDevices;

// ---------------------------------------------------------------------------------------------------------------------------------

namespace SimElation.Simhub.SliPlugin
{
	/// <summary>Interaction logic for DeviceInstanceControl.xaml.</summary>
	public partial class DeviceInstanceControl : UserControl
	{
		/// <summary>Constructor.</summary>
		public DeviceInstanceControl()
		{
			InitializeComponent();

			// TODO I'm sure this isn't the best way. DataContext is null after InitializeComponent() so handle its changed event.
			DataContextChanged +=
				(object sender, DependencyPropertyChangedEventArgs e) =>
				{
					m_deviceInstance = (DeviceInstance)DataContext;

					if (m_deviceInstance != null)
					{
						// TODO ditto. Using dialog:DialogParticipation.Register="{Binding}" in the xaml doesn't work, presumably
						// because {Binding} resolves to null at that point.
						DialogParticipation.SetRegister(this, this);
					}
				};
		}

		/// <summary>Get the vJoy button pulse length.</summary>
		public int? VJoyPulseButtonMs => m_deviceInstance?.DeviceSettings.VJoyButtonPulseMs;

		/// <summary>Remove a rotary switch -> vJoy mapping.</summary>
		/// <param name="rotarySwitchMapping"></param>
		public void RemoveRotarySwitchMapping(DeviceInstance.Settings.RotarySwitchMapping rotarySwitchMapping)
		{
			if (m_deviceInstance != null)
				m_deviceInstance.DeviceSettings.RotarySwitchMappings.Remove(rotarySwitchMapping);
		}

		/// <summary>Text to display when dll doesn't match driver version.</summary>
		public static String InvalidVersionString
		{
			get => String.Format("vJoy unavailable: driver version {0:x} doesn't match dll version {1:x}.",
				VJoyManager.Instance.DriverVersion, VJoyManager.Instance.DllVersion);
		}

		/// <summary>Text to display when dll version is bad and causes crashes.</summary>
		public static String BadVersionString
		{
			get => String.Format("vJoy unavailable: dll version {0:x} is known to cause crashes. Try vJoy package 2.1.8 or later.",
				VJoyManager.Instance.DllVersion);
		}

		private async void OnLeftSegmentRotaryClick(object sender, System.Windows.RoutedEventArgs e)
		{
			m_deviceInstance.DeviceSettings.LeftSegmentDisplayRotarySwitchIndex = await DetectOrForgetRotary(
				m_deviceInstance.DeviceSettings.LeftSegmentDisplayRotarySwitchIndex, "left segment display control");
		}

		private async void OnRightSegmentRotaryClick(object sender, System.Windows.RoutedEventArgs e)
		{
			m_deviceInstance.DeviceSettings.RightSegmentDisplayRotarySwitchIndex = await DetectOrForgetRotary(
				m_deviceInstance.DeviceSettings.RightSegmentDisplayRotarySwitchIndex, "right segment display control");
		}

		private async void OnBrightnessRotaryClick(object sender, System.Windows.RoutedEventArgs e)
		{
			m_deviceInstance.DeviceSettings.BrightnessRotarySwitchIndex = await DetectOrForgetRotary(
				m_deviceInstance.DeviceSettings.BrightnessRotarySwitchIndex, "brightness level");
		}

		private Task<int> DetectOrForgetRotary(int rotarySwitchIndex, String type)
		{
			return (rotarySwitchIndex == RotarySwitchDetector.unknownIndex) ?
				DetectRotary(type) : Task.FromResult<int>(RotarySwitchDetector.unknownIndex);
		}

		private async Task<int> DetectRotary(String type)
		{
			int rotarySwitchIndex = RotarySwitchDetector.unknownIndex;

			try
			{
				var dialog = await DialogCoordinator.Instance.ShowProgressAsync(this,
					String.Format("Detecting rotary switch for {0}...", type),
					String.Format("Change the position of a rotary switch on {0}", m_deviceInstance.DeviceInfo.PrettyInfo), true,
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

					rotarySwitchIndex = await m_deviceInstance.ManagedDevice.DetectRotary();

					// Just ignore detection result if cancelled.
					if (!dialog.IsCanceled)
					{
						dialog.SetProgress(1);
						dialog.SetMessage((rotarySwitchIndex == RotarySwitchDetector.unknownIndex) ?
							"No rotary detected" : String.Format("Detected rotary switch {0}",
							RotarySwitchDetector.RotarySwitchIndexToUiValue(rotarySwitchIndex)));

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

		private async void OnAddRotaryMappingClick(object sender, System.Windows.RoutedEventArgs e)
		{
			var rotarySwitchIndex = await DetectRotary("mapping to a vJoy device");
			if (RotarySwitchDetector.unknownIndex == rotarySwitchIndex)
				return;

			var rotarySwitchMapping = new DeviceInstance.Settings.RotarySwitchMapping() { RotarySwitchIndex = rotarySwitchIndex };
			m_deviceInstance.DeviceSettings.RotarySwitchMappings.Add(rotarySwitchMapping);
		}

		private void OnRefreshVJoyDevicesClick(object sender, System.Windows.RoutedEventArgs e)
		{
			// Just call through to the manager to get an up-to-date list. It's an ObservableCollection so the UI will update.
			_ = VJoyManager.Instance.DeviceIds;
		}

		private DeviceInstance m_deviceInstance;
	}

	/// <summary>Convert a product id to the appropriate image.</summary>
	public sealed class ProductIdToImageConverter : IValueConverter
	{
		/// <inheritdoc/>
		public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
		{
			var productId = (int)value;

			switch (productId)
			{
				case SliDevices.Pro.Constants.CompileTime.productId:
					return "..\\..\\assets\\SLI-PRO-RevA.png";

				case SliDevices.F1.Constants.CompileTime.productId:
					return "..\\..\\assets\\SLI-F1.png";

				default:
					return "";
			}
		}

		/// <inheritdoc/>
		public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
		{
			return DependencyProperty.UnsetValue;
		}
	}

	/// <summary>Convert a product id to the appropriate orientation of the status LEDs (pro - horizontal; f1 - vertical).</summary>
	public sealed class ProductIdToStatusLedOrientation : IValueConverter
	{
		/// <inheritdoc/>
		public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
		{
			var productId = (int)value;

			switch (productId)
			{
				case SliDevices.F1.Constants.CompileTime.productId:
					return Orientation.Vertical;

				default:
					return Orientation.Horizontal;
			}
		}

		/// <inheritdoc/>
		public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
		{
			return DependencyProperty.UnsetValue;
		}
	}

	/// <summary>Converter for rotary switch index to text for button.</summary>
	public sealed class RotarySwitchIndexConverter : IValueConverter
	{
		/// <inheritdoc/>
		public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
		{
			if ((int)value == SliDevices.RotarySwitchDetector.unknownIndex)
				return "Learn rotary switch";
			else
				return String.Format("Forget rotary switch {0}", RotarySwitchDetector.RotarySwitchIndexToUiValue((int)value));
		}

		/// <inheritdoc/>
		public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
		{
			return DependencyProperty.UnsetValue;
		}
	}

	/// <summary>Boolean converter for IsEnabled based off rotary switch control enabled.</summary>
	public sealed class IsRotarySwitchControlledConverter : IValueConverter
	{
		/// <inheritdoc/>
		public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
		{
			int rotarySwitchIndex = (int)value;

			return rotarySwitchIndex != SliDevices.RotarySwitchDetector.unknownIndex;
		}

		/// <inheritdoc/>
		public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
		{
			return DependencyProperty.UnsetValue;
		}
	}

	/// <summary>Boolean converter for IsEnabled based off rotary switch control NOT enabled.</summary>
	public sealed class IsNotRotarySwitchControlledConverter : IValueConverter
	{
		/// <inheritdoc/>
		public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
		{
			int rotarySwitchIndex = (int)value;

			return rotarySwitchIndex == SliDevices.RotarySwitchDetector.unknownIndex;
		}

		/// <inheritdoc/>
		public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
		{
			return DependencyProperty.UnsetValue;
		}
	}

	/// <summary>Boolean to LED color converter (for pit lane LEDs).</summary>
	public sealed class BoolToPitLaneLedColorConverter : IValueConverter
	{
		/// <inheritdoc/>
		public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
		{
			return System.Convert.ToBoolean(value) ? Colors.DimGray.ToString() : Colors.Blue.ToString();
		}

		/// <inheritdoc/>
		public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
		{
			return DependencyProperty.UnsetValue;
		}
	}

	/// <summary>Visibility of rotary switch -> vJoy mapping section. Needs vJoy installed!</summary>
	public sealed class IsVJoyInstalledToVisibilityConverter : IValueConverter
	{
		/// <inheritdoc/>
		public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
		{
			var driverVersion = (uint)value;

			return (driverVersion > 0) ? Visibility.Visible : Visibility.Collapsed;
		}

		/// <inheritdoc/>
		public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
		{
			return DependencyProperty.UnsetValue;
		}
	}

	/// <summary>If vJoy driver/dll versions don't match, return Visibility.Visible converter.</summary>
	public sealed class IsVJoyInvalidVersionToVisibilityConverter : IValueConverter
	{
		/// <inheritdoc/>
		public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
		{
			var isValidVersion = (bool)value;

			return isValidVersion ? Visibility.Collapsed : Visibility.Visible;
		}

		/// <inheritdoc/>
		public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
		{
			return DependencyProperty.UnsetValue;
		}
	}
}
