using System;
using System.Collections.Generic;
using System.IO;
using ColossalFramework.IO;
using OptionsFramework.Attributes;

namespace OptionsFramework
{
	sealed class XmlOptionsStoreProvider : IOptionsStoreProvider
	{
		public static XmlOptionsStoreProvider Instance => _instance;
		static readonly XmlOptionsStoreProvider _instance = new XmlOptionsStoreProvider();

		readonly Dictionary<string, object> _stores = new Dictionary<string, object>();

		XmlOptionsStoreProvider() { }

		public IOptionsStore<T> GetOrCreate<T>() where T : class, new()
		{
			var typeName = typeof(T).FullName;

			if (_stores.TryGetValue(typeName, out var storeObj))
			{
				return (IOptionsStore<T>)storeObj;
			}

			var optionsType = typeof(T);
			var attrs = optionsType.GetCustomAttributes(typeof(ModOptionsAttribute), false);
			if (attrs.Length != 1)
			{
				throw new FormatException($"The type {optionsType.Name} is missing the XmlOptionsAttribute");
			}

			var optionsAttr = (ModOptionsAttribute)attrs[0];

			var optionsFileName = $"{optionsAttr.ModName}.xml";
			var fullPath = Path.Combine(DataLocation.localApplicationData, optionsFileName);

			var store = new XmlOptionsStore<T>(fullPath);
			_stores.Add(typeName, store);

			return store;
		}
	}
}