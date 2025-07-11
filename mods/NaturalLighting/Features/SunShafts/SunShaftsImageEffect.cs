using System;
using UnityEngine;

namespace NaturalLighting.Features.SunShafts
{
	sealed class SunShaftsImageEffect : MonoBehaviour
	{
		Light _sunLight;
		Transform _sunTransform;
		Material _sunShaftsMaterial;
		ILogger _logger;

		float _intensity;
		float _threshold;
		float _blurRadius;
		int _blurIterations;

		DistanceBasedQualityScaling _distanceOptimization;

		// Cached shader parameters to avoid redundant SetVector calls
		Vector4 _cachedSunPosition;
		Vector4 _cachedBlurRadius;
		Vector4 _cachedSunThreshold;
		Vector4 _cachedSunColor;

		public void Initialize(Light sunLight, Transform sunTransform, Material sunShaftsMaterial,
			float intensity, float threshold, float blurRadius, int blurIterations, ILogger logger)
		{
			_sunLight = sunLight;
			_sunTransform = sunTransform;
			_sunShaftsMaterial = sunShaftsMaterial;
			_intensity = intensity;
			_threshold = threshold;
			_blurRadius = blurRadius;
			_blurIterations = blurIterations;
			_logger = logger;
			_distanceOptimization = new DistanceBasedQualityScaling();
		}

		// Helper methods to reduce redundant Material.SetVector calls
		void SetSunPositionIfChanged(Vector4 newSunPosition)
		{
			if (_cachedSunPosition != newSunPosition)
			{
				_cachedSunPosition = newSunPosition;
				_sunShaftsMaterial.SetVector("_SunPosition", newSunPosition);
			}
		}

		void SetBlurRadiusIfChanged(Vector4 newBlurRadius)
		{
			if (_cachedBlurRadius != newBlurRadius)
			{
				_cachedBlurRadius = newBlurRadius;
				_sunShaftsMaterial.SetVector("_BlurRadius4", newBlurRadius);
			}
		}

		void SetSunThresholdIfChanged(Vector4 newSunThreshold)
		{
			if (_cachedSunThreshold != newSunThreshold)
			{
				_cachedSunThreshold = newSunThreshold;
				_sunShaftsMaterial.SetVector("_SunThreshold", newSunThreshold);
			}
		}

		void SetSunColorIfChanged(Vector4 newSunColor)
		{
			if (_cachedSunColor != newSunColor)
			{
				_cachedSunColor = newSunColor;
				_sunShaftsMaterial.SetVector("_SunColor", newSunColor);
			}
		}

		void OnRenderImage(RenderTexture source, RenderTexture destination)
		{
			try
			{
				var camera = GetComponent<Camera>();
				if (camera == null)
				{
					Graphics.Blit(source, destination);
					return;
				}

				// Update sunTransform position every frame relative to camera position
				_sunTransform.position = camera.transform.position - _sunLight.transform.forward * 2000f;

				// Calculate sun screen position using the updated transform
				var sunScreenPosition = camera.WorldToViewportPoint(_sunTransform.position);

				// Enable depth texture mode
				camera.depthTextureMode |= DepthTextureMode.Depth;

				if (sunScreenPosition.z <= 0 ||
					!_distanceOptimization.TryCalculateQualitySettings(sunScreenPosition, _blurIterations, out var quality) ||
					!quality.HasValue)
				{
					// Sun not visible or effect disabled by quality settings, just pass through
					Graphics.Blit(source, destination);

					return;
				}

				ApplySunShaftsEffect(source, destination, sunScreenPosition, quality.Value);
			}
			catch (Exception err)
			{
				_logger?.LogFormat(LogType.Error, "[NaturalLighting] SunShaftsImageEffect error: {0}", err.Message);

				Graphics.Blit(source, destination);
			}
		}

		void ApplySunShaftsEffect(RenderTexture source, RenderTexture destination, Vector3 sunScreenPosition, QualitySettings quality)
		{
			var rtWidth = source.width / quality.ResolutionDivisor;
			var rtHeight = source.height / quality.ResolutionDivisor;
			var iterations = quality.BlurIterations;
			var intensity = _intensity * quality.IntensityMultiplier;

			// Step 1: Bright pass - extract bright areas using Pass 2 (with depth texture)
			var brightPass = RenderTexture.GetTemporary(rtWidth, rtHeight, 0);
			brightPass.filterMode = FilterMode.Bilinear;

			// Set parameters for bright pass (only update if changed)
			var sunPosition = new Vector4(sunScreenPosition.x, sunScreenPosition.y, sunScreenPosition.z, 0.75f);
			var sunThreshold = new Vector4(_threshold, _threshold, _threshold, _threshold);

			SetBlurRadiusIfChanged(new Vector4(1f, 1f, 0f, 0f) * _blurRadius);
			SetSunPositionIfChanged(sunPosition);
			SetSunThresholdIfChanged(sunThreshold);

			// Use Pass 2 for bright pass (with depth texture)
			Graphics.Blit(source, brightPass, _sunShaftsMaterial, 2);

			// Step 2: Radial blur iterations using Pass 1
			iterations = Mathf.Clamp(iterations, 1, 4);

			var baseBlurRadius = _blurRadius * (1f / 768f);
			SetBlurRadiusIfChanged(new Vector4(baseBlurRadius, baseBlurRadius, 0f, 0f));
			// Sun position already set above, no need to set again

			var blurred = brightPass;

			for (var i = 0; i < iterations; i++)
			{
				// First blur pass
				var blurTexture = RenderTexture.GetTemporary(rtWidth, rtHeight, 0);
				blurTexture.filterMode = FilterMode.Bilinear;
				Graphics.Blit(blurred, blurTexture, _sunShaftsMaterial, 1); // Pass 1: Radial blur

				if (blurred != brightPass)
				{
					RenderTexture.ReleaseTemporary(blurred);
				}

				var dynamicBlurRadius = _blurRadius * ((i * 2f + 1f) * 6f / 768f);
				SetBlurRadiusIfChanged(new Vector4(dynamicBlurRadius, dynamicBlurRadius, 0f, 0f));

				// Second blur pass
				blurred = RenderTexture.GetTemporary(rtWidth, rtHeight, 0);
				blurred.filterMode = FilterMode.Bilinear;
				Graphics.Blit(blurTexture, blurred, _sunShaftsMaterial, 1); // Pass 1: Radial blur

				RenderTexture.ReleaseTemporary(blurTexture);

				// Update blur radius for next iteration
				var blurRadius2 = _blurRadius * (((i * 2f + 2f) * 6f) / 768f);
				SetBlurRadiusIfChanged(new Vector4(blurRadius2, blurRadius2, 0f, 0f));
			}

			// Step 3: Final composite using Pass 0 (Screen blend)
			var sunColor = (Vector4)_sunLight.color * intensity;
			SetSunColorIfChanged(sunColor);

			_sunShaftsMaterial.SetTexture("_ColorBuffer", blurred);

			// Use Pass 0 for Screen blend mode (final composite)
			Graphics.Blit(source, destination, _sunShaftsMaterial, 0);

			// Cleanup
			RenderTexture.ReleaseTemporary(brightPass);
			if (blurred != brightPass) RenderTexture.ReleaseTemporary(blurred);
		}
	}
}
