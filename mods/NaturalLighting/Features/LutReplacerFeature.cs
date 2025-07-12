using NaturalLighting.Settings;
using UnityEngine;

namespace NaturalLighting.Features
{
	/// <summary>
	/// Manages replacement of Unity's default color correction LUT (Look-Up Table) with custom LUTs
	/// to provide natural lighting color grading for Cities: Skylines.
	/// 
	/// This feature dynamically swaps between the game's default color correction LUT and a custom
	/// natural lighting LUT based on user settings, allowing for enhanced visual quality while
	/// maintaining the ability to revert to original lighting.
	/// </summary>
	sealed class LutReplacerFeature : Feature<ModSettings>
	{
		LutProvider _lutProvider;
		ColorCorrectionManager _colorCorrections;
		Texture3D _defaultLut;
		bool _currentUseOwnLut;

		/// <summary>
		/// Initializes a new instance of the LutReplacerFeature class.
		/// </summary>
		/// <param name="modProvider">Provider for accessing mod resources and metadata.</param>
		/// <param name="logger">Logger for diagnostic output and error reporting.</param>
		public LutReplacerFeature(IModProvider modProvider, ILogger logger)
			: base(modProvider, logger) { }

		/// <summary>
		/// Called when the mod is loaded. Initializes the LUT provider and color correction manager,
		/// captures the default LUT for restoration purposes, and applies custom LUT if enabled.
		/// </summary>
		/// <param name="initialSettings">Current mod settings containing LUT replacement preferences.</param>
		public override void OnLoaded(ModSettings initialSettings)
		{
			var mod = ModProvider.GetCurrentMod();
			_lutProvider = new LutProvider(mod);

			_colorCorrections = Object.FindObjectOfType<ColorCorrectionManager>();

			var renderProperties = Object.FindObjectOfType<RenderProperties>();

			_defaultLut = renderProperties.m_ColorCorrectionLUT;

			_currentUseOwnLut = initialSettings.UseOwnLut;
			if (!_currentUseOwnLut) return;

			ReplaceLut(true);
		}

		/// <summary>
		/// Called when mod settings are changed during runtime. Switches between default and custom LUTs
		/// based on the updated UseOwnLut setting, but only if the setting has actually changed.
		/// </summary>
		/// <param name="currentSettings">Updated mod settings.</param>
		public override void OnSettingsChanged(ModSettings currentSettings)
		{
			if (_currentUseOwnLut == currentSettings.UseOwnLut) return;

			ReplaceLut(currentSettings.UseOwnLut);

			_currentUseOwnLut = currentSettings.UseOwnLut;
		}

		/// <summary>
		/// Called when the mod is being unloaded. Restores the default LUT to ensure
		/// the game returns to its original color correction state.
		/// </summary>
		public override void OnUnloading() => ReplaceLut(false);

		/// <summary>
		/// Called when the feature is being disposed. Restores the default LUT to clean up
		/// any modifications made to the game's color correction system.
		/// </summary>
		protected override void OnDispose() => ReplaceLut(false);

		/// <summary>
		/// Replaces the active color correction LUT with either the custom natural lighting LUT
		/// or restores the original default LUT based on the useOwnLut parameter.
		/// 
		/// When enabling custom LUT, attempts to load the "Default" LUT from the mod's assets.
		/// If the custom LUT cannot be loaded, falls back to the original default LUT to prevent
		/// visual corruption.
		/// </summary>
		/// <param name="useOwnLut">True to use the custom natural lighting LUT, false to restore the original default LUT.</param>
		void ReplaceLut(bool useOwnLut)
		{
			Logger.LogFormat(LogType.Log, "[NaturalLighting] LutReplacer.ReplaceLut: {0}", useOwnLut ? "Natural" : "Default");

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