using NaturalLighting.Settings;
using UnityEngine;

namespace NaturalLighting.Features
{
	sealed class NaturalSunlight : Feature<ModSettings>
	{
		static readonly Gradient NaturalColor = new Gradient()
		{
			colorKeys = new GradientColorKey[8]
			{
				new GradientColorKey(new Color32( 55,  66,  77, byte.MaxValue), 0.23f),
				new GradientColorKey(new Color32( 245,  173,  84, byte.MaxValue), 0.26f),
				new GradientColorKey(new Color32( 252,  222,  186, byte.MaxValue), 0.29f),
				new GradientColorKey(new Color32( byte.MaxValue,  byte.MaxValue,  byte.MaxValue, byte.MaxValue), 0.33f),
				new GradientColorKey(new Color32( byte.MaxValue,  byte.MaxValue,  byte.MaxValue, byte.MaxValue), 0.63f),
				new GradientColorKey(new Color32( 252,  222,  186, byte.MaxValue), 0.72f),
				new GradientColorKey(new Color32( 245,  173,  84, byte.MaxValue), 0.75f),
				new GradientColorKey(new Color32( 55,  66,  77, byte.MaxValue), 0.78f)
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

		public NaturalSunlight(ILogger logger)
		{
			_logger = logger;
		}


		public override void OnLoaded(ModSettings settings)
		{
			_dayNightProperties = Object.FindObjectOfType<DayNightProperties>();
			_defaultColor = _dayNightProperties.m_LightColor;

			_currentUseNaturalSunlight = settings.UseNaturalSunlight;
			if (_currentUseNaturalSunlight)
			{
				ReplaceSunlight(true);
			}
		}

		public override void OnSettingsChanged(ModSettings settings)
		{
			if (_currentUseNaturalSunlight == settings.UseNaturalSunlight) return;

			ReplaceSunlight(settings.UseNaturalSunlight);

			_currentUseNaturalSunlight = settings.UseNaturalSunlight;
		}

		public override void OnUnloading() => ReplaceSunlight(false);

		protected override void OnDispose() => ReplaceSunlight(false);

		void ReplaceSunlight(bool useNatural)
		{
			_logger.LogFormat(LogType.Log, "[NaturalLighting] NaturalSunlight.ReplaceSunlight: " + (useNatural ? "Natural" : "Default"));

			_dayNightProperties.m_LightColor = useNatural ? NaturalColor : _defaultColor;
		}
	}
}