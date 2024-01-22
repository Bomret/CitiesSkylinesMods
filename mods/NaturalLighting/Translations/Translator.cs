using System.Resources;
using System.Globalization;
using System.Diagnostics;
using ColossalFramework.Globalization;

namespace NaturalLighting
{
	interface ITranslator
	{
		void SetCurrentLanguage(string languageTag);
		string GetTranslation(string translationId);
	}

	sealed class Translator : ITranslator
	{
		string _currentLanguage;
		readonly ResourceManager _resources;

		public Translator(ResourceManager resources)
		{
			_resources = resources;
		}

		public void SetCurrentLanguage(string languageTag)
		{
			UnityEngine.Debug.LogFormat("[NaturalLighting] SetCurrentLanguage {0}", languageTag);

			_currentLanguage = languageTag;
		}

		public string GetTranslation(string translationId)
		{
			UnityEngine.Debug.LogFormat("[NaturalLighting] GetTranslation {0}", translationId);

			return _resources.GetString(translationId);
		}
	}
}
