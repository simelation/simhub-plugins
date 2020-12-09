/*
 * Code-behind for RotarySwitchMappingControl.
 */

using System;
using System.Collections.Specialized;
using System.Globalization;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Media;
using MahApps.Metro.Controls.Dialogs;

// ---------------------------------------------------------------------------------------------------------------------------------

namespace SimElation.Simhub.SliPlugin
{
	/// <summary>Workaround for ComboBox wiping out Text when Items changes, even if IsEditable!</summary>
	/// <remarks>
	/// See: https://stackoverflow.com/questions/22221199/how-to-disable-itemssource-synchronization-with-text-property-of-combobox
	/// </remarks>
	sealed class RotarySwitchMappingControlComboBox : ComboBox
	{
		protected override void OnSelectionChanged(SelectionChangedEventArgs e)
		{
			if (!m_isUpdatingItems)
				base.OnSelectionChanged(e);
		}

		protected override void OnItemsChanged(NotifyCollectionChangedEventArgs e)
		{
			try
			{
				m_isUpdatingItems = true;
				base.OnItemsChanged(e);
			}
			finally
			{
				m_isUpdatingItems = false;
			}
		}

		private bool m_isUpdatingItems = false;
	}

	/// <summary>Interaction logic for RotarySwitchMappingControl.xaml.</summary>
	public partial class RotarySwitchMappingControl : UserControl
	{
		/// <summary>Simulate rotary switch position.</summary>
		public uint SimulateRotarySwitchPosition { get; set; } = 1;

		/// <summary>How long to pause after pressing simulate button press before doing so.</summary>
		public int SimulatePressPauseTimeMs { get; set; } = 5000;

		/// <summary>Constructor.</summary>
		public RotarySwitchMappingControl()
		{
			InitializeComponent();

			// TODO I'm sure this isn't the best way. DataContext is null after InitializeComponent() so handle its changed event.
			DataContextChanged +=
				(object sender, DependencyPropertyChangedEventArgs e) =>
				{
					m_rotarySwitchMapping = (DeviceInstance.Settings.RotarySwitchMapping)DataContext;

					if (m_rotarySwitchMapping != null)
					{
						// TODO ditto. Using dialog:DialogParticipation.Register="{Binding}" in the xaml doesn't work, presumably
						// because {Binding} resolves to null at that point.
						DialogParticipation.SetRegister(this, this);
					}
				};
		}

		private async void OnRemoveClick(object sender, System.Windows.RoutedEventArgs e)
		{
			// TODO ugh. Should learn about commands I suppose.
			var deviceInstanceControl = FindParent<DeviceInstanceControl>(this);
			if ((deviceInstanceControl != null) && (m_rotarySwitchMapping != null))
			{
				var res = await DialogCoordinator.Instance.ShowMessageAsync(this, "Confirm delete",
					"Are you sure you want to delete this mapping?",
					MessageDialogStyle.AffirmativeAndNegative,
					new MetroDialogSettings()
					{
						AnimateShow = false,
						AnimateHide = false
					});

				if (res != MessageDialogResult.Affirmative)
					return;

				deviceInstanceControl.RemoveRotarySwitchMapping(m_rotarySwitchMapping);
			}
		}

		private async void OnTestButtonClick(object sender, System.Windows.RoutedEventArgs e)
		{
			if (m_rotarySwitchMapping == null)
				return;

			var deviceInstanceControl = FindParent<DeviceInstanceControl>(this);
			int pulseMs = deviceInstanceControl?.VJoyPulseButtonMs ?? 50;

			var buttonId = SimulateRotarySwitchPosition + m_rotarySwitchMapping.FirstVJoyButtonId - 1;
			await Task.Delay(SimulatePressPauseTimeMs);
			_ = VJoyManager.Instance.PulseButton(m_rotarySwitchMapping.VJoyDeviceId, buttonId, pulseMs);
		}

		private static T FindParent<T>(DependencyObject dependencyObject)
			where T : DependencyObject
		{
			DependencyObject parentObject = VisualTreeHelper.GetParent(dependencyObject);

			if (parentObject == null)
				return null;

			T parent = parentObject as T;
			if (parent != null)
				return parent;
			else
				return FindParent<T>(parentObject);
		}

		private DeviceInstance.Settings.RotarySwitchMapping m_rotarySwitchMapping;
	}

	/// <summary>Convert a rotary switch index to a number for the UI.</summary>
	public sealed class RotarySwitchIndexToTitleConverter : IValueConverter
	{
		/// <inheritdoc/>
		public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
		{
			var rotarySwitchIndex = (int)value;
			return String.Format("Configuration for rotary switch {0}",
				SliDevices.RotarySwitchDetector.RotarySwitchIndexToUiValue(rotarySwitchIndex));
		}

		/// <inheritdoc/>
		public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
		{
			return DependencyProperty.UnsetValue;
		}
	}
}
