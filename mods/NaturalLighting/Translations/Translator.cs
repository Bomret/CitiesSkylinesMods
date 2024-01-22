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

		public Translator(ResourceManager resources)
		{
			_resources = resources;
		}

		public void SetCurrentLanguage(string languageTag)
		{
			if (languageTag.Equals("zh", StringComparison.OrdinalIgnoreCase))
			{
				languageTag = "zh-cn";
			}

			UnityEngine.Debug.LogFormat("[NaturalLighting] SetCurrentLanguage {0}", languageTag);

			_currentLanguage = languageTag;
		}

		public string GetTranslation(string translationId)
		{
			var c = CultureInfo.GetCultureInfoByIetfLanguageTag(_currentLanguage);

			UnityEngine.Debug.LogFormat("[NaturalLighting] GetTranslation {0} for language {1} ({2})", translationId, c.Name, _currentLanguage);

			return _resources.GetString(translationId, c);
		}
	}
}
