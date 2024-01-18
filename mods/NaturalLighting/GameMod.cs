using OptionsFramework.Extensions;
using ICities;
using OptionsFramework;
using UnityEngine;
using NaturalLighting.Replacer;

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
		readonly IOptionsStore<Options> _optionsStore;

		GameObject _gameObject;

		public GameMod()
		{
			_translatorProvider = new TranslatorProvider();
			_optionsStore = XmlOptionsStoreProvider.Instance.GetOrCreate<Options>();
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

			_optionsStore.SaveOptions();

			UnityEngine.Object.Destroy(_gameObject);
			_gameObject = null;
		}

		public void OnSettingsUI(UIHelperBase helper)
		{
			helper.AddOptionsGroup(_optionsStore, _translatorProvider.GetOrCreate());
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
