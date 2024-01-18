using System;
using System.IO;
using System.Xml;
using System.Xml.Serialization;

namespace OptionsFramework
{
	public sealed class XmlOptionsStore<T> : IOptionsStore<T> where T : class, new()
	{
		readonly string _fileName;
		readonly XmlSerializer _xmlSerializer;

		T _options;

		public XmlOptionsStore(string optionsFileName)
		{
			_fileName = optionsFileName;
			_xmlSerializer = new XmlSerializer(typeof(T));
		}

		public T GetOrLoadOptions()
		{
			Ensure();

			return _options;
		}

		public void SaveOptions()
		{
			using (var streamWriter = new StreamWriter(_fileName))
			{
				_xmlSerializer.Serialize(streamWriter, _options);
			}
		}

		void Ensure()
		{
			if (_options != null) return;

			_options = (T)Activator.CreateInstance(typeof(T));

			LoadOptions();
		}

		void LoadOptions()
		{
			if (!File.Exists(_fileName))
			{
				SaveOptions();
			}

			ReadOptionsFile();
		}

		void ReadOptionsFile()
		{
			using (var reader = XmlReader.Create(_fileName))
			{
				var options = (T)_xmlSerializer.Deserialize(reader);

				foreach (var propertyInfo in typeof(T).GetProperties())
				{
					if (!propertyInfo.CanWrite)
					{
						continue;
					}

					var value = propertyInfo.GetValue(options, null);
					propertyInfo.SetValue(_options, value, null);
				}
			}
		}
	}
}