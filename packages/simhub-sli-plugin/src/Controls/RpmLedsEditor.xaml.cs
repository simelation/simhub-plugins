/*
 * RPM LED editing control.
 */

using System.Collections;
using System.Windows;
using System.Windows.Controls;

// ---------------------------------------------------------------------------------------------------------------------------------

namespace SimElation.Simhub.SliPlugin
{
	/// <summary>Interaction logic for RpmLedsEditor.xaml.</summary>
	public partial class RpmLedsEditor : UserControl
	{
		private static readonly DependencyProperty ItemsSourceProperty =
			DependencyProperty.Register(nameof(ItemsSource), typeof(IEnumerable), typeof(RpmLedsEditor));

		/// <summary>Set of <see cref="DeviceInstance.Settings.RpmLed"/>s.</summary>
		public IEnumerable ItemsSource
		{
			get => GetValue(ItemsSourceProperty) as IEnumerable;
			set => SetValue(ItemsSourceProperty, value);
		}

		/// <summary>Constructor.</summary>
		public RpmLedsEditor()
		{
			InitializeComponent();
		}
	}
}
