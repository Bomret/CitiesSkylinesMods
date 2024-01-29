using System;
using ICities;
using UnityEngine;
using NaturalLighting.Features;
using NaturalLighting.Settings;
using System.Collections.Generic;
using System.Linq;
using System.Collections.ObjectModel;
using ColossalFramework.Globalization;
using System.Globalization;
using System.Reflection;
using ColossalFramework.UI;

namespace NaturalLighting
{
#pragma warning disable CA1001 // Types that own disposable fields should be disposable
	public sealed class GameMod : LoadingExtensionBase, IUserMod
#pragma warning restore CA1001 // Types that own disposable fields should be disposable
	{
		public string Name => $"{_modName} {_version} - BETA";
		public string Description => $"Adjusts in-game lighting to look more natural.\nby Bomret";

		readonly string _modName = "Natural Lighting";
		readonly string _version = Assembly.GetExecutingAssembly().GetName().Version.ToString(3);

		static readonly Dictionary<string, string> IncompatibleMods = new Dictionary<string, string>
		{
			{"530871278", "Daylight Classic"},
			{"1794015399", "Render It!"},
			{"2983036781", "Lumina"},
			{"1899640536", "Theme Mixer 2"},
			{"3031623332", "Theme Mixer 2.5"},
		};
		readonly ModProvider<GameMod> _modProvider;
		readonly ModSettingsStore _settingsStore;
		readonly List<Feature<ModSettings>> _features;

		bool _isInGame;
		bool _isModSetup;
		Translator _translator;

		public GameMod()
		{
			_modProvider = new ModProvider<GameMod>();
			_settingsStore = ModSettingsStore.Create(_modName);

			_features = new List<Feature<ModSettings>>() {
				new NaturalSunlight(Debug.logger),
				new SofterShadowsOnBuildings(Debug.logger),
				new LutReplacer(_modProvider, Debug.logger)
			};
		}

		public void OnEnabled()
		{
			var mod = _modProvider.GetCurrentMod();

			_translator = new Translator(mod);
		}

		public void OnSettingsUI(UIHelperBase settingsUi)
		{
			if (settingsUi is null) throw new ArgumentNullException(nameof(settingsUi));

			_translator.SetCurrentLanguage(LocaleManager.instance.language);
			var settings = _settingsStore.GetOrLoadSettings();

			var generalSettings = settingsUi.AddGroup(_translator.GetTranslation(LocaleStrings.GeneralSettings));
			var useNaturalSunlight = (UICheckBox)generalSettings.AddCheckbox(_translator.GetTranslation(LocaleStrings.UseNaturalSunlight), settings.UseNaturalSunlight, b =>
			{
				settings.UseNaturalSunlight = b;
				NotifySettingChanged(settings);
			});

			var useSofterShadowsOnBuildings = (UICheckBox)generalSettings.AddCheckbox(_translator.GetTranslation(LocaleStrings.UseSofterShadowsOnBuildings), settings.UseSofterShadowsOnBuildings, b =>
			{
				settings.UseSofterShadowsOnBuildings = b;
				NotifySettingChanged(settings);
			});

			var useOwnLut = (UICheckBox)generalSettings.AddCheckbox(_translator.GetTranslation(LocaleStrings.UseOwnLut), settings.UseOwnLut, b =>
			{
				settings.UseOwnLut = b;
				NotifySettingChanged(settings);
			});

			var incompatibleMods = DetectIncompatibleMods();
			if (incompatibleMods.Count > 0 && !settings.IgnoreIncompatibleMods)
			{
				Debug.LogFormat("[NaturalLighting] Detected incompatible mods {0}.", string.Join(", ", incompatibleMods.ToArray()));

				useNaturalSunlight.isEnabled = false;
				useSofterShadowsOnBuildings.isEnabled = false;
				useOwnLut.isEnabled = false;

				var warningMessage = _translator.GetTranslation(LocaleStrings.IncompatibleModDetected);
				var warning = settingsUi.AddGroup(string.Format(CultureInfo.InvariantCulture, warningMessage, incompatibleMods[0]));

				warning.AddCheckbox(_translator.GetTranslation(LocaleStrings.EnableAnyway), settings.IgnoreIncompatibleMods, b =>
				{
					settings.IgnoreIncompatibleMods = b;
					useNaturalSunlight.isEnabled = b;
					useSofterShadowsOnBuildings.isEnabled = b;
					useOwnLut.isEnabled = b;

					NotifySettingChanged(settings);
				});
			}
		}

		public override void OnLevelLoaded(LoadMode mode)
		{
			_isInGame = true;

			var settings = _settingsStore.GetOrLoadSettings();

			var incompatibleMods = DetectIncompatibleMods();
			if (incompatibleMods.Count > 0 && !settings.IgnoreIncompatibleMods)
			{
				Debug.LogFormat("[NaturalLighting] Detected incompatible mods {0}. Disabling settings...", string.Join(", ", incompatibleMods.ToArray()));

				settings = new ModSettings
				{
					UseNaturalSunlight = false,
					UseSofterShadowsOnBuildings = false,
					UseOwnLut = false
				};
			}

			Debug.Log("[NaturalLighting] Starting...");

			foreach (var feature in _features)
			{
				feature.OnLoaded(settings);
			}

			_isModSetup = true;
		}

		public override void OnLevelUnloading()
		{
			_isInGame = false;

			if (!_isModSetup) return;

			Debug.Log("[NaturalLighting] Tearing down...");

			try
			{
				foreach (var feature in _features)
				{
					feature.OnUnloading();
				}
			}
			finally
			{
				_settingsStore.SaveSettings();
			}
		}

		public void OnDisabled()
		{
			if (!_isModSetup) return;

			Debug.Log("[NaturalLighting] Disabling...");

			foreach (var feature in _features)
			{
				feature.Dispose();
			}
		}

		void NotifySettingChanged(ModSettings settings)
		{
			_settingsStore.SaveSettings();

			if (!_isInGame) return;

			var incompatibleMods = DetectIncompatibleMods();
			if (incompatibleMods.Count > 0 && !settings.IgnoreIncompatibleMods)
			{
				Debug.LogFormat("[NaturalLighting] Detected incompatible mods {0}. Disabling settings...", string.Join(", ", incompatibleMods.ToArray()));

				settings = new ModSettings
				{
					UseNaturalSunlight = false,
					UseSofterShadowsOnBuildings = false,
					UseOwnLut = false
				};
			}

			foreach (var feature in _features)
			{
				feature.OnSettingsChanged(settings);
			}
		}

		ReadOnlyCollection<string> DetectIncompatibleMods()
		{
			var incompatibleMods = new List<string>();

			foreach (var mod in _modProvider.GetLoadedMods())
			{
				if (IncompatibleMods.TryGetValue(mod.NameOrSteamId, out var clearName))
				{
					incompatibleMods.Add(clearName);
				}
			}

			return incompatibleMods.AsReadOnly();
		}
	}
}
