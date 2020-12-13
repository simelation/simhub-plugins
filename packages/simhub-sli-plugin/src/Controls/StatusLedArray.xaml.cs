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
}
