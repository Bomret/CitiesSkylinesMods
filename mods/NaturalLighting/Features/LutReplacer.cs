using NaturalLighting.Settings;
using UnityEngine;

namespace NaturalLighting.Features
{
	sealed class LutReplacer : Feature<ModSettings>
	{
		readonly IModProvider _modProvider;
		readonly ILogger _logger;
		LutProvider _lutProvider;
		ColorCorrectionManager _colorCorrections;
		Texture3D _defaultLut;
		bool _currentUseOwnLut;

		public LutReplacer(IModProvider modProvider, ILogger logger)
		{
			_modProvider = modProvider;
			_logger = logger;
		}

		public override void OnLoaded(ModSettings initialSettings)
		{
			var mod = _modProvider.GetCurrentMod();
			_lutProvider = new LutProvider(mod);

			_colorCorrections = Object.FindObjectOfType<ColorCorrectionManager>();

			var renderProperties = Object.FindObjectOfType<RenderProperties>();

			_defaultLut = renderProperties.m_ColorCorrectionLUT;

			_currentUseOwnLut = initialSettings.UseOwnLut;
			if (!_currentUseOwnLut) return;

			ReplaceLut(true);
		}

		public override void OnSettingsChanged(ModSettings currentSettings)
		{
			if (_currentUseOwnLut == currentSettings.UseOwnLut) return;

			ReplaceLut(currentSettings.UseOwnLut);

			_currentUseOwnLut = currentSettings.UseOwnLut;
		}

		public override void OnUnloading() => ReplaceLut(false);
		protected override void OnDispose() => ReplaceLut(false);

		void ReplaceLut(bool useOwnLut)
		{
			_logger.LogFormat(LogType.Log, "[NaturalLighting] LutReplacer.ReplaceLut: {0}", useOwnLut ? "Natural" : "Default");

			if (!useOwnLut)
			{
				_colorCorrections.SetLUT(_defaultLut);
				return;
			}

			var replacementLut = _lutProvider.GetLut("Default");
			if (replacementLut is null)
			{
				replacementLut = _defaultLut;
			}

			_colorCorrections.SetLUT(replacementLut);

		}
	}
}