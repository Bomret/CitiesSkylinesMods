using NaturalLighting.Settings;
using UnityEngine;

namespace NaturalLighting.Replacer
{
	sealed class SunlightReplacer : Replacer<NaturalLightingSettings>
	{
		static readonly Gradient NaturalColor = new Gradient()
		{
			colorKeys = new GradientColorKey[8]
			{
				new GradientColorKey(new Color32( 55,  66,  77, byte.MaxValue), 0.23f),
				new GradientColorKey(new Color32( 245,  173,  84, byte.MaxValue), 0.26f),
				new GradientColorKey(new Color32( 252,  222,  186, byte.MaxValue), 0.29f),
				new GradientColorKey(new Color32( byte.MaxValue,  byte.MaxValue,  byte.MaxValue, byte.MaxValue), 0.35f),
				new GradientColorKey(new Color32( byte.MaxValue,  byte.MaxValue,  byte.MaxValue, byte.MaxValue), 0.65f),
				new GradientColorKey(new Color32( 252,  222,  186, byte.MaxValue), 0.71f),
				new GradientColorKey(new Color32( 245,  173,  84, byte.MaxValue), 0.74f),
				new GradientColorKey(new Color32( 55,  66,  77, byte.MaxValue), 0.77f)
			},
			alphaKeys = new GradientAlphaKey[2]
			{
				new GradientAlphaKey(1f, 0f),
				new GradientAlphaKey(1f, 1f)
			}
		};

		readonly ILogger _logger;

		DayNightProperties _dayNightProperties;
		Gradient _defaultColor;
		bool _currentUseNaturalSunlight;

		public SunlightReplacer(ILogger logger)
		{
			_logger = logger;
		}

		public override void OnLoaded(NaturalLightingSettings settings)
		{
			_dayNightProperties = Object.FindObjectOfType<DayNightProperties>();
			_defaultColor = _dayNightProperties.m_LightColor;

			_currentUseNaturalSunlight = settings.UseNaturalSunlight;
			if (_currentUseNaturalSunlight)
			{
				ReplaceSunlight(true);
			}
		}

		public override void OnSettingsChanged(NaturalLightingSettings settings)
		{
			if (_currentUseNaturalSunlight == settings.UseNaturalSunlight) return;

			ReplaceSunlight(settings.UseNaturalSunlight);

			_currentUseNaturalSunlight = settings.UseNaturalSunlight;
		}

		public override void OnUnloading() => ReplaceSunlight(false);

		protected override void OnDispose() => ReplaceSunlight(false);

		void ReplaceSunlight(bool useNatural)
		{
			var sunlightColor = useNatural ? NaturalColor : _defaultColor;
			_dayNightProperties.m_LightColor = sunlightColor;

			_logger.LogFormat(LogType.Log, "[NaturalLighting] SunlightReplacer.ReplaceSunlight: " + (useNatural ? "Natural" : "Default"));
		}
	}
}