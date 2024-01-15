using ColossalFramework;
using OptionsFramework;
using UnityEngine;

namespace DaylightClassic
{
	public static class DaylightClassic
	{
		private const string Europe = "LUTeurope";
		private const string Sunny = "LUTSunny";
		private const string North = "LUTNorth";
		private const string Tropical = "LUTTropical";
		private const string Winter = "LUTWinter";

		private static Texture3DWrapper _europeanClassic;
		private static Texture3DWrapper _sunnyClassic;
		private static Texture3DWrapper _northClassic;
		private static Texture3DWrapper _tropicalClassic;
		private static Texture3DWrapper _winterClassic;

		private static Texture3DWrapper _europeanAd;
		private static Texture3DWrapper _sunnyAd;
		private static Texture3DWrapper _northAd;
		private static Texture3DWrapper _tropicalAd;
		private static Texture3DWrapper _winterAd;

		private const float IntensityClassic = 3.318695f;
		private const float ExposureClassic = 1.0f;
		private static float _intensityAd = -1.0f;
		private static float _exposureAd = -1.0f;

		private static float _lonAd = -1.0f;
		private static float _latAd = -1.0f;

		private static readonly Gradient ColorClassic = new Gradient()
		{
			colorKeys = new GradientColorKey[8]
			{
				new GradientColorKey((Color) new Color32((byte) 55, (byte) 66, (byte) 77, byte.MaxValue), 0.23f),
				new GradientColorKey((Color) new Color32((byte) 245, (byte) 173, (byte) 84, byte.MaxValue), 0.26f),
				new GradientColorKey((Color) new Color32((byte) 252, (byte) 222, (byte) 186, byte.MaxValue), 0.29f),
				new GradientColorKey((Color) new Color32((byte) 255, (byte) 255, (byte) 255, byte.MaxValue), 0.35f),
				new GradientColorKey((Color) new Color32((byte) 255, (byte) 255, (byte) 255, byte.MaxValue), 0.65f),
				new GradientColorKey((Color) new Color32((byte) 252, (byte) 222, (byte) 186, byte.MaxValue), 0.71f),
				new GradientColorKey((Color) new Color32((byte) 245, (byte) 173, (byte) 84, byte.MaxValue), 0.74f),
				new GradientColorKey((Color) new Color32((byte) 55, (byte) 66, (byte) 77, byte.MaxValue), 0.77f)
			},
			alphaKeys = new GradientAlphaKey[2]
			{
				new GradientAlphaKey(1f, 0.0f),
				new GradientAlphaKey(1f, 1f)
			}
		};

		private static Gradient _colorAd;
		private static DayNightProperties _dayNightProperties;
		private static GameObject _gameObject;


		public static void SetUp()
		{
			_dayNightProperties = Object.FindObjectOfType<DayNightProperties>();
			Object.FindObjectOfType<RenderProperties>().m_sun =
				_dayNightProperties.sunLightSource.transform; //to fix sun position in some envs

			Reset();

			_gameObject = new GameObject("DaylightClassic");
			_gameObject.AddComponent<FogColorReplacer>();

			ReplaceFogEffect(XmlOptionsWrapper<Options>.Options.FogEffect);
			ReplaceSunlightColor(XmlOptionsWrapper<Options>.Options.SunlightColor);
			ReplaceSunlightIntensity(XmlOptionsWrapper<Options>.Options.SunlightIntensity);
			ReplaceLuts(XmlOptionsWrapper<Options>.Options.StockLuts);
			ReplaceLatLong(XmlOptionsWrapper<Options>.Options.SunPosition);
		}

		public static void CleanUp()
		{
			ReplaceFogEffect(false);
			ReplaceSunlightColor(false);
			ReplaceSunlightIntensity(false);
			ReplaceLuts(false);
			ReplaceLatLong(false);

			if (_gameObject != null)
			{
				Object.Destroy(_gameObject);
				_gameObject = null;
			}

			Reset();

			_dayNightProperties = null;
		}

		private static void Reset()
		{
			if (_europeanClassic != null)
			{
				Object.Destroy(_europeanClassic);
				_europeanClassic = null;
			}

			if (_tropicalClassic != null)
			{
				Object.Destroy(_tropicalClassic);
				_tropicalClassic = null;
			}

			if (_northClassic != null)
			{
				Object.Destroy(_northClassic);
				_northClassic = null;
			}

			if (_sunnyClassic != null)
			{
				Object.Destroy(_sunnyClassic);
				_sunnyClassic = null;
			}

			if (_winterClassic != null)
			{
				Object.Destroy(_winterClassic);
				_winterClassic = null;
			}

			_europeanAd = null;
			_tropicalAd = null;
			_northAd = null;
			_sunnyAd = null;
			_winterAd = null;
			_intensityAd = -1.0f;
			_lonAd = -1.0f;
			_latAd = -1.0f;
		}

		public static void ReplaceLuts(bool toClassic)
		{
			if (!InGame)
			{
				return;
			}

			for (var i = 0; i < ColorCorrectionManager.instance.m_BuiltinLUTs.Length; i++)
			{
				var replacement1 = GetReplacementLut(toClassic,
					ColorCorrectionManager.instance.m_BuiltinLUTs[i].name,
					ColorCorrectionManager.instance.m_BuiltinLUTs[i]);
				if (replacement1 == null)
				{
					continue;
				}

				ColorCorrectionManager.instance.m_BuiltinLUTs[i] = replacement1;
			}

			var renderProperties = Object.FindObjectOfType<RenderProperties>();
			var replacement2 = GetReplacementLut(toClassic,
				renderProperties.m_ColorCorrectionLUT.name,
				renderProperties.m_ColorCorrectionLUT);
			if (replacement2 != null)
			{
				renderProperties.m_ColorCorrectionLUT = replacement2;
			}

			var size = ColorCorrectionManager.instance.items.Length;
			var lastSelection = ColorCorrectionManager.instance.lastSelection;
			ColorCorrectionManager.instance.currentSelection = (lastSelection + 1) % size;
			ColorCorrectionManager.instance.currentSelection = lastSelection;
		}

		private static Texture3DWrapper GetReplacementLut(bool toClassic, string builtinLutName,
			Texture3DWrapper builtinLut)
		{
			switch (builtinLutName)
			{
				case Europe:
					if (_europeanAd == null)
					{
						_europeanAd = builtinLut;
					}

					if (_europeanClassic == null)
					{
						_europeanClassic = Util.LoadTexture("DaylightClassic.lut.EuropeanClassic.png", Europe);
					}

					return toClassic ? _europeanClassic : _europeanAd;
				case Tropical:
					if (_tropicalAd == null)
					{
						_tropicalAd = builtinLut;
					}

					if (_tropicalClassic == null)
					{
						_tropicalClassic = Util.LoadTexture("DaylightClassic.lut.TropicalClassic.png", Tropical);
					}

					return toClassic ? _tropicalClassic : _tropicalAd;
				case North:
					if (_northAd == null)
					{
						_northAd = builtinLut;
					}

					if (_northClassic == null)
					{
						_northClassic = Util.LoadTexture("DaylightClassic.lut.BorealClassic.png", North);
					}

					return toClassic ? _northClassic : _northAd;
				case Sunny:
					if (_sunnyAd == null)
					{
						_sunnyAd = builtinLut;
					}

					if (_sunnyClassic == null)
					{
						_sunnyClassic = Util.LoadTexture("DaylightClassic.lut.TemperateClassic.png", Sunny);
					}

					return toClassic ? _sunnyClassic : _sunnyAd;
				case Winter:
					if (_winterAd == null)
					{
						_winterAd = builtinLut;
					}

					if (_winterClassic == null)
					{
						_winterClassic = Util.LoadTexture("DaylightClassic.lut.WinterClassic.png", Winter);
					}

					return toClassic ? _winterClassic : _winterAd;
				default:
					return null;
			}
		}

		public static void ReplaceSunlightIntensity(bool toClassic)
		{
			if (!InGame)
			{
				return;
			}

			if (_intensityAd < 0)
			{
				_intensityAd = _dayNightProperties.m_SunIntensity;
			}

			if (_exposureAd < 0)
			{
				_exposureAd = _dayNightProperties.m_Exposure;
			}

			_dayNightProperties.m_SunIntensity = toClassic ? IntensityClassic : _intensityAd;
			_dayNightProperties.m_Exposure = toClassic ? ExposureClassic : _exposureAd;
		}

		public static void ReplaceSunlightColor(bool toClassic)
		{
			if (!InGame)
			{
				return;
			}

			if (_colorAd == null)
			{
				_colorAd = _dayNightProperties.m_LightColor;
			}

			_dayNightProperties.m_LightColor = toClassic ? ColorClassic : _colorAd;
		}

		public static void ReplaceFogEffect(bool toClassic)
		{
			if (!InGame)
			{
				return;
			}

			if (toClassic)
			{
				if (_gameObject.GetComponent<FogEffectReplacer>() == null)
				{
					_gameObject.AddComponent<FogEffectReplacer>();
				}
			}
			else
			{
				if (_gameObject.GetComponent<FogEffectReplacer>() != null)
				{
					Object.Destroy(_gameObject.GetComponent<FogEffectReplacer>());
				}
			}
		}

		public static void ReplaceLatLong(bool toClassic)
		{
			if (!InGame)
			{
				return;
			}

			if (_lonAd < 0.0f)
			{
				_lonAd = _dayNightProperties.m_Longitude;
			}

			if (_latAd < 0.0f)
			{
				_latAd = _dayNightProperties.m_Latitude;
			}

			var env = Util.Env;
			if (toClassic)
			{
				if (env == "Europe") //London
				{
					_dayNightProperties.m_Latitude = 51.5072f;
					_dayNightProperties.m_Longitude = -0.1275f;
				}
				else if (env == "North") //Stockholm
				{
					_dayNightProperties.m_Latitude = 59.3293f;
					_dayNightProperties.m_Longitude = 18.0686f;
				}
				else if (env == "Sunny") //Malta
				{
					_dayNightProperties.m_Latitude = 35.8833f;
					_dayNightProperties.m_Longitude = 14.5000f;
				}
				else if (env == "Tropical") //Mecca
				{
					_dayNightProperties.m_Latitude = 21.4167f;
					_dayNightProperties.m_Longitude = 39.8167f;
				}
			}
			else
			{
				_dayNightProperties.m_Latitude = _latAd;
				_dayNightProperties.m_Longitude = _lonAd;
			}
		}

		private static bool InGame => _gameObject != null;
	}
}