using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml.Serialization;
using ColossalFramework.Globalization;
using System.Xml;
using UnityEngine;

namespace NaturalLighting
{
	public interface ITranslator
	{
		string GetTranslation(string translationId);
	}

	/// <summary>
	/// Provides translations read from external xml files.
	/// </summary>
	sealed class Translator : ITranslator, IDisposable
	{
		const string FallbackLanguage = "en-US";

		readonly List<Language> _languages = new List<Language>();
		readonly XmlSerializer _xmlSerializer;
		readonly DirectoryInfo _localesDirectory;

		Language _currentLanguage;
		bool _isDisposed;

		public Translator(DirectoryInfo localesDirectory)
		{
			_xmlSerializer = new XmlSerializer(typeof(Language));
			LocaleManager.eventLocaleChanged += SetCurrentLanguage;
			_localesDirectory = localesDirectory;
		}

		public string GetTranslation(string translationId)
		{
			LoadLanguages();

			if (_currentLanguage == null)
			{
				Debug.LogWarningFormat("[NaturalLighting] Translator: Can't get a translation for {0} as no language is defined", translationId);

				return translationId;
			}

			var translation = _currentLanguage.Translations.SingleOrDefault(x =>
				x.Key.Equals(translationId, StringComparison.OrdinalIgnoreCase));

			if (translation is null)
			{
				Debug.LogWarningFormat("[NaturalLighting] Translator: Returned translation for language {0} ({1}) doesn't contain a suitable translation for {2}", _currentLanguage.ReadableName, _currentLanguage.UniqueName, translationId);

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

			RefreshLanguages();
			SetCurrentLanguage();
		}

		void RefreshLanguages()
		{
			_languages.Clear();

			if (!_localesDirectory.Exists)
			{
				Debug.LogWarning("[NaturalLighting] Translator: Can't find any language files");
				return;
			}

			foreach (var languageFile in _localesDirectory.GetFiles("*.xml", SearchOption.TopDirectoryOnly))
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

					Debug.LogFormat("[NaturalLighting] Translator: Loaded language {0} ({1})", language.ReadableName, language.UniqueName);

					return language;
				}
			}
			catch (Exception err)
			{
				Debug.LogErrorFormat("[NaturalLighting] Translator: Error deserializing language file {0}: {1}", languageFile.Name, err);
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
