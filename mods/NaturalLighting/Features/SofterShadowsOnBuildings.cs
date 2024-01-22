using System.Reflection;
using NaturalLighting.Settings;
using UnityEngine;

namespace NaturalLighting.Features
{
	sealed class SofterShadowsOnBuildings : Feature<ModSettings>
	{
		static readonly Gradient SofterShadowsEquatorColor = new Gradient
		{
			colorKeys = new GradientColorKey[6]
				{
					new GradientColorKey(new Color32(20, 25, 36, byte.MaxValue), 0.225f),
					new GradientColorKey(new Color32(100, 90, 70, byte.MaxValue), 0.26f),
					new GradientColorKey(new Color32(152, 188, 218, byte.MaxValue), 0.30f),
					new GradientColorKey(new Color32(152, 188, 218, byte.MaxValue), 0.70f),
					new GradientColorKey(new Color32(100, 90, 70, byte.MaxValue), 0.76f),
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

		public SofterShadowsOnBuildings(ILogger logger)
		{
			_logger = logger;
		}

		public override void OnLoaded(ModSettings settings)
		{
			_defaultEquatorColor = Object.FindObjectOfType<DayNightProperties>().m_AmbientColor.equatorColor;
			_equatorColorField = typeof(DayNightProperties.AmbientColor)
				.GetField("m_EquatorColor", BindingFlags.Instance | BindingFlags.NonPublic);

			_currentUseSofterShadows = settings.UseSofterShadowsOnBuildings;
			if (!_currentUseSofterShadows) return;

			ReplaceEquatorColor(true);
		}

		public override void OnSettingsChanged(ModSettings settings)
		{
			if (_currentUseSofterShadows == settings.UseSofterShadowsOnBuildings) return;

			ReplaceEquatorColor(settings.UseSofterShadowsOnBuildings);

			_currentUseSofterShadows = settings.UseSofterShadowsOnBuildings;
		}

		public override void OnUnloading() => ReplaceEquatorColor(false);

		protected override void OnDispose() => ReplaceEquatorColor(false);

		void ReplaceEquatorColor(bool useNatural)
		{
			_logger.LogFormat(LogType.Log, "[NaturalLighting] SofterShadowsOnBuildings.ReplaceEquatorColor: {0}", useNatural ? "Natural" : "Default");

			_equatorColorField
				.SetValue(DayNightProperties.instance.m_AmbientColor, useNatural ? SofterShadowsEquatorColor : _defaultEquatorColor);
		}
	}
}