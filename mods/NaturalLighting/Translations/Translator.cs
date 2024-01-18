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
		const string FallbackLanguage = "en";

		readonly List<Language> _languages = new List<Language>();
		readonly XmlSerializer _xmlSerializer;
		readonly DirectoryInfo _localesDirectory;

		Language _currentLanguage;
		bool _isDisposed;

		public Translator(DirectoryInfo localesDirectory)
		{
			_localesDirectory = localesDirectory;
			_xmlSerializer = new XmlSerializer(typeof(Language));

			LocaleManager.eventLocaleChanged += SetCurrentLanguage;
		}

		public string GetTranslation(string translationId)
		{
			if (_currentLanguage is null && _languages.Count == 0)
			{
				LoadLanguages();
				SetCurrentLanguage();
			}

			if (_currentLanguage is null) return translationId;

			var translation = _currentLanguage.Translations.SingleOrDefault(x =>
				x.ID.Equals(translationId, StringComparison.OrdinalIgnoreCase));

			if (translation is null)
			{
				Debug.LogWarningFormat("[NaturalLighting] Translator.GetTranslation: Could not find a translation for '{0}' in language '{0}' ({1})", translationId, _currentLanguage.Name, _currentLanguage.Tag);

				return translationId;
			}

			return translation.Value;
		}


		void LoadLanguages()
		{
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
				l.Tag.Equals(LocaleManager.instance.language, StringComparison.OrdinalIgnoreCase) ||
				l.Tag.Equals(FallbackLanguage, StringComparison.OrdinalIgnoreCase));
		}

		Language DeserializeLanguageFile(FileInfo languageFile)
		{
			try
			{
				using (var xmlReader = XmlReader.Create(languageFile.FullName))
				{
					var language = (Language)_xmlSerializer.Deserialize(xmlReader);

					Debug.LogFormat("[NaturalLighting] Translator: Loaded language {0} ({1})", language.Name, language.Tag);

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
