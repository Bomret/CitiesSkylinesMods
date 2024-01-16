using System;
using ColossalFramework;
using UnityEngine;

namespace DaylightClassicRevived
{
	enum GameEnvironment
	{
		Unknown = 0,
		Europe = 1,
		North = 2,
		Sunny = 3,
		Tropical = 4
	}

	static class GameEnvironmentProvider
	{
		public static GameEnvironment GetInGameEnvironment()
		{
			var _simulationManager = Singleton<SimulationManager>.instance;

			var mMetaData = _simulationManager?.m_metaData;
			if (mMetaData is null) return GameEnvironment.Unknown;

			try
			{
				return (GameEnvironment)Enum.Parse(typeof(GameEnvironment), mMetaData.m_environment);
			}
			catch
			{
				Debug.LogWarningFormat("[DaylighClassicRevived] {0} is not a known game environment. Skipping adjustments", mMetaData.m_environment);
				return GameEnvironment.Unknown;
			}
		}
	}
}