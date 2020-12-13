/*
 * LED handling.
 */

using System;
using System.Diagnostics;
using System.Windows.Media;
using SimHub.Plugins.OutputPlugins.Dash.GLCDTemplating;
using SimHub.Plugins.OutputPlugins.Dash.TemplatingCommon;
using SimHub.Plugins.OutputPlugins.GraphicalDash.Models;

// ---------------------------------------------------------------------------------------------------------------------------------

namespace SimElation.Simhub.SliPlugin
{
	/// <summary>Class handling LED binding to game properties.</summary>
	public sealed class Led
	{
		/// <summary>SimHub data for binding to game properties / ncalc.</summary>
		public ExpressionValue ExpressionValue { get; set; } = new ExpressionValue();

		/// <summary>For FormulaPicker.EditPropertyName.</summary>
		public String EditPropertyName { get; set; } = "";

		/// <summary>Brush to paint the LED when on.</summary>
		/// <remarks>No JsonIgnoreAttribute as saving the color out to config file simplifies reload.</remarks>
		public Brush SetBrush { get; set; } = Brushes.Blue;

		/// <summary>LED state.</summary>
		public enum State
		{
			/// <summary>LED off.</summary>
			off,

			/// <summary>LED on (solid).</summary>
			on,

			/// <summary>LED blinking.</summary>
			blink
		}

		/// <summary>Blinking state.</summary>
		public enum BlinkState
		{
			/// <summary>In blinking on state.</summary>
			on,

			/// <summary>In blinking off state.</summary>
			off
		}

		/// <summary>Default constructor.</summary>
		/// <remarks>For deserializing config.</remarks>
		public Led()
		{
		}

		/// <summary>Constructor.</summary>
		/// <param name="setColor">Color of the LED when set in the UI.</param>
		public Led(Color setColor)
		{
			SetBrush = new SolidColorBrush(setColor);
		}

		/// <summary>Called on a game data update.</summary>
		/// <param name="ncalcEngine"></param>
		/// <param name="blinkIntervalMs"></param>
		/// <returns>true if this LED should be on.</returns>
		public bool ProcessGameData(NCalcEngineBase ncalcEngine, long blinkIntervalMs)
		{
			bool isSet = false;

			if (!ExpressionValue.IsNone)
			{
				var res = ncalcEngine.ParseValueOrDefault(ExpressionValue, 0);

				State ledState = (Led.State)res;
				isSet = ProcessLedState(ledState, blinkIntervalMs);
			}

			return isSet;
		}

		// Figure out if an LED should be on from a state. Handles blinking.
		private bool ProcessLedState(State ledState, long blinkIntervalMs)
		{
			if (State.blink == ledState)
			{
				return HandleBlink(blinkIntervalMs);
			}
			else
			{
				m_stopwatch.Stop();
				return (State.on == ledState);
			}
		}

		private bool HandleBlink(long blinkIntervalMs)
		{
			if (!m_stopwatch.IsRunning)
			{
				m_stopwatch.Restart();
				m_blinkState = BlinkState.on;
			}

			switch (m_blinkState)
			{
				case BlinkState.on:
					if (m_stopwatch.ElapsedMilliseconds >= blinkIntervalMs)
					{
						m_stopwatch.Restart();
						m_blinkState = BlinkState.off;
					}
					break;

				case BlinkState.off:
					if (m_stopwatch.ElapsedMilliseconds >= blinkIntervalMs)
					{
						m_stopwatch.Restart();
						m_blinkState = BlinkState.on;
					}
					break;
			}

			return (BlinkState.on == m_blinkState);
		}

		private BlinkState m_blinkState;
		private readonly Stopwatch m_stopwatch = new Stopwatch();
	}
}
