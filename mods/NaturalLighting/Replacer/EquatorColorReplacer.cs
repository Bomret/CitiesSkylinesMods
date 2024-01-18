using System.Reflection;
using UnityEngine;

namespace NaturalLighting.Replacer
{
	sealed class EquatorColorReplacer : Replacer
	{
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
			alphaKeys = new GradientAlphaKey[]
			{
				new GradientAlphaKey(1f, 0f),
				new GradientAlphaKey(1f, 1f)
			}
		};
		FieldInfo _equatorColorField;
		Gradient _defaultEquatorColor;
		bool _currentUseSofterShadows;

		public void Awake()
		{
			_defaultEquatorColor = FindObjectOfType<DayNightProperties>().m_AmbientColor.equatorColor;
			_equatorColorField = typeof(DayNightProperties.AmbientColor)
				.GetField("m_EquatorColor", BindingFlags.Instance | BindingFlags.NonPublic);

			var settings = ModSettingsStore.GetOrLoadSettings();

			_currentUseSofterShadows = settings.UseSofterShadows;
		}

		public void Start()
		{
			if (!_currentUseSofterShadows) return;

			ReplaceEquatorColor(true);
		}

		public void Update()
		{
			var settings = ModSettingsStore.GetOrLoadSettings();

			if (_currentUseSofterShadows == settings.UseSofterShadows) return;

			ReplaceEquatorColor(settings.UseSofterShadows);

			_currentUseSofterShadows = settings.UseSofterShadows;
		}

		public void OnDestroy() => ReplaceEquatorColor(false);

		void ReplaceEquatorColor(bool useNatural)
		{
			var equatorColor = useNatural ? SofterShadowsEquatorColor : _defaultEquatorColor;

			Debug.LogFormat("[NaturalLighting] EquatorColorReplacer.ReplaceEquatorColor: {0}", useNatural ? "Natural" : "Default");

			_equatorColorField
				.SetValue(DayNightProperties.instance.m_AmbientColor, equatorColor);
		}
	}
}