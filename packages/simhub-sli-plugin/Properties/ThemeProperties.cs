/*
 * Theme properties.
 */

using System.Windows;
using System.Windows.Media;

// ---------------------------------------------------------------------------------------------------------------------------------

namespace SimElation.Simhub.SliPlugin
{
	/// <summary>Common styling stuff.</summary>
	/// <remarks>
	/// This is based on https://thomaslevesque.com/2011/10/01/wpf-creating-parameterized-styles-with-attached-properties/
	/// to allow parameters to style templates.
	/// </remarks>
	public static class ThemeProperties
	{
		/// <inheritdoc/>
		public static Brush GetSetLedBrush(DependencyObject dependencyObject)
		{
			return (Brush)dependencyObject.GetValue(SetLedBrushProperty);
		}

		/// <inheritdoc/>
		public static void SetSetLedBrush(DependencyObject dependencyObject, Brush value)
		{
			dependencyObject.SetValue(SetLedBrushProperty, value);
		}

		/// <summary>Attached property for the color of a lit LED in the UI. Defaults to blue.</summary>
		public static readonly DependencyProperty SetLedBrushProperty = DependencyProperty.RegisterAttached("SetLedBrush",
			typeof(Brush), typeof(ThemeProperties), new FrameworkPropertyMetadata(Brushes.Blue));
	}
}
