using System;
using System.IO;

namespace NaturalLighting
{
	sealed class TranslatorProvider : IDisposable
	{
		Translator _translator;
		bool _isDisposed;

		public ITranslator GetOrCreate()
		{
			if (_translator != null) return _translator;

			var pluginInfo = PluginInfoProvider.GetOrResolvePluginInfo();
			var languageFilesDirPath = Path.Combine(pluginInfo.modPath, "Locales");
			var languageFilesDir = new DirectoryInfo(languageFilesDirPath);

			_translator = new Translator(languageFilesDir);

			return _translator;
		}

		void Dispose(bool disposing)
		{
			if (_isDisposed)
			{
				return;
			}

			if (disposing)
			{
				_translator?.Dispose();
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
