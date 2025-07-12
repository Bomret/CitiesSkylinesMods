using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using System.Reflection;
using ColossalFramework.Globalization;
using ColossalFramework.UI;
using ICities;
using NaturalLighting.Features;
using NaturalLighting.Features.ChromaticAberration;
using NaturalLighting.Features.SunShafts;
using NaturalLighting.Settings;
using UnityEngine;

namespace NaturalLighting
{
	/// <summary>
	/// Main mod class for Natural Lighting that provides enhanced lighting effects for Cities: Skylines.
	/// 
	/// This mod adjusts various lighting parameters to create more natural and visually appealing lighting,
	/// including sunlight color, shadow softness, color correction LUTs, and sun shaft effects.
	/// It includes compatibility checking to prevent conflicts with other lighting mods.
	/// </summary>
	public sealed class GameMod : LoadingExtensionBase, IUserMod
	{
		/// <summary>
		/// Gets the display name of the mod including version information.
		/// </summary>
		public string Name => $"{_modName} {_version}";

		/// <summary>
		/// Gets the description of the mod displayed in the mod manager.
		/// </summary>
		public string Description => $"Adjusts in-game lighting to look more natural.\nby Bomret";

		readonly string _modName = "Natural Lighting";
		readonly string _version = Assembly.GetExecutingAssembly().GetName().Version.ToString(3);

		/// <summary>
		/// Dictionary of incompatible mod Steam IDs mapped to their display names.
		/// These mods conflict with Natural Lighting's functionality.
		/// </summary>
		static readonly Dictionary<string, string> IncompatibleMods = new Dictionary<string, string>
		{
			{"530871278", "Daylight Classic"},
			{"1794015399", "Render It!"},
			{"2983036781", "Lumina"},
			{"933513277", "Sun Shafts"},
			{"1881187805", "Sun Shafts (Chinese)"},
			{"1138510774", "PostProcessFX - Multi-platform"},
		};
		readonly ModProvider<GameMod> _modProvider;
		private readonly ObjectProvider _serviceProvider;
		readonly ModSettingsStore _settingsStore;
		readonly List<Feature<ModSettings>> _features;

		bool _isInGame;
		bool _isModSetup;

		ModInfo _mod;
		private Translator _translator;

		/// <summary>
		/// Initializes a new instance of the GameMod class and sets up all lighting features.
		/// </summary>
		public GameMod()
		{
			_settingsStore = ModSettingsStore.Create(_modName);

			_modProvider = new ModProvider<GameMod>();
			_serviceProvider = new ObjectProvider();

			_features = new List<Feature<ModSettings>>() {
				new NaturalSunlightFeature(Debug.logger),
				new SofterShadowsOnBuildingsFeature(Debug.logger),
				new LutReplacerFeature(Debug.logger),
				new SunshaftsFeature(Debug.logger),
				new ChromaticAberrationFeature(Debug.logger)
			};
		}

		/// <summary>
		/// Called when the mod is enabled. Initializes the translation system.
		/// </summary>
		public void OnEnabled()
		{
			_mod = _modProvider.GetCurrentMod();

			_translator = new Translator(_mod);
			_serviceProvider.Register<IShaderProvider>(new ShaderProvider(_mod));
			_serviceProvider.Register<ILutProvider>(new LutProvider(_mod));
		}

		/// <summary>
		/// Creates the mod's settings UI in the game's options menu.
		/// Includes incompatible mod detection and appropriate warnings.
		/// </summary>
		/// <param name="settingsUi">The UI helper for creating settings controls.</param>
		/// <exception cref="ArgumentNullException">Thrown when settingsUi is null.</exception>
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
			useNaturalSunlight.tooltip = "Makes the sunlight appear more white and natural";

			var useSofterShadowsOnBuildings = (UICheckBox)generalSettings.AddCheckbox(_translator.GetTranslation(LocaleStrings.UseSofterShadowsOnBuildings), settings.UseSofterShadowsOnBuildings, b =>
			{
				settings.UseSofterShadowsOnBuildings = b;
				NotifySettingChanged(settings);
			});
			useSofterShadowsOnBuildings.tooltip = "Makes shadows on the side of buildings appear less harsh and dark";

			var useOwnLut = (UICheckBox)generalSettings.AddCheckbox(_translator.GetTranslation(LocaleStrings.UseOwnLut), settings.UseOwnLut, b =>
			{
				settings.UseOwnLut = b;
				NotifySettingChanged(settings);
			});
			useOwnLut.tooltip = "Use the built-in Natural Lighting LUT";

			var enableSunshafts = (UICheckBox)generalSettings.AddCheckbox(_translator.GetTranslation(LocaleStrings.EnableSunshafts), settings.EnableSunshafts, b =>
			{
				settings.EnableSunshafts = b;
				NotifySettingChanged(settings);
			});
			enableSunshafts.tooltip = "Enable enhanced sunshafts and god ray effects for more dramatic lighting";

			var useChromaticAberration = (UICheckBox)generalSettings.AddCheckbox(_translator.GetTranslation(LocaleStrings.UseChromaticAberration), settings.UseChromaticAberration, b =>
			{
				settings.UseChromaticAberration = b;
				NotifySettingChanged(settings);
			});
			useChromaticAberration.tooltip = "Enable subtle chromatic aberration effect that simulates realistic camera lens distortion";

			var incompatibleMods = DetectIncompatibleMods();
			if (incompatibleMods.Count > 0)
			{
				Debug.LogFormat("[NaturalLighting] Detected incompatible mods {0}.", string.Join(", ", incompatibleMods.ToArray()));

				if (!settings.IgnoreIncompatibleMods)
				{
					useNaturalSunlight.isEnabled = false;
					useSofterShadowsOnBuildings.isEnabled = false;
					useOwnLut.isEnabled = false;
					enableSunshafts.isEnabled = false;
					useChromaticAberration.isEnabled = false;
				}

				var warningMessage = _translator.GetTranslation(LocaleStrings.IncompatibleModDetected);
				var warning = settingsUi.AddGroup(string.Format(CultureInfo.InvariantCulture, warningMessage, incompatibleMods[0]));

				var useAnyway = (UICheckBox)warning.AddCheckbox(_translator.GetTranslation(LocaleStrings.EnableAnyway), settings.IgnoreIncompatibleMods, b =>
				{
					settings.IgnoreIncompatibleMods = b;
					useNaturalSunlight.isEnabled = b;
					useSofterShadowsOnBuildings.isEnabled = b;
					useOwnLut.isEnabled = b;
					enableSunshafts.isEnabled = b;
					useChromaticAberration.isEnabled = b;

					NotifySettingChanged(settings);
				});
				useAnyway.tooltip = "Apply Natural Lighting settings regardless of incompatible mods. Recommended only for experienced players";
			}
		}

		/// <summary>
		/// Called when a game level is loaded. Initializes all lighting features with current settings.
		/// Automatically disables features if incompatible mods are detected and not overridden.
		/// </summary>
		/// <param name="mode">The game loading mode (new game, load game, etc.).</param>
		public override void OnLevelLoaded(LoadMode mode)
		{
			_isInGame = true;

			var settings = _settingsStore.GetOrLoadSettings();
			settings = ApplyIncompatibilityCheck(settings);

			Debug.Log("[NaturalLighting] Starting...");

			foreach (var feature in _features)
			{
				try
				{
					feature.OnLoaded(_serviceProvider, settings);
				}
				catch (Exception ex)
				{
					Debug.LogErrorFormat("[NaturalLighting] Failed to load feature {0}: {1}", feature.GetType().Name, ex.Message);
				}
			}

			_isModSetup = true;
		}

		/// <summary>
		/// Called when a game level is being unloaded. Safely tears down all lighting features.
		/// </summary>
		public override void OnLevelUnloading()
		{
			_isInGame = false;

			if (!_isModSetup) return;

			Debug.Log("[NaturalLighting] Tearing down...");

			foreach (var feature in _features)
			{
				try
				{
					feature.OnUnloading();
				}
				catch (Exception ex)
				{
					Debug.LogErrorFormat("[NaturalLighting] Error unloading feature {0}: {1}", feature.GetType().Name, ex.Message);
				}
			}

			_settingsStore.SaveSettings();
		}

		/// <summary>
		/// Called when the mod is disabled. Properly disposes of all features.
		/// </summary>
		public void OnDisabled()
		{
			if (!_isModSetup) return;

			Debug.Log("[NaturalLighting] Disabling...");

			foreach (var feature in _features)
			{
				try
				{
					feature.Dispose();
				}
				catch (Exception ex)
				{
					Debug.LogErrorFormat("[NaturalLighting] Error disposing feature {0}: {1}", feature.GetType().Name, ex.Message);
				}
			}
		}

		/// <summary>
		/// Notifies all features when settings have changed and saves the settings to disk.
		/// Automatically applies incompatibility checks before notifying features.
		/// </summary>
		/// <param name="settings">The updated settings to apply.</param>
		void NotifySettingChanged(ModSettings settings)
		{
			_settingsStore.SaveSettings();

			if (!_isInGame) return;

			settings = ApplyIncompatibilityCheck(settings);

			foreach (var feature in _features)
			{
				try
				{
					feature.OnSettingsChanged(settings);
				}
				catch (Exception ex)
				{
					Debug.LogErrorFormat("[NaturalLighting] Error notifying feature {0} of setting change: {1}", feature.GetType().Name, ex.Message);
				}
			}
		}

		/// <summary>
		/// Applies incompatibility checks to the provided settings.
		/// If incompatible mods are detected and not overridden, returns disabled settings.
		/// </summary>
		/// <param name="settings">The settings to check for compatibility.</param>
		/// <returns>The original settings if compatible, or disabled settings if incompatible mods are detected.</returns>
		ModSettings ApplyIncompatibilityCheck(ModSettings settings)
		{
			var incompatibleMods = DetectIncompatibleMods();
			if (incompatibleMods.Count > 0 && !settings.IgnoreIncompatibleMods)
			{
				Debug.LogFormat("[NaturalLighting] Detected incompatible mods {0}. Disabling settings...", string.Join(", ", incompatibleMods.ToArray()));

				return new ModSettings
				{
					UseNaturalSunlight = false,
					UseSofterShadowsOnBuildings = false,
					UseOwnLut = false,
					EnableSunshafts = false,
					IgnoreIncompatibleMods = settings.IgnoreIncompatibleMods // Preserve the override setting
				};
			}

			return settings;
		}

		/// <summary>
		/// Detects incompatible mods that are currently loaded.
		/// </summary>
		/// <returns>A read-only collection of display names for detected incompatible mods.</returns>
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
