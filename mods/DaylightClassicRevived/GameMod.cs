using System.Linq;
using ColossalFramework.UI;
using OptionsFramework.Extensions;
using ICities;
using OptionsFramework;
using UnityEngine;
using System;

namespace DaylightClassicRevived
{
#pragma warning disable CA1001 // Types that own disposable fields should be disposable
	public sealed class GameMod : LoadingExtensionBase, IUserMod
#pragma warning restore CA1001 // Types that own disposable fields should be disposable
	{
		public string Name => "Daylight Classic Revived";
		public string Description => $"{_translationProvider.GetTranslation("DC_DESCRIPTION")}, version {Version}";

		const string Version = "1.13.0";
		readonly TranslationProvider _translationProvider;
		static UICheckBox[] _checkBoxes;
		static bool _crashed;

		public GameMod()
		{
			_translationProvider = new TranslationProvider();
		}

		public static void AllToClassic()
		{
			if (_crashed) return;

			foreach (var uiCheckBox in _checkBoxes)
			{
				uiCheckBox.isChecked = true;
			}
		}

		public static void AllToAfterDark()
		{
			if (_crashed) return;

			foreach (var uiCheckBox in _checkBoxes)
			{
				uiCheckBox.isChecked = false;
			}
		}

		public override void OnLevelLoaded(LoadMode mode)
		{
			base.OnLevelLoaded(mode);

			if (_crashed) return;

			DaylightClassic.SetUp();
		}

		public override void OnLevelUnloading()
		{
			base.OnLevelUnloading();

			if (_crashed) return;

			DaylightClassic.CleanUp();
		}

		public void OnSettingsUI(UIHelperBase helper)
		{
			if (_crashed) return;

			var components = helper.AddOptionsGroup(XmlOptionsWrapper<Options>.Instance, s => _translationProvider.GetTranslation(s));
			_checkBoxes = components.OfType<UICheckBox>().ToArray();
		}

		public void OnDisabled()
		{
			if (_crashed) return;

			_translationProvider.Dispose();
		}
	}
}
