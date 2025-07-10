using UnityEngine;

namespace NaturalLighting.Features.SunShafts
{
	/// <summary>
	/// Represents quality settings for sun shafts rendering optimization.
	/// </summary>
	internal readonly struct QualitySettings
	{
		/// <summary>
		/// Resolution divisor for render textures (1 = full res, 2 = half res, 4 = quarter res)
		/// </summary>
		public int ResolutionDivisor { get; }

		/// <summary>
		/// Number of blur iterations to perform
		/// </summary>
		public int BlurIterations { get; }

		/// <summary>
		/// Intensity multiplier to apply to the effect
		/// </summary>
		public float IntensityMultiplier { get; }

		/// <summary>
		/// Whether to skip the effect entirely
		/// </summary>
		public bool ShouldSkipEffect { get; }

		/// <summary>
		/// Whether to skip border clearing operations for performance
		/// </summary>
		public bool ShouldSkipBorderClearing { get; }

		public QualitySettings(int resolutionDivisor, int blurIterations, float intensityMultiplier,
			bool shouldSkipEffect = false, bool shouldSkipBorderClearing = false)
		{
			ResolutionDivisor = resolutionDivisor;
			BlurIterations = blurIterations;
			IntensityMultiplier = intensityMultiplier;
			ShouldSkipEffect = shouldSkipEffect;
			ShouldSkipBorderClearing = shouldSkipBorderClearing;
		}
	}
}