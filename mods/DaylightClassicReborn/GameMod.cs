using System.Linq;
using ColossalFramework.UI;
using OptionsFramework.Extensions;
using ICities;
using OptionsFramework;
using UnityEngine;
using System;

namespace DaylightClassicReborn
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

		public GameMod()
		{
			_translationProvider = new TranslationProvider();
		}

		public static void AllToClassic()
		{
			foreach (var uiCheckBox in _checkBoxes)
			{
				uiCheckBox.isChecked = true;
			}
		}

		public static void AllToAfterDark()
		{
			foreach (var uiCheckBox in _checkBoxes)
			{
				uiCheckBox.isChecked = false;
			}
		}

		public override void OnLevelLoaded(LoadMode mode)
		{
			base.OnLevelLoaded(mode);

			DaylightClassic.SetUp();
		}

		public override void OnLevelUnloading()
		{
			base.OnLevelUnloading();

			DaylightClassic.CleanUp();
		}

		public void OnSettingsUI(UIHelperBase helper)
		{
			var components = helper.AddOptionsGroup(XmlOptionsWrapper<Options>.Instance, s => _translationProvider.GetTranslation(s));
			_checkBoxes = components.OfType<UICheckBox>().ToArray();
		}

		public void OnDisabled()
		{
			_translationProvider.Dispose();
		}
	}
}
