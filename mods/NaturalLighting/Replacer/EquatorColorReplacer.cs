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

		DayNightProperties _dayNightProperties;
		Gradient _defaultEquatorColor;
		FieldInfo _equatorColorField;

		bool _currentUseSofterShadows;

		public void Awake()
		{
			Debug.Log("[NaturalLighting] EquatorColorReplacer.Awake");

			_dayNightProperties = FindObjectOfType<DayNightProperties>();
			_defaultEquatorColor = _dayNightProperties.m_AmbientColor.equatorColor;
			_equatorColorField = _dayNightProperties.m_AmbientColor
				.GetType()
				.GetField("m_EquatorColor", BindingFlags.Instance | BindingFlags.NonPublic);

			var options = OptionsStore.GetOrLoadOptions();
			_currentUseSofterShadows = options.UseSofterShadows;
		}

		public void Start()
		{
			Debug.Log("[NaturalLighting] EquatorColorReplacer.Start");

			if (_currentUseSofterShadows)
			{
				ReplaceEquatorColor(true);
			}
		}

		public void Update()
		{
			var options = OptionsStore.GetOrLoadOptions();

			if (_currentUseSofterShadows != options.UseSofterShadows)
			{
				ReplaceEquatorColor(options.UseSofterShadows);

				_currentUseSofterShadows = options.UseSofterShadows;
			}
		}

		public void OnDestroy()
		{
			Debug.Log("[NaturalLighting] EquatorColorReplacer.OnDestroy");

			ReplaceEquatorColor(false);
		}

		void ReplaceEquatorColor(bool replace)
		{
			var equatorColor = replace ? SofterShadowsEquatorColor : _defaultEquatorColor;

			Debug.LogFormat("[NaturalLighting] EquatorColorReplacer.ReplaceEquatorColor: {0}", replace ? "Natural" : "Default");

			_equatorColorField.SetValue(_dayNightProperties.m_AmbientColor, equatorColor);
		}
	}
}