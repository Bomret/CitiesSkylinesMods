using UnityEngine;

namespace NaturalLighting.Features.NaturalSunlight
{
	/// <summary>
	/// Contains static gradient definitions for natural sunlight effects.
	/// </summary>
	static class NaturalSunlightConstants
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
		public static readonly Gradient NaturalColor = new Gradient()
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
	}
}
