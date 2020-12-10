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
				return String.Format("Forget rotary switch {0}", (int)value + 1);
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
}
