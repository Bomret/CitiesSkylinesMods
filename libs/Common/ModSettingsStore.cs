using System;
using System.IO;
using System.Xml;
using System.Xml.Serialization;
using ColossalFramework.IO;
using UnityEngine;

namespace Common
{
	public sealed class ModSettingsStore<TSettings> where TSettings : class, new()
	{
		readonly string _settingsFilePath;
		readonly XmlSerializer _xmlSerializer;

		TSettings _settings;

		public ModSettingsStore(string settingsFilePath)
		{
			_settingsFilePath = settingsFilePath;
			_xmlSerializer = new XmlSerializer(typeof(TSettings));
		}

		public static ModSettingsStore<TSettings> Create(string modName)
		{
			var settingsFile = Path.Combine(DataLocation.localApplicationData, $"{modName}.xml");

			return new ModSettingsStore<TSettings>(settingsFile);
		}

		public TSettings GetOrLoadSettings()
		{
			if (_settings != null) return _settings;

			if (!File.Exists(_settingsFilePath))
			{
				ResetSettings();
			}
			else
			{
				try
				{
					using (var reader = XmlReader.Create(_settingsFilePath))
					{
						_settings = (TSettings)_xmlSerializer.Deserialize(reader);
					}
				}
				catch (Exception err)
				{
					Debug.LogErrorFormat("Error reading {0}", _settingsFilePath);
					Debug.LogException(err);

					ResetSettings();
				}
			}

			return _settings;
		}

		public void SaveSettings()
		{
			if (_settings is null) return;

			using (var writer = XmlWriter.Create(_settingsFilePath))
			{
				_xmlSerializer.Serialize(writer, _settings);
			}
		}

		void ResetSettings()
		{
			_settings = new TSettings();

			SaveSettings();
		}
	}
}