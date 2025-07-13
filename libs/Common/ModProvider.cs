using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using ColossalFramework.Plugins;

namespace Common
{
	public interface IModProvider
	{
		ModData GetCurrentMod();
		ReadOnlyCollection<ModData> GetLoadedMods();
	}

	public sealed class ModProvider<TCurrentMod> : IModProvider
		where TCurrentMod : class
	{
		public ModData GetCurrentMod()
		{
			var plugin = PluginManager.instance.FindPluginInfo(typeof(TCurrentMod).Assembly);

			return ToModInfo(plugin);
		}

		public ReadOnlyCollection<ModData> GetLoadedMods()
		{
			var loadedMods = new List<ModData>();
			foreach (var mod in PluginManager.instance.GetPluginsInfo())
			{
				if (!mod.isEnabled) continue;

				loadedMods.Add(ToModInfo(mod));
			}

			return loadedMods.AsReadOnly();
		}

		static ModData ToModInfo(PluginManager.PluginInfo plugin) => new ModData(
			nameOrSteamId: plugin.name,
			directory: new DirectoryInfo(plugin.modPath),
			isEnabled: () => plugin.isEnabled);
	}
}
