/*
 * Extended device description with SliPlugin-specific details.
 */

using System;
using System.Collections.Generic;
using System.Windows.Media;

// ---------------------------------------------------------------------------------------------------------------------------------

namespace SimElation.Simhub.SliPlugin
{
	/// <summary>Extended device description with SliPlugin-specific details.</summary>
	/// <remarks>
	/// For factoring out segment display formatting (6 vs 4 chars difference) and status LED color differences.
	/// </remarks>
	public interface ISliPluginDeviceDescriptor
	{
		/// <summary>The plugin-agnostic device descriptor.</summary>
		SliDevices.IDeviceDescriptor DeviceDescriptor { get; }

		/// <summary>Output formatters interface.</summary>
		IOutputFormatters OutputFormatters { get; }

		/// <summary>Left status LEDs.</summary>
		Led[] LeftStatusLeds { get; }

		/// <summary>Right status LEDs.</summary>
		Led[] RightStatusLeds { get; }
	}

	/// <summary>Singleton of supported device descriptors.</summary>
	public sealed class SliPluginDeviceDescriptors
	{
		/// <summary>Singleton instance.</summary>
		public static SliPluginDeviceDescriptors Instance { get => m_instance.Value; }

		/// <summary>Dictionary of descriptors, indexed by USB product id.</summary>
		public Dictionary<int, ISliPluginDeviceDescriptor> Dictionary { get => m_dictionary; }

		private static readonly Lazy<SliPluginDeviceDescriptors> m_instance =
			new Lazy<SliPluginDeviceDescriptors>(() => new SliPluginDeviceDescriptors());
		private readonly Dictionary<int, ISliPluginDeviceDescriptor> m_dictionary;

		private SliPluginDeviceDescriptors()
		{
			m_dictionary =
				new Dictionary<int, ISliPluginDeviceDescriptor>()
				{
					{ SliDevices.Pro.Constants.CompileTime.productId, new SliProDeviceDescriptor() },
					{ SliDevices.F1.Constants.CompileTime.productId, new SliF1DeviceDescriptor() }
				};
		}
	}

	// SLI-Pro descriptor.
	sealed class SliProDeviceDescriptor : ISliPluginDeviceDescriptor
	{
		public SliDevices.IDeviceDescriptor DeviceDescriptor { get; } = s_descriptor;
		public IOutputFormatters OutputFormatters { get; } = new SliProFormatters();
		public Led[] LeftStatusLeds { get; } =
			new Led[]
			{
				MakeStatusLed.Run(s_descriptor.Constants.LeftStatusLedColors[0], "Left status LED 1",
					String.Format("if ([Flag_Blue], {0}, {1})", (int) Led.State.on, (int) Led.State.off)),

				MakeStatusLed.Run(s_descriptor.Constants.LeftStatusLedColors[1], "Left status LED 2",
					String.Format("if ([Flag_Yellow], {0}, {1})", (int) Led.State.on, (int) Led.State.off)),

				MakeStatusLed.Run(s_descriptor.Constants.LeftStatusLedColors[2], "Left status LED 3",
					String.Format("if ([CarSettings_FuelAlertActive], {0}, {1})", (int) Led.State.blink, (int) Led.State.off))
			};
		public Led[] RightStatusLeds { get; } =
			new Led[]
			{
				MakeStatusLed.Run(s_descriptor.Constants.RightStatusLedColors[0], "Right status LED 1",
					String.Format("if ([ABSActive], {0}, {1})", (int) Led.State.on, (int) Led.State.off)),

				MakeStatusLed.Run(s_descriptor.Constants.RightStatusLedColors[1], "Right status LED 2",
					String.Format("if ([TCActive], {0}, {1})", (int) Led.State.on, (int) Led.State.off)),

				MakeStatusLed.Run(s_descriptor.Constants.RightStatusLedColors[2], "Right status LED 3",
					// Flash for DRS available (to get attention!), solid for DRS on.
					// NB rf2 (at least?) reports DRS available in the pits (in a practise session), so ignore DRS state if in pit.
					String.Format("if (([IsInPitLane] || [IsInPit]), {2}, if ([DRSEnabled], {0}, if ([DRSAvailable], {1}, {2})))",
						(int) Led.State.on, (int) Led.State.blink, (int) Led.State.off))
			};

		private static SliDevices.IDeviceDescriptor s_descriptor =
			SliDevices.DeviceDescriptors.Instance.Dictionary[SliDevices.Pro.Constants.CompileTime.productId];
	}

	// SLI-F1 descriptor.
	sealed class SliF1DeviceDescriptor : ISliPluginDeviceDescriptor
	{
		public SliDevices.IDeviceDescriptor DeviceDescriptor { get; } = s_descriptor;
		public IOutputFormatters OutputFormatters { get; } = new SliF1Formatters();
		public Led[] LeftStatusLeds { get; } =
			new Led[]
			{
				MakeStatusLed.Run(s_descriptor.Constants.LeftStatusLedColors[0], "Left status LED 1",
					String.Format("if ([Flag_Blue], {0}, {1})", (int) Led.State.on, (int) Led.State.off)),

				MakeStatusLed.Run(s_descriptor.Constants.LeftStatusLedColors[1], "Left status LED 2",
					String.Format("if ([Flag_Yellow], {0}, {1})", (int) Led.State.on, (int) Led.State.off)),

				MakeStatusLed.Run(s_descriptor.Constants.LeftStatusLedColors[2], "Left status LED 3",
					String.Format("if ([CarSettings_FuelAlertActive], {0}, {1})", (int) Led.State.blink, (int) Led.State.off))
			};
		public Led[] RightStatusLeds { get; } =
			new Led[]
			{
				MakeStatusLed.Run(s_descriptor.Constants.RightStatusLedColors[0], "Right status LED 1",
					String.Format("if ([ABSActive], {0}, {1})", (int) Led.State.on, (int) Led.State.off)),

				MakeStatusLed.Run(s_descriptor.Constants.RightStatusLedColors[1], "Right status LED 2",
					String.Format("if ([TCActive], {0}, {1})", (int) Led.State.on, (int) Led.State.off)),

				MakeStatusLed.Run(s_descriptor.Constants.RightStatusLedColors[2], "Right status LED 3",
					// Flash for DRS available (to get attention!), solid for DRS on.
					// NB rf2 (at least?) reports DRS available in the pits (in a practise session), so ignore DRS state if in pit.
					String.Format("if (([IsInPitLane] || [IsInPit]), {2}, if ([DRSEnabled], {0}, if ([DRSAvailable], {1}, {2})))",
						(int) Led.State.on, (int) Led.State.blink, (int) Led.State.off))
			};

		private static SliDevices.IDeviceDescriptor s_descriptor =
			SliDevices.DeviceDescriptors.Instance.Dictionary[SliDevices.F1.Constants.CompileTime.productId];
	}

	static class MakeStatusLed
	{
		public static Led Run(Color color, String title, String formula)
		{
			var statusLed =
				new Led(color)
				{
					ExpressionValue = formula,
					EditPropertyName = title
				};

			return statusLed;
		}
	}
}
