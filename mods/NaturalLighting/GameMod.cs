using System;
using ICities;
using UnityEngine;
using NaturalLighting.Replacer;
using NaturalLighting.Settings;

namespace NaturalLighting
{
#pragma warning disable CA1001 // Types that own disposable fields should be disposable
	public sealed class GameMod : LoadingExtensionBase, IUserMod
#pragma warning restore CA1001 // Types that own disposable fields should be disposable
	{
		public string Name => "Natural Lighting";
		public string Description => $"Adjusts in-game lighting to look more natural, version {Version}\n- by Bomret";
		const string Version = "1.0.0";

		readonly TranslatorProvider _translatorProvider;
		readonly ModSettingsStore _settingsStore;

		GameObject _gameObject;

		public GameMod()
		{
			_translatorProvider = new TranslatorProvider();
			_settingsStore = ModSettingsStore.GetOrCreate();
		}

		public override void OnLevelLoaded(LoadMode mode)
		{
			base.OnLevelLoaded(mode);

			Debug.Log("[NaturalLighting] Initializing...");

			_gameObject = new GameObject("NaturalLighting");
			AddChild<EquatorColorReplacer>(_gameObject);
			AddChild<SunlightReplacer>(_gameObject);
		}

		public override void OnLevelUnloading()
		{
			base.OnLevelUnloading();

			if (_gameObject is null) return;

			Debug.Log("[NaturalLighting] Tearing down...");

			_settingsStore.SaveSettings();

			UnityEngine.Object.Destroy(_gameObject);
			_gameObject = null;
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
				_settingsStore.SaveSettings();
			});

			generalSettings.AddCheckbox(translator.GetTranslation("NL_USE_SOFTER_SHADOWS"), settings.UseSofterShadows, b =>
			{
				settings.UseSofterShadows = b;
				_settingsStore.SaveSettings();
			});
		}

		public void OnDisabled() => _translatorProvider.Dispose();

		static T AddChild<T>(GameObject gameObject) where T : Component
		{
			var child = gameObject.GetComponent<T>();
			if (child is null)
			{
				return gameObject.AddComponent<T>();
			}

			return child;
		}
	}
}
