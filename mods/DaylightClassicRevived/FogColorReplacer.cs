using OptionsFramework;
using UnityEngine;

namespace DaylightClassicRevived
{
	sealed class FogColorReplacer : MonoBehaviour
	{
		static readonly Color SkyTintClassic = new Color(0.4078431372549019607843137254902f,
			0.65098039215686274509803921568627f, 0.82745098039215686274509803921569f,
			1.0f); //(104f, 166f, 211f, 255f);;

		static readonly Vector3 WaveLengthsClassic = new Vector3(680f, 680f, 680f);

		DayNightFogEffect _newEffect;
		bool _cachedReplaceFogColor;
		bool _cachedEffectEnabled;
		Color _skyTintAd = Color.clear;
		Vector3 _waveLengthsAd = Vector3.zero;
		DayNightProperties _dayNightProperties;
		Color _cachedSkyTint;
		Vector3 _cachedWaveLengths;
		float _cachedTimeOfDay;

		public void Awake()
		{
			_newEffect = FindObjectOfType<DayNightFogEffect>();
			_dayNightProperties = FindObjectOfType<DayNightProperties>();
			_waveLengthsAd = _dayNightProperties.m_WaveLengths;
			_skyTintAd = _dayNightProperties.m_SkyTint;
		}

		public void Update()
		{
			ReplaceFogColorIfNeeded();
		}

		public void OnDestroy()
		{
			ReplaceFogColorImpl(false);
		}

		void ReplaceFogColorIfNeeded()
		{
			if (_newEffect == null ||
				_cachedReplaceFogColor == XmlOptionsWrapper<Options>.Options.FogColor &&
				_cachedEffectEnabled == _newEffect.enabled &&
				_dayNightProperties.m_WaveLengths.Equals(_cachedWaveLengths) &&
				_dayNightProperties.m_SkyTint.Equals(_cachedSkyTint) &&
				_dayNightProperties.m_TimeOfDay.Equals(_cachedTimeOfDay))
			{
				return;
			}

			ReplaceFogColorImpl(_newEffect.enabled && XmlOptionsWrapper<Options>.Options.FogColor);
			_cachedReplaceFogColor = XmlOptionsWrapper<Options>.Options.FogColor;
			_cachedEffectEnabled = _newEffect.enabled;
			_cachedWaveLengths = _dayNightProperties.m_WaveLengths;
			_cachedSkyTint = _dayNightProperties.m_SkyTint;
			_cachedTimeOfDay = _dayNightProperties.m_TimeOfDay;
		}

		void ReplaceFogColorImpl(bool toClassic)
		{
			_dayNightProperties.m_SkyTint = toClassic
				? SkyTintClassicGradient.Evaluate(_dayNightProperties.normalizedTimeOfDay)
				: _skyTintAd;
			_dayNightProperties.m_WaveLengths = toClassic
				? FromColor(WaveLengthsClassicGradient.Evaluate(_dayNightProperties.normalizedTimeOfDay))
				: _waveLengthsAd;
		}

		Gradient SkyTintClassicGradient => new Gradient()
		{
			colorKeys = new GradientColorKey[6]
			{
				new GradientColorKey(_skyTintAd, 0f),
				new GradientColorKey(_skyTintAd, 0.29f),
				new GradientColorKey(SkyTintClassic, 0.35f),
				new GradientColorKey(SkyTintClassic, 0.65f),
				new GradientColorKey(_skyTintAd, 0.71f),
				new GradientColorKey(_skyTintAd, 1f)
			},
			alphaKeys = new GradientAlphaKey[2]
			{
				new GradientAlphaKey(1f, 0.0f),
				new GradientAlphaKey(1f, 1f)
			}
		};

		Gradient WaveLengthsClassicGradient => new Gradient()
		{
			colorKeys = new GradientColorKey[6]
			{
				new GradientColorKey(FromVector3(_waveLengthsAd), 0f),
				new GradientColorKey(FromVector3(_waveLengthsAd), 0.29f),
				new GradientColorKey(FromVector3(WaveLengthsClassic), 0.35f),
				new GradientColorKey(FromVector3(WaveLengthsClassic), 0.65f),
				new GradientColorKey(FromVector3(_waveLengthsAd), 0.71f),
				new GradientColorKey(FromVector3(_waveLengthsAd), 1f)
			},
			alphaKeys = new GradientAlphaKey[2]
			{
				new GradientAlphaKey(1f, 0.0f),
				new GradientAlphaKey(1f, 1f)
			}
		};

		static Color FromVector3(Vector3 vector3)
		{
			return new Color(vector3.x / 1000f, vector3.y / 1000f, vector3.z / 1000f);
		}

		static Vector3 FromColor(Color color)
		{
			return new Vector3(color.r * 1000f, color.g * 1000f, color.b * 1000f);
		}
	}
}