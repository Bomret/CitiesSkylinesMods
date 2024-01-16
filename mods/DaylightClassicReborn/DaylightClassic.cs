using ColossalFramework;
using OptionsFramework;
using UnityEngine;

namespace DaylightClassicReborn
{
	static class DaylightClassic
	{
		const string Europe = "LUTeurope";
		const string Sunny = "LUTSunny";
		const string North = "LUTNorth";
		const string Tropical = "LUTTropical";
		const string Winter = "LUTWinter";

		static Texture3DWrapper _europeanClassic;
		static Texture3DWrapper _sunnyClassic;
		static Texture3DWrapper _northClassic;
		static Texture3DWrapper _tropicalClassic;
		static Texture3DWrapper _winterClassic;

		static Texture3DWrapper _europeanAfterDark;
		static Texture3DWrapper _sunnyAfterDark;
		static Texture3DWrapper _northAfterDark;
		static Texture3DWrapper _tropicalAfterDark;
		static Texture3DWrapper _winterAfterDark;

		const float IntensityClassic = 3.318695f;
		const float ExposureClassic = 1.0f;

		static float _intensityAfterDark = -1.0f;
		static float _exposureAfterDark = -1.0f;
		static float _lonAfterDark = -1.0f;
		static float _latAfterDark = -1.0f;

		static readonly Gradient ColorClassic = new Gradient()
		{
			colorKeys = new GradientColorKey[8]
			{
				new GradientColorKey((Color) new Color32( 55,  66,  77, byte.MaxValue), 0.23f),
				new GradientColorKey((Color) new Color32( 245,  173,  84, byte.MaxValue), 0.26f),
				new GradientColorKey((Color) new Color32( 252,  222,  186, byte.MaxValue), 0.29f),
				new GradientColorKey((Color) new Color32( 255,  255,  255, byte.MaxValue), 0.35f),
				new GradientColorKey((Color) new Color32( 255,  255,  255, byte.MaxValue), 0.65f),
				new GradientColorKey((Color) new Color32( 252,  222,  186, byte.MaxValue), 0.71f),
				new GradientColorKey((Color) new Color32( 245,  173,  84, byte.MaxValue), 0.74f),
				new GradientColorKey((Color) new Color32( 55,  66,  77, byte.MaxValue), 0.77f)
			},
			alphaKeys = new GradientAlphaKey[2]
			{
				new GradientAlphaKey(1f, 0.0f),
				new GradientAlphaKey(1f, 1f)
			}
		};

		static Gradient _colorAfterDark;
		static DayNightProperties _dayNightProperties;
		static GameObject _gameObject;

		public static void SetUp()
		{
			Debug.Log("[DaylighClassicRevived] Setting up DaylightClassic");

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
			ReplaceShadows();
		}

		static void ReplaceShadows()
		{
			if (!InGame)
			{
				return;
			}

			if (_gameObject.GetComponent<ShadowReplacer>() == null)
			{
				_gameObject.AddComponent<ShadowReplacer>();
			}
		}

		public static void CleanUp()
		{
			Debug.Log("[DaylighClassicRevived] Cleaning up DaylightClassic");

			ReplaceFogEffect(false);
			ReplaceSunlightColor(false);
			ReplaceSunlightIntensity(false);
			ReplaceLuts(false);
			ReplaceLatLong(false);

			if (_gameObject.GetComponent<ShadowReplacer>() != null)
			{
				Object.Destroy(_gameObject.GetComponent<ShadowReplacer>());
			}

			if (_gameObject != null)
			{
				Object.Destroy(_gameObject);
				_gameObject = null;
			}

			Reset();

			_dayNightProperties = null;
		}

		static void Reset()
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

			_europeanAfterDark = null;
			_tropicalAfterDark = null;
			_northAfterDark = null;
			_sunnyAfterDark = null;
			_winterAfterDark = null;
			_intensityAfterDark = -1.0f;
			_lonAfterDark = -1.0f;
			_latAfterDark = -1.0f;
		}

		public static void ReplaceLuts(bool toClassic)
		{
			if (!InGame)
			{
				return;
			}

			for (var i = 0; i < ColorCorrectionManager.instance.m_BuiltinLUTs.Length; i++)
			{
				var existingLut = ColorCorrectionManager.instance.m_BuiltinLUTs[i];
				var replacement1 = GetReplacementLut(toClassic, existingLut);

				if (replacement1 == null)
				{
					continue;
				}

				ColorCorrectionManager.instance.m_BuiltinLUTs[i] = replacement1;
			}

			var renderProperties = Object.FindObjectOfType<RenderProperties>();
			var replacement2 = GetReplacementLut(toClassic, renderProperties.m_ColorCorrectionLUT);

			if (replacement2 != null)
			{
				renderProperties.m_ColorCorrectionLUT = replacement2;
			}

			var size = ColorCorrectionManager.instance.items.Length;
			var lastSelection = ColorCorrectionManager.instance.lastSelection;

			ColorCorrectionManager.instance.currentSelection = (lastSelection + 1) % size;
			ColorCorrectionManager.instance.currentSelection = lastSelection;
		}

		static Texture3DWrapper GetReplacementLut(bool toClassic, Texture3DWrapper existingLut)
		{
			switch (existingLut.name)
			{
				case Europe:
					if (_europeanAfterDark == null)
					{
						_europeanAfterDark = existingLut;
					}

					if (_europeanClassic == null)
					{
						_europeanClassic = TextureLoader.LoadTextureFromEmbeddedResource("DaylightClassicReborn.Assets.Luts.EuropeanClassic.png", Europe);
					}

					return toClassic ? _europeanClassic : _europeanAfterDark;

				case Tropical:
					if (_tropicalAfterDark == null)
					{
						_tropicalAfterDark = existingLut;
					}

					if (_tropicalClassic == null)
					{
						_tropicalClassic = TextureLoader.LoadTextureFromEmbeddedResource("DaylightClassicReborn.Assets.Luts.TropicalClassic.png", Tropical);
					}

					return toClassic ? _tropicalClassic : _tropicalAfterDark;

				case North:
					if (_northAfterDark == null)
					{
						_northAfterDark = existingLut;
					}

					if (_northClassic == null)
					{
						_northClassic = TextureLoader.LoadTextureFromEmbeddedResource("DaylightClassicReborn.Assets.Luts.BorealClassic.png", North);
					}

					return toClassic ? _northClassic : _northAfterDark;

				case Sunny:
					if (_sunnyAfterDark == null)
					{
						_sunnyAfterDark = existingLut;
					}

					if (_sunnyClassic == null)
					{
						_sunnyClassic = TextureLoader.LoadTextureFromEmbeddedResource("DaylightClassicReborn.Assets.Luts.TemperateClassic.png", Sunny);
					}

					return toClassic ? _sunnyClassic : _sunnyAfterDark;

				case Winter:
					if (_winterAfterDark == null)
					{
						_winterAfterDark = existingLut;
					}

					if (_winterClassic == null)
					{
						_winterClassic = TextureLoader.LoadTextureFromEmbeddedResource("DaylightClassicReborn.Assets.Luts.WinterClassic.png", Winter);
					}

					return toClassic ? _winterClassic : _winterAfterDark;

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

			if (_intensityAfterDark < 0)
			{
				_intensityAfterDark = _dayNightProperties.m_SunIntensity;
			}

			if (_exposureAfterDark < 0)
			{
				_exposureAfterDark = _dayNightProperties.m_Exposure;
			}

			_dayNightProperties.m_SunIntensity = toClassic ? IntensityClassic : _intensityAfterDark;
			_dayNightProperties.m_Exposure = toClassic ? ExposureClassic : _exposureAfterDark;
		}

		public static void ReplaceSunlightColor(bool toClassic)
		{
			if (!InGame)
			{
				return;
			}

			if (_colorAfterDark == null)
			{
				_colorAfterDark = _dayNightProperties.m_LightColor;
			}

			_dayNightProperties.m_LightColor = toClassic ? ColorClassic : _colorAfterDark;
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

			if (_lonAfterDark < 0.0f)
			{
				_lonAfterDark = _dayNightProperties.m_Longitude;
			}

			if (_latAfterDark < 0.0f)
			{
				_latAfterDark = _dayNightProperties.m_Latitude;
			}

			var env = GameEnvironmentProvider.GetInGameEnvironment();
			if (toClassic)
			{
				if (env == GameEnvironment.Europe) //London
				{
					_dayNightProperties.m_Latitude = 51.5072f;
					_dayNightProperties.m_Longitude = -0.1275f;
				}
				else if (env == GameEnvironment.North) //Stockholm
				{
					_dayNightProperties.m_Latitude = 59.3293f;
					_dayNightProperties.m_Longitude = 18.0686f;
				}
				else if (env == GameEnvironment.Sunny) //Malta
				{
					_dayNightProperties.m_Latitude = 35.8833f;
					_dayNightProperties.m_Longitude = 14.5000f;
				}
				else if (env == GameEnvironment.Tropical) //Mecca
				{
					_dayNightProperties.m_Latitude = 21.4167f;
					_dayNightProperties.m_Longitude = 39.8167f;
				}
			}
			else
			{
				_dayNightProperties.m_Latitude = _latAfterDark;
				_dayNightProperties.m_Longitude = _lonAfterDark;
			}
		}

		static bool InGame => _gameObject != null;
	}
}