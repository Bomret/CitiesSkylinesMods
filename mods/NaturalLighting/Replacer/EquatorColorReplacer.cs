using System.Reflection;
using NaturalLighting.Settings;
using UnityEngine;

namespace NaturalLighting.Replacer
{
	sealed class EquatorColorReplacer : Replacer<NaturalLightingSettings>
	{
		static readonly Gradient SofterShadowsEquatorColor = new Gradient
		{
			colorKeys = new GradientColorKey[6]
				{
					new GradientColorKey(new Color32(20, 25, 36, byte.MaxValue), 0.225f),
					new GradientColorKey(new Color32(80, 70, 50, byte.MaxValue), 0.25f),
					new GradientColorKey(new Color32(152, 188, 218, byte.MaxValue), 0.28f),
					new GradientColorKey(new Color32(152, 188, 218, byte.MaxValue), 0.72f),
					new GradientColorKey(new Color32(80, 70, 50, byte.MaxValue), 0.75f),
					new GradientColorKey(new Color32(20, 25, 36, byte.MaxValue), 0.775f)
				},
			alphaKeys = new GradientAlphaKey[2]
			{
				new GradientAlphaKey(1f, 0f),
				new GradientAlphaKey(1f, 1f)
			}
		};

		readonly ILogger _logger;

		FieldInfo _equatorColorField;
		Gradient _defaultEquatorColor;
		bool _currentUseSofterShadows;

		public EquatorColorReplacer(ILogger logger)
		{
			_logger = logger;
		}

		public override void OnLoaded(NaturalLightingSettings settings)
		{
			_defaultEquatorColor = Object.FindObjectOfType<DayNightProperties>().m_AmbientColor.equatorColor;
			_equatorColorField = typeof(DayNightProperties.AmbientColor)
				.GetField("m_EquatorColor", BindingFlags.Instance | BindingFlags.NonPublic);

			_currentUseSofterShadows = settings.UseSofterShadows;
			if (!_currentUseSofterShadows) return;

			ReplaceEquatorColor(true);
		}

		public override void OnSettingsChanged(NaturalLightingSettings settings)
		{
			if (_currentUseSofterShadows == settings.UseSofterShadows) return;

			ReplaceEquatorColor(settings.UseSofterShadows);

			_currentUseSofterShadows = settings.UseSofterShadows;
		}

		public override void OnUnloading() => ReplaceEquatorColor(false);

		protected override void OnDispose() => ReplaceEquatorColor(false);

		void ReplaceEquatorColor(bool useNatural)
		{
			var equatorColor = useNatural ? SofterShadowsEquatorColor : _defaultEquatorColor;

			_logger.LogFormat(LogType.Log, "[NaturalLighting] EquatorColorReplacer.ReplaceEquatorColor: {0}", useNatural ? "Natural" : "Default");

			_equatorColorField
				.SetValue(DayNightProperties.instance.m_AmbientColor, equatorColor);
		}
	}
}