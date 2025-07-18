using Common;
using UnityEngine;

namespace NaturalLighting.Features.SunShafts
{
	/// <summary>
	/// Handles distance-based quality scaling optimization for sun shafts effect.
	/// Scales quality based on how far the sun is from the screen center.
	/// </summary>
	sealed class DistanceBasedQualityScaling
	{
		readonly float _edgeThreshold;
		readonly float _centerRadius;
		readonly int _minIterations;
		readonly int _maxIterations;

		public DistanceBasedQualityScaling(
			float edgeThreshold = 0.85f,
			float centerRadius = 0.3f,
			int minIterations = 1,
			int maxIterations = 4)
		{
			_edgeThreshold = edgeThreshold;
			_centerRadius = centerRadius;
			_minIterations = minIterations;
			_maxIterations = maxIterations;
		}

		/// <summary>
		/// Calculate quality scaling factors based on sun position on screen.
		/// Returns scaling factors for resolution, iterations, and intensity.
		/// </summary>
		/// <param name="sunScreenPosition">Sun position in viewport coordinates (0-1)</param>
		/// <param name="baseIterations">Base number of blur iterations</param>
		/// <returns>Quality settings adjusted for distance from screen center</returns>
		public bool TryCalculateQualitySettings(Vector3 sunScreenPosition, int baseIterations, out QualitySettings? qualitySettings)
		{
			qualitySettings = null;

			var distanceFromCenter = Vector2.Distance(
				new Vector2(sunScreenPosition.x, sunScreenPosition.y),
				new Vector2(0.5f, 0.5f)
			);

			var normalizedDistance = Mathf.Clamp01(distanceFromCenter / 0.707f);

			var isNearEdge =
				sunScreenPosition.x < 0.05f ||
				sunScreenPosition.x > 0.95f ||
				sunScreenPosition.y < 0.05f ||
				sunScreenPosition.y > 0.95f;

			if (isNearEdge)
			{
				// skip effect if sun is near the screen's edge
				return false;
			}

			// Calculate quality scaling based on distance from center
			var qualityScale = CalculateQualityScale(normalizedDistance);

			// Resolution scaling: use lower resolution when far from center
			var resolutionDivisor = 4; // Start with baseline quarter resolution
			if (normalizedDistance > _edgeThreshold)
			{
				resolutionDivisor = 8; // Eighth resolution near edges
			}
			else if (normalizedDistance > _centerRadius)
			{
				resolutionDivisor = 6; // Sixth resolution in transition zone
			}

			// Iteration scaling: reduce blur iterations when effect is less visible
			var scaledIterations = Mathf.RoundToInt(baseIterations * qualityScale);
			scaledIterations = Mathf.Clamp(scaledIterations, _minIterations, _maxIterations);

			// Intensity scaling: reduce intensity near edges (effect less noticeable there)
			var intensityMultiplier = Mathf.Lerp(0.7f, 1.0f, 1.0f - normalizedDistance);

			// Skip border clearing for performance when using lower quality
			var shouldSkipBorderClearing = resolutionDivisor > 4 || scaledIterations < 2;

			qualitySettings = new QualitySettings(resolutionDivisor, scaledIterations, intensityMultiplier, false, shouldSkipBorderClearing);

			return true;
		}

		/// <summary>
		/// Calculate quality scale based on normalized distance from center.
		/// Uses a smooth curve to transition between full and reduced quality.
		/// </summary>
		float CalculateQualityScale(float normalizedDistance)
		{
			if (normalizedDistance <= _centerRadius)
			{
				// Full quality in center area
				return 1.0f;
			}
			if (normalizedDistance >= _edgeThreshold)
			{
				// Minimum quality near edges
				return 0.3f;
			}

			// Smooth transition between center and edge
			var t = (normalizedDistance - _centerRadius) / (_edgeThreshold - _centerRadius);

			// Use smoothstep for nice transition curve
			var smoothT = t * t * (3.0f - 2.0f * t);

			return Mathf.Lerp(1.0f, 0.3f, smoothT);
		}
	}
}