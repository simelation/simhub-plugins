/*
 * Game data with extras from the PersistantTrackerPlugin.
 */

using GameReaderCommon;
using SimHub.Plugins;

// ---------------------------------------------------------------------------------------------------------------------------------

namespace SimElation.Simhub.SliPlugin
{
	/// <summary>Game data with extras from the PersistantTrackerPlugin.</summary>
	public sealed class NormalizedData
	{
		/// <summary>Game data as passed to the plugin's <see cref="IDataPlugin.DataUpdate"/> method.</summary>
		public GameReaderCommon.StatusDataBase StatusData { get => m_statusData; }

		/// <summary>In pit garage or pit lane?</summary>
		/// <remarks>
		/// AMS2 note: both these are false when leaving garage!
		/// </remarks>
		public bool IsInPit { get => m_isInPit; }

		/// <summary>
		/// <see cref="StatusDataBase.DeltaToSessionBest"/> if non-null,
		/// otherwise PersistantTrackerPlugin.SessionBestLiveDeltaSeconds.
		/// </summary>
		public double? DeltaToSessionBest { get => m_deltaToSessionBest; }

		/// <summary>
		/// <see cref="StatusDataBase.DeltaToAllTimeBest"/> if non-null,
		/// otherwise PersistantTrackerPlugin.AllTimeBestLiveDeltaSeconds.
		/// </summary>
		public double? DeltaToAllTimeBest { get => m_deltaToAllTimeBest; }

		/// <summary>
		/// <see cref="StatusDataBase.EstimatedFuelRemaingLaps"/> if non-null,
		/// otherwise PersistantTrackerPlugin.EstimatedFuelRemaingLaps.
		/// </summary>
		public double? FuelRemainingLaps { get => m_fuelRemainingLaps; }

		/// <summary>Populate the fields from a game data update.</summary>
		/// <param name="pluginManager"></param>
		/// <param name="statusData"></param>
		public void Populate(PluginManager pluginManager, StatusDataBase statusData)
		{
			m_statusData = statusData;
			m_isInPit = (statusData.IsInPitLane != 0) || (statusData.IsInPit != 0);

			m_deltaToSessionBest = statusData.DeltaToSessionBest ??
				(double?)pluginManager.GetPropertyValue("PersistantTrackerPlugin.SessionBestLiveDeltaSeconds");

			m_deltaToAllTimeBest = statusData.DeltaToAllTimeBest ??
				(double?)pluginManager.GetPropertyValue("PersistantTrackerPlugin.AllTimeBestLiveDeltaSeconds");

			m_fuelRemainingLaps = statusData.EstimatedFuelRemaingLaps ??
				(double?)pluginManager.GetPropertyValue("DataCorePlugin.Computed.Fuel_RemainingLaps");
		}

		private GameReaderCommon.StatusDataBase m_statusData;
		private bool m_isInPit;
		private double? m_deltaToSessionBest;
		private double? m_deltaToAllTimeBest;
		private double? m_fuelRemainingLaps;
	}
}
