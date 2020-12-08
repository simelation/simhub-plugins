/*
 * Code-behind for status LED array.
 */

using System;
using System.Collections;
using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using MahApps.Metro.SimpleChildWindow;
using SimHub.Plugins.OutputPlugins.EditorControls;

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

		private async void OnClick(object sender, System.Windows.RoutedEventArgs e)
		{
			var toggleButton = sender as ToggleButton;
			var led = (toggleButton.DataContext as Led);
			var bindingEditor = new BindingEditor();

			// HACK Can't get ToggleButton to behave exactly as I want: binding IsChecked using OneTime mode, but after that
			// clicking on the button WILL toggle the control's IsChecked property. I don't want that but there doesn't
			// seem to be a OneTimeBindingThenLeaveItToCodeBehind. Maybe a custom toggle button or just a plain button is better
			// (though the latter will complicated the styling, I think, since we can't trigger on IsChecked).
			// So just toggle the IsChecked state here after a click before we decide whether it is truly checked once
			// the user has closed the binding dialog.
			toggleButton.IsChecked ^= true;

			// Clone current state so cancelling the dialog doesn't overwrite settings. Clone() doesn't deep copy the
			// actual Formula object though!
			// TODO raise ticket about right thing to do.
			var bindingData = (SimHub.Plugins.OutputPlugins.GraphicalDash.Models.DashboardBindingData)led.BindingData.Clone();
			bindingData.Formula = new SimHub.Plugins.OutputPlugins.Dash.GLCDTemplating.ExpressionValue(
				bindingData.Formula.Expression, bindingData.Formula.Interpreter);
			bindingEditor.DataContext = bindingData;

			await ChildWindowManager.ShowChildWindowAsync(Window.GetWindow(this), bindingEditor);
			if (bindingEditor.OK)
				led.BindingData = bindingData;

			toggleButton.IsChecked = !led.BindingData.IsNone;
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
