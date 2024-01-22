using System.Collections.Generic;
using ColossalFramework.Plugins;
using System.Collections.ObjectModel;
using System.IO;

namespace NaturalLighting
{
	interface IModProvider
	{
		ModInfo GetCurrentMod();
		ReadOnlyCollection<ModInfo> GetLoadedMods();
	}

	sealed class ModProvider<TCurrentMod> : IModProvider
		where TCurrentMod : class
	{
		public ModInfo GetCurrentMod()
		{
			var plugin = PluginManager.instance.FindPluginInfo(typeof(TCurrentMod).Assembly);

			return ToModInfo(plugin);
		}

		public ReadOnlyCollection<ModInfo> GetLoadedMods()
		{
			var loadedMods = new List<ModInfo>();
			foreach (var mod in PluginManager.instance.GetPluginsInfo())
			{
				if (!mod.isEnabled) continue;

				loadedMods.Add(ToModInfo(mod));
			}

			return loadedMods.AsReadOnly();
		}

		static ModInfo ToModInfo(PluginManager.PluginInfo plugin) => new ModInfo(
			nameOrSteamId: plugin.name,
			directory: new DirectoryInfo(plugin.modPath),
			isEnabled: () => plugin.isEnabled);
	}
}
