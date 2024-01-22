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
		readonly string _settingsFilePath;
		readonly XmlSerializer _xmlSerializer;

		ModSettings _settings;

		public ModSettingsStore(string settingsFilePath)
		{
			_settingsFilePath = settingsFilePath;
			_xmlSerializer = new XmlSerializer(typeof(ModSettings));
		}

		public static ModSettingsStore Create(string modName)
		{
			var settingsFile = Path.Combine(DataLocation.localApplicationData, $"{modName}.xml");

			return new ModSettingsStore(settingsFile);
		}

		public ModSettings GetOrLoadSettings()
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
						_settings = (ModSettings)_xmlSerializer.Deserialize(reader);
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
			_settings = new ModSettings();

			SaveSettings();
		}
	}
}