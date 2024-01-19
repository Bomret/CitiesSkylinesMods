using System;
using ColossalFramework.Plugins;

namespace NaturalLighting
{
	static class PluginInfoProvider
	{
		static PluginManager.PluginInfo _info;

		public static PluginManager.PluginInfo GetOrResolvePluginInfo()
		{
			if (_info != null) return _info;

			return ResolveMod();
		}

		static PluginManager.PluginInfo ResolveMod()
		{
			_info = PluginManager.instance.FindPluginInfo(typeof(GameMod).Assembly);
			if (_info is null)
			{
				throw new InvalidOperationException("[NaturalLighting] Failed to find NaturalLighting assembly");
			}

			return _info;
		}
	}
}