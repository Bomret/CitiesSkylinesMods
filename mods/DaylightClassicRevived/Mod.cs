using System.Linq;
using ColossalFramework.UI;
using DaylightClassic.TranslationFramework;
using OptionsFramework.Extensions;
using ICities;
using OptionsFramework;

namespace DaylightClassic
{
	public sealed class GameMod : IUserMod
	{
		const string Version = "1.13.0";
		static UICheckBox[] _checkBoxes;
		static readonly Translation translation = new Translation();

		public string Name => "Daylight Classic Revived";
		public string Description => $"{translation.GetTranslation("DC_DESCRIPTION")}, version {Version}";

#pragma warning disable CA1822 // Mark members as static
		public void OnSettingsUI(UIHelperBase helper)
#pragma warning restore CA1822 // Mark members as static
		{
			var components = helper.AddOptionsGroup(XmlOptionsWrapper<Options>.Instance, s => translation.GetTranslation(s));
			_checkBoxes = components.OfType<UICheckBox>().ToArray();
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
	}
}
