﻿using System;
using ICities;
using UnityEngine;
using NaturalLighting.Replacer;
using NaturalLighting.Settings;
using System.Collections.Generic;

namespace NaturalLighting
{
#pragma warning disable CA1001 // Types that own disposable fields should be disposable
	public sealed class GameMod : LoadingExtensionBase, IUserMod
#pragma warning restore CA1001 // Types that own disposable fields should be disposable
	{
		public string Name => "Natural Lighting";
		public string Description => $"Adjusts in-game lighting to look more natural, version {Version}\nby Bomret";
		const string Version = "1.0.0";

		readonly List<Replacer<NaturalLightingSettings>> _replacers;
		readonly TranslatorProvider _translatorProvider;
		readonly ModSettingsStore _settingsStore;

		bool _inGame;

		public GameMod()
		{
			_translatorProvider = new TranslatorProvider();
			_settingsStore = ModSettingsStore.GetOrCreate();

			_replacers = new List<Replacer<NaturalLightingSettings>>() {
				new SunlightReplacer(Debug.logger),
				new EquatorColorReplacer(Debug.logger)
			};
		}

		public void OnSettingsUI(UIHelperBase settingsUi)
		{
			if (settingsUi is null) throw new ArgumentNullException(nameof(settingsUi));

			var translator = _translatorProvider.GetOrCreate();
			var settings = _settingsStore.GetOrLoadSettings();

			var generalSettings = settingsUi.AddGroup(translator.GetTranslation("NL_GENERAL_SETTINGS"));

			generalSettings.AddCheckbox(translator.GetTranslation("NL_USE_NATURAL_SUNLIGHT"), settings.UseNaturalSunlight, b =>
			{
				settings.UseNaturalSunlight = b;
				NotifySettingChanged(settings);
			});

			generalSettings.AddCheckbox(translator.GetTranslation("NL_USE_SOFTER_SHADOWS"), settings.UseSofterShadows, b =>
			{
				settings.UseSofterShadows = b;
				NotifySettingChanged(settings);
			});
		}

		public override void OnLevelLoaded(LoadMode mode)
		{
			Debug.Log("[NaturalLighting] Starting...");

			var settings = _settingsStore.GetOrLoadSettings();

			foreach (var replacer in _replacers)
			{
				replacer.OnLoaded(settings);
			}

			_inGame = true;
		}

		public override void OnLevelUnloading()
		{
			Debug.Log("[NaturalLighting] Tearing down...");

			foreach (var replacer in _replacers)
			{
				replacer.OnUnloading();
			}

			_settingsStore.SaveSettings();

			_inGame = false;
		}

		public void OnDisabled()
		{
			Debug.Log("[NaturalLighting] Disabling...");

			foreach (var replacer in _replacers)
			{
				replacer.Dispose();
			}
		}

		void NotifySettingChanged(NaturalLightingSettings settings)
		{
			_settingsStore.SaveSettings();

			if (!_inGame) return;

			foreach (var replacer in _replacers)
			{
				replacer.OnSettingsChanged(settings);
			}
		}
	}
}