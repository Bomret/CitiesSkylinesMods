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
			Debug.Log("[NaturalLighting] SunlightReplacer.Awake");

			_dayNightProperties = FindObjectOfType<DayNightProperties>();
			_defaultColor = _dayNightProperties.m_LightColor;

			var options = OptionsStore.GetOrLoadOptions();
			_currentUseNaturalSunlight = options.UseNaturalSunlight;
		}

		public void Start()
		{
			Debug.Log("[NaturalLighting] SunlightReplacer.Start");

			if (_currentUseNaturalSunlight)
			{
				ReplaceSunlight(true);
			}
		}

		public void Update()
		{
			var options = OptionsStore.GetOrLoadOptions();

			if (_currentUseNaturalSunlight != options.UseNaturalSunlight)
			{
				ReplaceSunlight(options.UseNaturalSunlight);

				_currentUseNaturalSunlight = options.UseNaturalSunlight;
			}
		}

		void ReplaceSunlight(bool replace)
		{
			var sunlightColor = replace ? NaturalColor : _defaultColor;
			_dayNightProperties.m_LightColor = sunlightColor;

			Debug.Log("[NaturalLighting] SunlightReplacer.ReplaceSunlight: " + (replace ? "Natural" : "Default"));
		}

		public void OnDestroy()
		{
			Debug.Log("[NaturalLighting] SunlightReplacer.OnDestroy");

			ReplaceSunlight(false);
		}
	}
}