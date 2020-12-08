/*
 * Code-behind for segment display control UI.
 */

using System;
using System.Collections;
using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;

// ---------------------------------------------------------------------------------------------------------------------------------

namespace SimElation.Simhub.SliPlugin
{
	/// <summary>Interaction logic for SegmentDisplayControl.xaml.</summary>
	public partial class SegmentDisplayControl : UserControl
	{
		private static readonly DependencyProperty LabelProperty =
			DependencyProperty.Register(nameof(Label), typeof(string), typeof(SegmentDisplayControl));

		private static readonly DependencyProperty RotarySwitchIndexProperty =
			DependencyProperty.Register(nameof(RotarySwitchIndex), typeof(int), typeof(SegmentDisplayControl));

		private static readonly DependencyProperty LearnRotaryButtonContentProperty =
			DependencyProperty.Register(nameof(LearnRotaryButtonContent), typeof(string), typeof(SegmentDisplayControl));

		private static readonly DependencyProperty SegmentDisplayFriendlyNamesProperty =
			DependencyProperty.Register(nameof(SegmentDisplayFriendlyNames), typeof(IEnumerable), typeof(SegmentDisplayControl));

		private static readonly DependencyProperty SelectedIndexProperty =
			DependencyProperty.Register(nameof(SelectedIndex), typeof(int), typeof(SegmentDisplayControl));

		private static readonly DependencyProperty NextDisplayProperty =
			DependencyProperty.Register(nameof(NextDisplay), typeof(string), typeof(SegmentDisplayControl));

		private static readonly DependencyProperty PreviousDisplayProperty =
			DependencyProperty.Register(nameof(PreviousDisplay), typeof(string), typeof(SegmentDisplayControl));

		private static readonly DependencyProperty PeekCurrentDisplayProperty =
			DependencyProperty.Register(nameof(PeekCurrentDisplay), typeof(string), typeof(SegmentDisplayControl));

		private static readonly RoutedEvent LearnRotaryClickEvent = ButtonBase.ClickEvent.AddOwner(typeof(SegmentDisplayControl));

		/// <summary>Title property for segment display (e.g. "Left segment", "Right segment").</summary>
		public string Label
		{
			get => GetValue(LabelProperty) as string;
			set => SetValue(LabelProperty, value);
		}

		/// <summary>Assigned rotary switch index.</summary>
		public int RotarySwitchIndex
		{
			get => (GetValue(RotarySwitchIndexProperty) as int?) ?? SliDevices.RotarySwitchDetector.unknownIndex;
			set => SetValue(RotarySwitchIndexProperty, value);
		}

		/// <summary>Content for the learn rotary button.</summary>
		public string LearnRotaryButtonContent
		{
			get => GetValue(LearnRotaryButtonContentProperty) as string;
			set => SetValue(LearnRotaryButtonContentProperty, value);
		}

		/// <summary>Property for an enumerable of items for the drop down (the various displays available).</summary>
		public IEnumerable SegmentDisplayFriendlyNames
		{
			get => GetValue(SegmentDisplayFriendlyNamesProperty) as IEnumerable;
			set => SetValue(SegmentDisplayFriendlyNamesProperty, value);
		}

		/// <summary>Currently selected display property.</summary>
		public int SelectedIndex
		{
			get => (GetValue(SelectedIndexProperty) as int?) ?? -1;
			set => SetValue(SelectedIndexProperty, value);
		}

		/// <summary>Name for next display action.</summary>
		public string NextDisplay
		{
			get => GetValue(NextDisplayProperty) as string;
			set => SetValue(NextDisplayProperty, value);
		}

		/// <summary>Name for previous display action.</summary>
		public string PreviousDisplay
		{
			get => GetValue(PreviousDisplayProperty) as string;
			set => SetValue(PreviousDisplayProperty, value);
		}

		/// <summary>Name for peek current display action.</summary>
		public string PeekCurrentDisplay
		{
			get => GetValue(PeekCurrentDisplayProperty) as string;
			set => SetValue(PeekCurrentDisplayProperty, value);
		}

		/// <summary>Click handler for learn/forget rotary.</summary>
		public event RoutedEventHandler LearnRotaryClick
		{
			// Button has x:Name property ("learnRotaryButton") in the xaml so we can access it here.
			add => learnRotaryButton.AddHandler(LearnRotaryClickEvent, value);
			remove => learnRotaryButton.RemoveHandler(LearnRotaryClickEvent, value);
		}

		/// <summary>Constructor.</summary>
		public SegmentDisplayControl()
		{
			InitializeComponent();
		}

		private void OnLoaded(object sender, RoutedEventArgs e)
		{
			// TODO figure out how to do this in xaml. Problem is NextDisplay property is null before InitializeComponent()
			// and then it blows up making the ui:ControlEditor trying to assign null for ActionName.
			nextButtonEditor.ActionName = NextDisplay;
			previousButtonEditor.ActionName = PreviousDisplay;
			peekCurrentButtonEditor.ActionName = PeekCurrentDisplay;

			// TODO and here. "peekModeContainer" is an x:Name give to the ItemsControl for the list of segment display modes.
			// So then search for all ui:ControlEditors in that ItemsControl to set their ActionName.
			var deviceInstance = (DeviceInstance)DataContext;
			foreach (var item in peekModeContainer.Items)
			{
				var contentPresenter = (ContentPresenter)peekModeContainer.ItemContainerGenerator.ContainerFromItem(item);

				// contentPresenter can be null but OnLoaded will be invoked again later when it's not null.
				if (contentPresenter != null)
				{
					var controlsEditor =
						(SimHub.Plugins.UI.ControlsEditor)contentPresenter.ContentTemplate.FindName("peekMode", contentPresenter);
					var modeName = ((SegmentDisplayManager.SegmentDisplayMode)controlsEditor.DataContext).FriendlyName;
					controlsEditor.ActionName = deviceInstance.MakeActionNameFQ(deviceInstance.MakeActionName(modeName));
				}
			}
		}
	}

	/// <summary>Convert a segment display name to a tool tip for the peek control editor.</summary>
	public sealed class SegmentDisplayFriendlyNameToPeekControlEditorToolTip : IValueConverter
	{
		/// <inheritdoc/>
		public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
		{
			return String.Format("Assign a control to peek the current value of '{0}'.", value);
		}

		/// <inheritdoc/>
		public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
		{
			return DependencyProperty.UnsetValue;
		}
	}
}
