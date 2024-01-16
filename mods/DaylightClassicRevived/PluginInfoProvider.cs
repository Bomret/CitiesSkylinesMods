using System;
using System.Linq;
using ColossalFramework.Plugins;
using ICities;

namespace DaylightClassicRevived
{
	public static class PluginInfoProvider
	{
		static PluginManager.PluginInfo _info;

		public static PluginManager.PluginInfo GetOrResolvePluginInfo()
		{
			if (_info != null)
			{
				return _info;
			}

			return ResolveMod();
		}

		static PluginManager.PluginInfo ResolveMod()
		{
			foreach (var plugin in PluginManager.instance.GetPluginsInfo())
			{
				if (plugin.GetInstances<IUserMod>().Any(mod => mod is GameMod))
				{
					_info = plugin;

					return _info;
				}
			}

			throw new InvalidOperationException("[DaylighClassicRevived] Failed to find DaylightClassicRevived assembly");
		}
	}
}