using System;
using System.IO;
using System.Xml;
using System.Xml.Serialization;
using ColossalFramework.IO;
using UnityEngine;

namespace NaturalLighting.Settings
{
	public sealed class ModSettingsStore
	{
		static ModSettingsStore _instance;

		readonly string _settingsFilePath;
		readonly XmlSerializer _xmlSerializer;

		NaturalLightingSettings _settings;

		public ModSettingsStore(string settingsFilePath)
		{
			_settingsFilePath = settingsFilePath;
			_xmlSerializer = new XmlSerializer(typeof(NaturalLightingSettings));
		}

		public static ModSettingsStore GetOrCreate()
		{
			if (_instance != null) return _instance;

			var fullPath = Path.Combine(DataLocation.localApplicationData, "NaturalLighting.xml");

			_instance = new ModSettingsStore(fullPath);

			return _instance;
		}

		public NaturalLightingSettings GetOrLoadSettings()
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
						_settings = (NaturalLightingSettings)_xmlSerializer.Deserialize(reader);
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
			_settings = new NaturalLightingSettings();

			SaveSettings();
		}
	}
}