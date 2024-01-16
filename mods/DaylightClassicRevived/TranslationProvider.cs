using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml.Serialization;
using ColossalFramework.Globalization;
using System.Xml;
using UnityEngine;

namespace DaylightClassicRevived
{
	/// <summary>
	/// Provides translations read from external xml files.
	/// </summary>
	sealed class TranslationProvider : IDisposable
	{
		const string FallbackLanguage = "en";

		readonly List<Language> _languages = new List<Language>();
		readonly XmlSerializer _xmlSerializer;

		Language _currentLanguage;
		bool _isDisposed;

		public TranslationProvider()
		{
			Debug.Log("[DaylighClassicRevived] Initializing TranslationProvider");

			_xmlSerializer = new XmlSerializer(typeof(Language));
			LocaleManager.eventLocaleChanged += SetCurrentLanguage;
		}

		public string GetTranslation(string translationId)
		{
			LoadLanguages();

			if (_currentLanguage == null)
			{
				Debug.LogWarningFormat("[DaylighClassicRevived] Can't get a translation for \"{0}\" as there is not a language defined", translationId);

				return translationId;
			}

			var translation = _currentLanguage.Translations.SingleOrDefault(x =>
				x.Key.Equals(translationId, StringComparison.OrdinalIgnoreCase));

			if (translation is null)
			{
				Debug.LogWarningFormat("[DaylighClassicRevived] Returned translation for language \"{0}\" doesn't contain a suitable translation for \"{1}\"", _currentLanguage.UniqueName, translationId);

				return translationId;
			}

			return translation.Value;
		}

		void LoadLanguages()
		{
			if (_languages.Count > 0)
			{
				return;
			}

			Debug.Log("[DaylighClassicRevived] Loading Languages");

			RefreshLanguages();
			SetCurrentLanguage();
		}

		void RefreshLanguages()
		{
			_languages.Clear();

			var languagePath = Path.Combine(PluginInfoProvider.GetOrResolvePluginInfo().modPath, "Locale");
			var languagesDir = new DirectoryInfo(languagePath);

			if (!languagesDir.Exists)
			{
				Debug.LogWarning("[DaylighClassicRevived] Can't find any language files");
				return;
			}

			foreach (var languageFile in languagesDir.GetFiles("*.xml", SearchOption.TopDirectoryOnly))
			{
				var language = DeserializeLanguageFile(languageFile);

				if (language != null)
				{
					_languages.Add(language);
				}
			}
		}

		void SetCurrentLanguage()
		{
			if (_languages.Count == 0 || !LocaleManager.exists)
			{
				return;
			}

			_currentLanguage = _languages.Find(l =>
				l.UniqueName.Equals(LocaleManager.instance.language, StringComparison.OrdinalIgnoreCase) ||
				l.UniqueName.Equals(FallbackLanguage, StringComparison.OrdinalIgnoreCase));
		}

		Language DeserializeLanguageFile(FileInfo languageFile)
		{
			try
			{
				using (var xmlReader = XmlReader.Create(languageFile.FullName))
				{
					var language = (Language)_xmlSerializer.Deserialize(xmlReader);

					Debug.LogFormat("[DaylighClassicRevived] Loaded language {0} ({1})", language.ReadableName, language.UniqueName);

					return language;
				}
			}
			catch (Exception err)
			{
				Debug.LogErrorFormat("[DaylighClassicRevived] Error deserializing language file {0}: {1}", languageFile.Name, err);
			}

			return null;
		}

		void Dispose(bool disposing)
		{
			if (_isDisposed)
			{
				return;
			}

			if (disposing)
			{
				LocaleManager.eventLocaleChanged -= SetCurrentLanguage;
			}

			_isDisposed = true;
		}

		public void Dispose()
		{
			// Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
			Dispose(disposing: true);
			GC.SuppressFinalize(this);
		}
	}
}
