/*
 * Device description.
 */

using System;
using System.Collections.Generic;

// ---------------------------------------------------------------------------------------------------------------------------------

namespace SimElation.SliDevices
{
	/// <summary>Interface describing a device.</summary>
	public interface IDescriptor
	{
		/// <summary>Various device-specific constant values (number of LEDs, etc.).</summary>
		IConstants Constants { get; }

		/// <summary>LED state report format for sending to a device.</summary>
		ILedStateReport LedStateReport { get; }

		/// <summary>LED brightness report format for sending to a device.</summary>
		IBrightnessReport BrightnessReport { get; }

		/// <summary>Input report format from a device.</summary>
		IInputReport InputReport { get; }
	}

	/// <summary>Concrete class describing a device.</summary>
	/// <typeparam name="TConstants">Constants describing the device.</typeparam>
	/// <typeparam name="TLedStateReport">LED state report format.</typeparam>
	/// <typeparam name="TBrightnessReport">LED brightness report format.</typeparam>
	/// <typeparam name="TInputReport">Input report format.</typeparam>
	public sealed class Descriptor<TConstants, TLedStateReport, TBrightnessReport, TInputReport> : IDescriptor
		where TConstants : IConstants, new()
		where TLedStateReport : ILedStateReport, new()
		where TBrightnessReport : IBrightnessReport, new()
		where TInputReport : IInputReport, new()
	{
		/// <inheritdoc/>
		public IConstants Constants { get; } = new TConstants();

		/// <inheritdoc/>
		public ILedStateReport LedStateReport { get; } = new TLedStateReport();

		/// <inheritdoc/>
		public IBrightnessReport BrightnessReport { get; } = new TBrightnessReport();

		/// <inheritdoc/>
		public IInputReport InputReport { get; } = new TInputReport();
	}
}

// ---------------------------------------------------------------------------------------------------------------------------------

namespace SimElation.SliDevices
{
	using F1Descriptor = Descriptor<F1.Constants, F1.LedStateReport, F1.BrightnessReport, F1.InputReport>;
	using ProDescriptor = Descriptor<Pro.Constants, Pro.LedStateReport, Pro.BrightnessReport, Pro.InputReport>;

	/// <summary>Singleton of device descriptors.</summary>
	public sealed class Descriptors
	{
		/// <summary>Singleton instance.</summary>
		public static Descriptors Instance { get => m_instance.Value; }

		/// <summary>SLI-Pro descriptor.</summary>
		public ProDescriptor ProDescriptor { get; } = new ProDescriptor();

		/// <summary>SLI-F1 descriptor.</summary>
		public F1Descriptor F1Descriptor { get; } = new F1Descriptor();

		/// <summary>Dictionary of descriptors, indexed by USB product id.</summary>
		public Dictionary<int, IDescriptor> Dictionary { get => m_dictionary; }

		/// <summary>Array of supported product ids.</summary>
		public int[] ProductIds { get => m_productIds; }

		private static readonly Lazy<Descriptors> m_instance = new Lazy<Descriptors>(() => new Descriptors());
		private readonly Dictionary<int, IDescriptor> m_dictionary;
		private readonly int[] m_productIds;

		private Descriptors()
		{
			m_dictionary =
				new Dictionary<int, IDescriptor>()
				{
					{ Pro.Constants.CompileTime.productId, ProDescriptor },
					{ F1.Constants.CompileTime.productId, F1Descriptor }
				};

			m_productIds = new int[m_dictionary.Keys.Count];
			m_dictionary.Keys.CopyTo(m_productIds, 0);
		}
	}
}
