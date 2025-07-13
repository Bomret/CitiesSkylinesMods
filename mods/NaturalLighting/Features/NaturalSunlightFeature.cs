using Common;
using NaturalLighting.Settings;
using UnityEngine;

namespace NaturalLighting.Features
{
	/// <summary>
	/// Manages replacement of Unity's default sunlight color gradient with a more natural color progression
	/// that better simulates realistic daylight transitions throughout the day/night cycle.
	/// 
	/// This feature replaces the game's default sunlight coloring with a custom gradient that includes
	/// warmer dawn/dusk colors and cooler night tones, providing enhanced visual realism while
	/// maintaining the ability to revert to original lighting.
	/// </summary>
	sealed class NaturalSunlightFeature : Feature<ModSettings>
	{
		/// <summary>
		/// Natural sunlight color gradient with realistic color transitions throughout the day.
		/// The gradient includes 8 key points representing different times:
		/// - 0.23f (Night): Cool blue-gray tones
		/// - 0.26f (Dawn): Warm orange sunrise
		/// - 0.29f (Early Morning): Soft warm white
		/// - 0.33f-0.63f (Day): Pure white daylight
		/// - 0.72f (Evening): Soft warm white
		/// - 0.75f (Dusk): Warm orange sunset
		/// - 0.78f (Night): Cool blue-gray tones
		/// </summary>
		static readonly Gradient NaturalColor = new Gradient()
		{
			colorKeys = new GradientColorKey[8]
			{
				new GradientColorKey(new Color32( 55,  66,  77, byte.MaxValue), 0.23f),
				new GradientColorKey(new Color32( 245,  173,  84, byte.MaxValue), 0.26f),
				new GradientColorKey(new Color32( 252,  222,  186, byte.MaxValue), 0.29f),
				new GradientColorKey(new Color32( byte.MaxValue,  byte.MaxValue,  byte.MaxValue, byte.MaxValue), 0.33f),
				new GradientColorKey(new Color32( byte.MaxValue,  byte.MaxValue,  byte.MaxValue, byte.MaxValue), 0.63f),
				new GradientColorKey(new Color32( 252,  222,  186, byte.MaxValue), 0.72f),
				new GradientColorKey(new Color32( 245,  173,  84, byte.MaxValue), 0.75f),
				new GradientColorKey(new Color32( 55,  66,  77, byte.MaxValue), 0.78f)
			},
			alphaKeys = new GradientAlphaKey[2]
			{
				new GradientAlphaKey(1f, 0f),
				new GradientAlphaKey(1f, 1f)
			}
		};

		DayNightProperties _dayNightProperties;
		Gradient _defaultColor;
		bool _currentUseNaturalSunlight;

		/// <summary>
		/// Initializes a new instance of the NaturalSunlightFeature class.
		/// </summary>
		/// <param name="modProvider">Provider for accessing mod resources and metadata.</param>
		/// <param name="logger">Logger for diagnostic output and error reporting.</param>
		public NaturalSunlightFeature(ILogger logger) : base(logger) { }

		/// <summary>
		/// Called when the mod is loaded. Initializes the day/night properties manager,
		/// captures the default sunlight color for restoration purposes, and applies natural sunlight if enabled.
		/// </summary>
		/// <param name="settings">Current mod settings containing natural sunlight preferences.</param>
		public override void OnLoaded(IServiceProvider serviceProvider, ModSettings settings)
		{
			_dayNightProperties = Object.FindObjectOfType<DayNightProperties>();
			if (_dayNightProperties == null)
			{
				Logger.LogFormat(LogType.Error, "[NaturalLighting] DayNightProperties not found - natural sunlight feature disabled");
				return;
			}

			_defaultColor = _dayNightProperties.m_LightColor;

			_currentUseNaturalSunlight = settings.UseNaturalSunlight;
			if (_currentUseNaturalSunlight)
			{
				ReplaceSunlight(true);
			}
		}

		/// <summary>
		/// Called when mod settings are changed during runtime. Switches between default and natural sunlight
		/// based on the updated UseNaturalSunlight setting, but only if the setting has actually changed.
		/// </summary>
		/// <param name="settings">Updated mod settings.</param>
		public override void OnSettingsChanged(ModSettings settings)
		{
			if (_currentUseNaturalSunlight == settings.UseNaturalSunlight) return;

			ReplaceSunlight(settings.UseNaturalSunlight);

			_currentUseNaturalSunlight = settings.UseNaturalSunlight;
		}

		/// <summary>
		/// Called when the mod is being unloaded. Restores the default sunlight color to ensure
		/// the game returns to its original lighting state.
		/// </summary>
		public override void OnUnloading() => ReplaceSunlight(false);

		/// <summary>
		/// Called when the feature is being disposed. Restores the default sunlight color to clean up
		/// any modifications made to the game's lighting system.
		/// </summary>
		protected override void OnDispose() => ReplaceSunlight(false);

		/// <summary>
		/// Replaces the active sunlight color with either the natural sunlight gradient
		/// or restores the original default gradient based on the useNatural parameter.
		/// </summary>
		/// <param name="useNatural">True to use the natural sunlight gradient, false to restore the original default gradient.</param>
		void ReplaceSunlight(bool useNatural)
		{
			if (_dayNightProperties == null)
			{
				Logger.LogFormat(LogType.Warning, "[NaturalLighting] Cannot replace sunlight: DayNightProperties is null");
				return;
			}

			Logger.LogFormat(LogType.Log, "[NaturalLighting] NaturalSunlight.ReplaceSunlight: {0}", useNatural ? "Natural" : "Default");

			_dayNightProperties.m_LightColor = useNatural ? NaturalColor : _defaultColor;
		}
	}
}