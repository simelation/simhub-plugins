/*
 * Code-behind for status LED array.
 */

using System;
using System.Collections;
using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;

// ---------------------------------------------------------------------------------------------------------------------------------

namespace SimElation.Simhub.SliPlugin
{
	/// <summary>Interaction logic for StatusLedArray.xaml.</summary>
	public partial class StatusLedArray : UserControl
	{
		private static readonly DependencyProperty TitleProperty =
			DependencyProperty.Register(nameof(Title), typeof(string), typeof(StatusLedArray));

		private static readonly DependencyProperty OrientationProperty =
			DependencyProperty.Register(nameof(Orientation), typeof(Orientation), typeof(StatusLedArray),
				new PropertyMetadata(Orientation.Horizontal));

		private static readonly DependencyProperty StatusLedsProperty =
			DependencyProperty.Register(nameof(StatusLeds), typeof(IEnumerable), typeof(StatusLedArray));

		private static readonly DependencyProperty NumberEnabledProperty =
			DependencyProperty.Register(nameof(NumberEnabled), typeof(int), typeof(StatusLedArray), new PropertyMetadata(-1));

		/// <summary>Title property for display (e.g. "Left status LEDs").</summary>
		public string Title
		{
			get => GetValue(TitleProperty) as string;
			set => SetValue(TitleProperty, value);
		}

		/// <summary>Orientation of the array.</summary>
		public Orientation Orientation
		{
			get => (Orientation)GetValue(OrientationProperty);
			set => SetValue(OrientationProperty, value);
		}

		/// <summary>The collection of LEDs.</summary>
		public IEnumerable StatusLeds
		{
			get => GetValue(StatusLedsProperty) as IEnumerable;
			set => SetValue(StatusLedsProperty, value);
		}

		/// <summary>The number of LEDs at the start of the array than can be set.</summary>
		public int NumberEnabled
		{
			get => (int)GetValue(NumberEnabledProperty);
			set => SetValue(NumberEnabledProperty, value);
		}

		/// <summary>Default constructor.</summary>
		public StatusLedArray()
		{
			InitializeComponent();
		}
	}

	/// <summary>Convert a SimHib <see cref="SimHub.Plugins.OutputPlugins.GraphicalDash.Models.BindingMode"/> to a bool.</summary>
	public sealed class IsDashBindingDataModeNotNone : IValueConverter
	{
		/// <inheritdoc/>
		public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
		{
			var bindingData = (SimHub.Plugins.OutputPlugins.GraphicalDash.Models.DashboardBindingData)value;

			// NB Only Mode gets serialized to config, not IsNone etc.
			return bindingData.Mode != SimHub.Plugins.OutputPlugins.GraphicalDash.Models.BindingMode.None;
		}

		/// <inheritdoc/>
		public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
		{
			return DependencyProperty.UnsetValue;
		}
	}

	/// <summary>Should the control for number of enabled LEDs be visible?</summary>
	public class NumberEnabledVisibilityConverter : IValueConverter
	{
		/// <inheritdoc/>
		public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
		{
			return ((int)value == -1) ? Visibility.Collapsed : Visibility.Visible;
		}

		/// <inheritdoc/>
		public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
		{
			return DependencyProperty.UnsetValue;
		}
	}

	/// <summary>Common functionality for status LED converters.</summary>
	public class StatusLedConverterBase
	{
		/// <summary>
		/// Is an LED enabled? Its index must be less than the number of enabled LEDs in the array (or number enabled is -1).
		/// </summary>
		protected bool IsEnabled(object[] values)
		{
			var led = (Led)values[0];
			var statusLeds = (Led[])(values[1]);
			var numberEnabled = (int)values[2];

			return (-1 == numberEnabled) ? true : (Array.IndexOf(statusLeds, led) < numberEnabled);
		}
	}

	/// <summary>Is an LED clickable?</summary>
	public sealed class IsStatusLedEnabled : StatusLedConverterBase, IMultiValueConverter
	{
		/// <inheritdoc/>
		public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
		{
			return IsEnabled(values);
		}

		/// <inheritdoc/>
		public object[] ConvertBack(object value, Type[] targetType, object parameter, CultureInfo culture)
		{
			throw new NotSupportedException();
		}
	}

	/// <summary>Is a formula assigned?</summary>
	public sealed class IsStatusLedAssigned : StatusLedConverterBase, IMultiValueConverter
	{
		/// <inheritdoc/>
		public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
		{
			var isNone = (bool)values[3];
			return IsEnabled(values) && !isNone;
		}

		/// <inheritdoc/>
		public object[] ConvertBack(object value, Type[] targetType, object parameter, CultureInfo culture)
		{
			throw new NotSupportedException();
		}
	}
}
