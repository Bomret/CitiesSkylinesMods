using OptionsFramework;
using UnityEngine;

namespace DaylightClassicReborn
{
	sealed class FogColorReplacer : MonoBehaviour
	{
		struct State
		{
			public bool UseFogColorReplacer { get; set; }
			public bool IsFogEffectEnabled { get; set; }
			public Color CurrentSkyTint { get; set; }
			public Vector3 CurrentWaveLengths { get; set; }
			public float CurrentTimeOfDay { get; set; }
		}

		static readonly Color DefaultSkyTint = new Color(
			r: 0.5f,
			g: 0.5f,
			b: 0.5f,
			a: 1f);
		static readonly Vector3 DefaultWaveLengths = new Vector3(x: 680f, y: 550f, z: 440f);

		static readonly Color ClassicSkyTint = new Color(
			r: 0.4078431372549019607843137254902f,
			g: 0.65098039215686274509803921568627f,
			b: 0.82745098039215686274509803921569f,
			a: 1.0f);
		static readonly Gradient ClassicSkyTintGradient = new Gradient()
		{
			colorKeys = new GradientColorKey[6]
					{
				new GradientColorKey(DefaultSkyTint, 0f),
				new GradientColorKey(DefaultSkyTint, 0.29f),
				new GradientColorKey(ClassicSkyTint, 0.35f),
				new GradientColorKey(ClassicSkyTint, 0.65f),
				new GradientColorKey(DefaultSkyTint, 0.71f),
				new GradientColorKey(DefaultSkyTint, 1f)
					},
			alphaKeys = new GradientAlphaKey[2]
					{
				new GradientAlphaKey(1f, 0.0f),
				new GradientAlphaKey(1f, 1f)
					}
		};

		static readonly Vector3 ClassicWaveLengths = new Vector3(x: 680f, y: 680f, z: 680f);
		static readonly Gradient ClassicWaveLengthsGradient = new Gradient()
		{
			colorKeys = new GradientColorKey[6]
			{
				new GradientColorKey(ColorToVector3(DefaultWaveLengths), 0f),
				new GradientColorKey(ColorToVector3(DefaultWaveLengths), 0.29f),
				new GradientColorKey(ColorToVector3(ClassicWaveLengths), 0.35f),
				new GradientColorKey(ColorToVector3(ClassicWaveLengths), 0.65f),
				new GradientColorKey(ColorToVector3(DefaultWaveLengths), 0.71f),
				new GradientColorKey(ColorToVector3(DefaultWaveLengths), 1f)
			},
			alphaKeys = new GradientAlphaKey[2]
			{
				new GradientAlphaKey(1f, 0.0f),
				new GradientAlphaKey(1f, 1f)
			}
		};

		DayNightProperties _dayNightProperties;
		DayNightFogEffect _fogEffect;
		State _currentState;

		public void Awake()
		{
			_dayNightProperties = FindObjectOfType<DayNightProperties>();
			_fogEffect = FindObjectOfType<DayNightFogEffect>();

			_currentState = new State
			{
				UseFogColorReplacer = XmlOptionsWrapper<Options>.Options.FogColor,
				IsFogEffectEnabled = _fogEffect?.enabled ?? false,
				CurrentSkyTint = _dayNightProperties.m_SkyTint,
				CurrentTimeOfDay = _dayNightProperties.m_TimeOfDay,
				CurrentWaveLengths = _dayNightProperties.m_WaveLengths
			};
		}

		public void Update() => SetupEffect();

		public void OnDestroy() => ReplaceFogColor(false);

		void SetupEffect()
		{
			if (_fogEffect is null || NothingChanged())
			{
				return;
			}

			var useClassicFog = _fogEffect.enabled && XmlOptionsWrapper<Options>.Options.FogColor;

			ReplaceFogColor(useClassicFog);

			_currentState.IsFogEffectEnabled = _fogEffect.enabled;
			_currentState.UseFogColorReplacer = XmlOptionsWrapper<Options>.Options.FogColor;
			_currentState.CurrentSkyTint = _dayNightProperties.m_SkyTint;
			_currentState.CurrentTimeOfDay = _dayNightProperties.m_TimeOfDay;
			_currentState.CurrentWaveLengths = _dayNightProperties.m_WaveLengths;
		}

		bool NothingChanged()
		{
			return _currentState.Equals(new State
			{
				CurrentSkyTint = _dayNightProperties.m_SkyTint,
				CurrentTimeOfDay = _dayNightProperties.m_TimeOfDay,
				CurrentWaveLengths = _dayNightProperties.m_WaveLengths,
				IsFogEffectEnabled = _fogEffect.enabled,
				UseFogColorReplacer = XmlOptionsWrapper<Options>.Options.FogColor
			});
		}

		void ReplaceFogColor(bool useClassicFog)
		{
			_dayNightProperties.m_SkyTint = useClassicFog
				? ClassicSkyTintGradient.Evaluate(_dayNightProperties.normalizedTimeOfDay)
				: DefaultSkyTint;

			_dayNightProperties.m_WaveLengths = useClassicFog
				? Vector3ToColor(ClassicWaveLengthsGradient.Evaluate(_dayNightProperties.normalizedTimeOfDay))
				: DefaultWaveLengths;
		}

		static Color ColorToVector3(Vector3 vector3) =>
			new Color(vector3.x / 1000f, vector3.y / 1000f, vector3.z / 1000f);

		static Vector3 Vector3ToColor(Color color) =>
			new Vector3(color.r * 1000f, color.g * 1000f, color.b * 1000f);
	}
}