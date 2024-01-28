using System.Resources;
using System.Globalization;
using System;

namespace NaturalLighting
{
	interface ITranslator
	{
		void SetCurrentLanguage(string languageTag);
		string GetTranslation(string translationId);
	}

	sealed class Translator : ITranslator
	{
		readonly ResourceManager _resources;
		string _currentLanguage = "en";

		public Translator(ModInfo mod)
		{
			_resources = ResourceManager.CreateFileBasedResourceManager("strings", mod.Directory.CreateSubdirectory("Assets").CreateSubdirectory("Locales").FullName, null);
		}

		public void SetCurrentLanguage(string languageTag)
		{
			if (languageTag.Equals("zh", StringComparison.OrdinalIgnoreCase))
			{
				languageTag = "zh-cn";
			}

			_currentLanguage = languageTag;
		}

		public string GetTranslation(string translationId)
		{
			var culture = CultureInfo.GetCultureInfoByIetfLanguageTag(_currentLanguage);

			return _resources.GetString(translationId, culture);
		}
	}
}
