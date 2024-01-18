using UnityEngine;

namespace NaturalLighting.Replacer
{
	sealed class SunlightReplacer : Replacer
	{
		static readonly Gradient NaturalColor = new Gradient()
		{
			colorKeys = new GradientColorKey[8]
			{
				new GradientColorKey((Color) new Color32( 55,  66,  77, byte.MaxValue), 0.23f),
				new GradientColorKey((Color) new Color32( 245,  173,  84, byte.MaxValue), 0.26f),
				new GradientColorKey((Color) new Color32( 252,  222,  186, byte.MaxValue), 0.29f),
				new GradientColorKey((Color) new Color32( byte.MaxValue,  byte.MaxValue,  byte.MaxValue, byte.MaxValue), 0.35f),
				new GradientColorKey((Color) new Color32( byte.MaxValue,  byte.MaxValue,  byte.MaxValue, byte.MaxValue), 0.65f),
				new GradientColorKey((Color) new Color32( 252,  222,  186, byte.MaxValue), 0.71f),
				new GradientColorKey((Color) new Color32( 245,  173,  84, byte.MaxValue), 0.74f),
				new GradientColorKey((Color) new Color32( 55,  66,  77, byte.MaxValue), 0.77f)
			},
			alphaKeys = new GradientAlphaKey[]
			{
				new GradientAlphaKey(1f, 0f),
				new GradientAlphaKey(1f, 1f)
			}
		};

		DayNightProperties _dayNightProperties;
		Gradient _defaultColor;
		bool _currentUseNaturalSunlight;

		public void Awake()
		{
			_dayNightProperties = FindObjectOfType<DayNightProperties>();
			_defaultColor = _dayNightProperties.m_LightColor;

			var settings = ModSettingsStore.GetOrLoadSettings();

			_currentUseNaturalSunlight = settings.UseNaturalSunlight;
		}

		public void Start()
		{
			if (_currentUseNaturalSunlight)
			{
				ReplaceSunlight(true);
			}
		}

		public void Update()
		{
			var settings = ModSettingsStore.GetOrLoadSettings();

			if (_currentUseNaturalSunlight == settings.UseNaturalSunlight) return;

			ReplaceSunlight(settings.UseNaturalSunlight);

			_currentUseNaturalSunlight = settings.UseNaturalSunlight;
		}

		public void OnDestroy() => ReplaceSunlight(false);

		void ReplaceSunlight(bool useNatural)
		{
			var sunlightColor = useNatural ? NaturalColor : _defaultColor;
			_dayNightProperties.m_LightColor = sunlightColor;

			Debug.Log("[NaturalLighting] SunlightReplacer.ReplaceSunlight: " + (useNatural ? "Natural" : "Default"));
		}
	}
}