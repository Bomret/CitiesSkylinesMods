using System.Reflection;
using OptionsFramework;
using UnityEngine;

namespace DaylightClassicReborn
{
	sealed class ShadowReplacer : MonoBehaviour
	{
		static readonly Gradient DefaultEquatorColor = new Gradient
		{
			colorKeys = new GradientColorKey[6]
			{
				new GradientColorKey(new Color32(20, 25, 36, byte.MaxValue), 0.225f),
				new GradientColorKey(new Color32(80, 70, 50, byte.MaxValue), 0.25f),
				new GradientColorKey(new Color32(102, 138, 168, byte.MaxValue), 0.28f),
				new GradientColorKey(new Color32(102, 138, 168, byte.MaxValue), 0.72f),
				new GradientColorKey(new Color32(80, 70, 50, byte.MaxValue), 0.75f),
				new GradientColorKey(new Color32(20, 25, 36, byte.MaxValue), 0.775f)
			},
			alphaKeys = gradientAlphaKeys
		};

		static readonly Gradient SofterShadowsEquatorColor = new Gradient
		{
			colorKeys = new GradientColorKey[]
				{
					new GradientColorKey(new Color32(20, 25, 36, byte.MaxValue), 0.225f),
					new GradientColorKey(new Color32(80, 70, 50, byte.MaxValue), 0.25f),
					new GradientColorKey(new Color32(byte.MaxValue, byte.MaxValue, byte.MaxValue, byte.MaxValue), 0.28f),
					new GradientColorKey(new Color32(byte.MaxValue, byte.MaxValue, byte.MaxValue, byte.MaxValue), 0.72f),
					new GradientColorKey(new Color32(80, 70, 50, byte.MaxValue), 0.75f),
					new GradientColorKey(new Color32(20, 25, 36, byte.MaxValue), 0.775f)
				},
			alphaKeys = gradientAlphaKeys
		};

		static readonly GradientAlphaKey[] gradientAlphaKeys = new GradientAlphaKey[]
		{
			new GradientAlphaKey(1f, 0f),
			new GradientAlphaKey(1f, 1f)
		};

		DayNightProperties _dayNightProperties;
		Gradient _currentEquatorColor;
		bool _useShadowReplacer;

		public void Awake()
		{
			_dayNightProperties = FindObjectOfType<DayNightProperties>();
			_currentEquatorColor = _dayNightProperties.m_AmbientColor.equatorColor;
			_useShadowReplacer = XmlOptionsWrapper<Options>.Options.SofterShadows;

			SetupEffect();
		}

		public void Update() => SetupEffect();

		public void OnDestroy() => ReplaceEquatorColor(DefaultEquatorColor);

		void SetupEffect()
		{
			if (NothingChanged()) return;

			var equatorColor = XmlOptionsWrapper<Options>.Options.SofterShadows
				? SofterShadowsEquatorColor
				: DefaultEquatorColor;

			ReplaceEquatorColor(equatorColor);

			_currentEquatorColor = equatorColor;
		}

		static void ReplaceEquatorColor(Gradient equatorColor)
		{
			typeof(DayNightProperties.AmbientColor)
				.GetField("m_EquatorColor", BindingFlags.Instance | BindingFlags.NonPublic)
				.SetValue(DayNightProperties.instance.m_AmbientColor, equatorColor);
		}

		bool NothingChanged() =>
			_useShadowReplacer == XmlOptionsWrapper<Options>.Options.SofterShadows &&
			_dayNightProperties.m_AmbientColor.equatorColor == _currentEquatorColor;
	}
}