﻿/*
 * Code-behind for segment display control UI.
 */

using System.Collections;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;

// ---------------------------------------------------------------------------------------------------------------------------------

namespace SimElation.SimHubIntegration.SliProPlugin
{
	/// <summary>Interaction logic for SegmentDisplayControl.xaml.</summary>
	public partial class SegmentDisplayControl : UserControl
	{
		private static readonly DependencyProperty LabelProperty =
			DependencyProperty.Register("Label", typeof(string), typeof(SegmentDisplayControl));

		private static readonly DependencyProperty IsRotaryControlledProperty =
			DependencyProperty.Register("IsRotaryControlled", typeof(bool), typeof(SegmentDisplayControl));

		private static readonly DependencyProperty LearnRotaryButtonContentProperty =
			DependencyProperty.Register("LearnRotaryButtonContent", typeof(string), typeof(SegmentDisplayControl));

		private static readonly DependencyProperty ItemsSourceProperty =
			DependencyProperty.Register("ItemsSource", typeof(IEnumerable), typeof(SegmentDisplayControl));

		private static readonly DependencyProperty SelectedIndexProperty =
			DependencyProperty.Register("SelectedIndex", typeof(int), typeof(SegmentDisplayControl));

		private static readonly DependencyProperty NextDisplayProperty =
			DependencyProperty.Register("NextDisplay", typeof(string), typeof(SegmentDisplayControl));

		private static readonly DependencyProperty PreviousDisplayProperty =
			DependencyProperty.Register("PreviousDisplay", typeof(string), typeof(SegmentDisplayControl));

		private static readonly RoutedEvent LearnRotaryClickEvent = ButtonBase.ClickEvent.AddOwner(typeof(SegmentDisplayControl));

		/// <summary>Title property for segment display (e.g. "Left segment", "Right segment").</summary>
		public string Label
		{
			get => GetValue(LabelProperty) as string;
			set => SetValue(LabelProperty, value);
		}

		/// <summary>Is the display rotary controlled?</summary>
		public bool IsRotaryControlled
		{
			get => (bool)GetValue(IsRotaryControlledProperty);
			set => SetValue(IsRotaryControlledProperty, value);
		}

		/// <summary>Content for the learn rotary button.</summary>
		public string LearnRotaryButtonContent
		{
			get => GetValue(LearnRotaryButtonContentProperty) as string;
			set => SetValue(LearnRotaryButtonContentProperty, value);
		}

		/// <summary>Property for an enumerable of items for the drop down (the various displays available).</summary>
		public IEnumerable ItemsSource
		{
			get => GetValue(ItemsSourceProperty) as IEnumerable;
			set => SetValue(ItemsSourceProperty, value);
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

		/// <summary>Control loaded callback.</summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		public void OnLoaded(object sender, RoutedEventArgs e)
		{
			// TODO figure out how to do this in xaml. Problem is NextDisplay property is null before InitializeComponent()
			// and then it blows up making the ui:ControlEditor trying to assign null for ActionName.
			nextButtonEditor.ActionName = NextDisplay;
			previousButtonEditor.ActionName = PreviousDisplay;
		}
	}
}