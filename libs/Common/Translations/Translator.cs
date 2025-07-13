using System;
using System.Globalization;
using System.Resources;

namespace Common.Translations
{
	public interface ITranslator
	{
		void SetCurrentLanguage(string languageTag);
		string GetTranslation(string translationId);
	}

	public sealed class Translator : ITranslator
	{
		readonly ResourceManager _resources;
		string _currentLanguage = "en";

		public Translator(ModData mod)
		{
			_resources = ResourceManager.CreateFileBasedResourceManager("strings", mod.Directory.CreateSubdirectory("Assets").CreateSubdirectory("Locales").FullName, null);
		}

		public void SetCurrentLanguage(string languageTag)
		{
			if (string.IsNullOrEmpty(languageTag))
			{
				throw new ArgumentException("String is null or empty", languageTag);
			}

			if (languageTag.Equals("zh", StringComparison.OrdinalIgnoreCase))
			{
				languageTag = "zh-cn";
			}

			_currentLanguage = languageTag;
		}

		public string GetTranslation(string translationId)
		{
			if (string.IsNullOrEmpty(translationId))
			{
				throw new ArgumentException("String is null or empty", translationId);
			}

			var culture = CultureInfo.GetCultureInfoByIetfLanguageTag(_currentLanguage);

			return _resources.GetString(translationId, culture);
		}
	}
}
