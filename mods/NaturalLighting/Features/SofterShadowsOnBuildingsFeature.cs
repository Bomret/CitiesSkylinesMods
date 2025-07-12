using System.Reflection;
using NaturalLighting.Settings;
using UnityEngine;

namespace NaturalLighting.Features
{
	/// <summary>
	/// Manages replacement of Unity's default ambient equator color gradient to create softer shadows on buildings.
	/// 
	/// This feature modifies the ambient lighting system by replacing the equator color component of the ambient
	/// color gradient, which affects how shadows appear on building surfaces. The custom gradient provides more
	/// natural and softer shadow transitions throughout the day/night cycle while maintaining visual coherence.
	/// 
	/// Uses reflection to access Unity's private ambient color fields since they are not exposed through public APIs.
	/// </summary>
	sealed class SofterShadowsOnBuildingsFeature : Feature<ModSettings>
	{
		/// <summary>
		/// Softer shadow equator color gradient with natural color transitions throughout the day.
		/// The gradient includes 6 key points representing different lighting conditions:
		/// - 0.225f (Night): Dark blue-gray for night shadows
		/// - 0.26f (Dawn): Warm brown for sunrise shadows
		/// - 0.30f-0.70f (Day): Light blue for daylight shadows
		/// - 0.76f (Dusk): Warm brown for sunset shadows
		/// - 0.775f (Night): Dark blue-gray for night shadows
		/// </summary>
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

		FieldInfo _equatorColorField;
		Gradient _defaultEquatorColor;
		bool _currentUseSofterShadows;

		/// <summary>
		/// Initializes a new instance of the SofterShadowsOnBuildingsFeature class.
		/// </summary>
		/// <param name="modProvider">Provider for accessing mod resources and metadata.</param>
		/// <param name="logger">Logger for diagnostic output and error reporting.</param>
		public SofterShadowsOnBuildingsFeature(ILogger logger) : base(logger) { }

		/// <summary>
		/// Called when the mod is loaded. Initializes reflection access to Unity's ambient color system,
		/// captures the default equator color for restoration purposes, and applies softer shadows if enabled.
		/// </summary>
		/// <param name="settings">Current mod settings containing softer shadows preferences.</param>
		public override void OnLoaded(IObjectProvider objectProvider, ModSettings settings)
		{
			try
			{
				var dayNightProperties = Object.FindObjectOfType<DayNightProperties>();
				if (dayNightProperties == null)
				{
					Logger.LogFormat(LogType.Error, "[NaturalLighting] DayNightProperties not found - softer shadows feature disabled");
					return;
				}

				_defaultEquatorColor = dayNightProperties.m_AmbientColor.equatorColor;

				_equatorColorField = typeof(DayNightProperties.AmbientColor)
					.GetField("m_EquatorColor", BindingFlags.Instance | BindingFlags.NonPublic);

				if (_equatorColorField == null)
				{
					Logger.LogFormat(LogType.Error, "[NaturalLighting] Could not access m_EquatorColor field via reflection - softer shadows feature disabled");
					return;
				}

				_currentUseSofterShadows = settings.UseSofterShadowsOnBuildings;
				if (!_currentUseSofterShadows) return;

				ReplaceEquatorColor(true);
			}
			catch (System.Exception ex)
			{
				Logger.LogFormat(LogType.Error, "[NaturalLighting] Failed to initialize SofterShadowsOnBuildings: {0}", ex.Message);
			}
		}

		/// <summary>
		/// Called when mod settings are changed during runtime. Switches between default and softer shadow
		/// equator colors based on the updated UseSofterShadowsOnBuildings setting, but only if the setting has actually changed.
		/// </summary>
		/// <param name="settings">Updated mod settings.</param>
		public override void OnSettingsChanged(ModSettings settings)
		{
			if (_currentUseSofterShadows == settings.UseSofterShadowsOnBuildings) return;

			ReplaceEquatorColor(settings.UseSofterShadowsOnBuildings);

			_currentUseSofterShadows = settings.UseSofterShadowsOnBuildings;
		}

		/// <summary>
		/// Called when the mod is being unloaded. Restores the default equator color to ensure
		/// the game returns to its original ambient lighting state.
		/// </summary>
		public override void OnUnloading() => ReplaceEquatorColor(false);

		/// <summary>
		/// Called when the feature is being disposed. Restores the default equator color to clean up
		/// any modifications made to the game's ambient lighting system.
		/// </summary>
		protected override void OnDispose() => ReplaceEquatorColor(false);

		/// <summary>
		/// Replaces the ambient equator color with either the softer shadows gradient
		/// or restores the original default gradient based on the useNatural parameter.
		/// 
		/// Uses reflection to modify Unity's private ambient color fields since they are not
		/// accessible through public APIs.
		/// </summary>
		/// <param name="useNatural">True to use the softer shadows gradient, false to restore the original default gradient.</param>
		void ReplaceEquatorColor(bool useNatural)
		{
			if (_equatorColorField == null)
			{
				Logger.LogFormat(LogType.Warning, "[NaturalLighting] Cannot replace equator color: reflection field is null");
				return;
			}

			if (DayNightProperties.instance == null)
			{
				Logger.LogFormat(LogType.Warning, "[NaturalLighting] Cannot replace equator color: DayNightProperties.instance is null");
				return;
			}

			Logger.LogFormat(LogType.Log, "[NaturalLighting] SofterShadowsOnBuildings.ReplaceEquatorColor: {0}", useNatural ? "Natural" : "Default");

			try
			{
				_equatorColorField
					.SetValue(DayNightProperties.instance.m_AmbientColor, useNatural ? SofterShadowsEquatorColor : _defaultEquatorColor);
			}
			catch (System.Exception ex)
			{
				Logger.LogFormat(LogType.Error, "[NaturalLighting] Failed to set equator color via reflection: {0}", ex.Message);
			}
		}
	}
}