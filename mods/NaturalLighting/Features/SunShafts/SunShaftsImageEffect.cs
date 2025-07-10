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

		void OnRenderImage(RenderTexture source, RenderTexture destination)
		{
			if (_sunShaftsMaterial == null || _sunLight == null)
			{
				Graphics.Blit(source, destination);
				return;
			}

			try
			{
				var camera = GetComponent<Camera>();
				if (camera == null)
				{
					Graphics.Blit(source, destination);
					return;
				}

				// Update sunTransform position every frame relative to camera position
				if (_sunTransform != null)
				{
					_sunTransform.position = camera.transform.position - _sunLight.transform.forward * 2000f;
				}

				// Calculate sun screen position using the updated transform
				Vector3 sunScreenPosition;
				if (_sunTransform != null)
				{
					sunScreenPosition = camera.WorldToViewportPoint(_sunTransform.position);
				}
				else
				{
					// Fallback: calculate directly
					var sunWorldPosition = camera.transform.position - (_sunLight.transform.forward * 2000f);
					sunScreenPosition = camera.WorldToViewportPoint(sunWorldPosition);
				}

				// Enable depth texture mode
				camera.depthTextureMode |= DepthTextureMode.Depth;

				// Only render effect when sun is in front of camera (z > 0)
				if (sunScreenPosition.z > 0)
				{
					ApplySunShaftsEffect(source, destination, sunScreenPosition);
				}
				else
				{
					// Sun not visible (behind camera), just pass through
					Graphics.Blit(source, destination);
				}
			}
			catch (Exception err)
			{
				_logger?.LogFormat(LogType.Error, "[NaturalLighting] SunShaftsImageEffect error: {0}", err.Message);

				Graphics.Blit(source, destination);
			}
		}

		void ApplySunShaftsEffect(RenderTexture source, RenderTexture destination, Vector3 sunScreenPosition)
		{
			if (!_distanceOptimization.TryCalculateQualitySettings(sunScreenPosition, _blurIterations, out var quality) && quality.HasValue)
			{
				// Skip effect entirely if false
				Graphics.Blit(source, destination);

				return;
			}

			var rtWidth = source.width / quality.Value.ResolutionDivisor;
			var rtHeight = source.height / quality.Value.ResolutionDivisor;
			var iterations = quality.Value.BlurIterations;
			var intensity = _intensity * quality.Value.IntensityMultiplier;

			// Step 1: Bright pass - extract bright areas using Pass 2 (with depth texture)
			var brightPass = RenderTexture.GetTemporary(rtWidth, rtHeight, 0);
			brightPass.filterMode = FilterMode.Bilinear;

			// Set parameters like the reference implementation BEFORE bright pass
			_sunShaftsMaterial.SetVector("_BlurRadius4", new Vector4(1f, 1f, 0f, 0f) * _blurRadius);
			_sunShaftsMaterial.SetVector("_SunPosition", new Vector4(sunScreenPosition.x, sunScreenPosition.y, sunScreenPosition.z, 0.75f)); // Include Z and maxRadius (reference value)
			_sunShaftsMaterial.SetVector("_SunThreshold", new Vector4(_threshold, _threshold, _threshold, _threshold));

			// Use Pass 2 for bright pass (with depth texture)
			Graphics.Blit(source, brightPass, _sunShaftsMaterial, 2);

			// Step 2: Radial blur iterations using Pass 1
			iterations = Mathf.Clamp(iterations, 1, 4);

			var baseBlurRadius = _blurRadius * (1f / 768f);
			_sunShaftsMaterial.SetVector("_BlurRadius4", new Vector4(baseBlurRadius, baseBlurRadius, 0f, 0f));
			_sunShaftsMaterial.SetVector("_SunPosition", new Vector4(sunScreenPosition.x, sunScreenPosition.y, sunScreenPosition.z, 0.75f));

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
				_sunShaftsMaterial.SetVector("_BlurRadius4", new Vector4(dynamicBlurRadius, dynamicBlurRadius, 0f, 0f));

				// Second blur pass
				blurred = RenderTexture.GetTemporary(rtWidth, rtHeight, 0);
				blurred.filterMode = FilterMode.Bilinear;
				Graphics.Blit(blurTexture, blurred, _sunShaftsMaterial, 1); // Pass 1: Radial blur

				RenderTexture.ReleaseTemporary(blurTexture);

				// Update blur radius for next iteration
				var blurRadius2 = _blurRadius * (((i * 2f + 2f) * 6f) / 768f);
				_sunShaftsMaterial.SetVector("_BlurRadius4", new Vector4(blurRadius2, blurRadius2, 0f, 0f));
			}

			// Step 3: Final composite using Pass 0 (Screen blend)
			_sunShaftsMaterial.SetVector("_SunColor",
				new Vector4(_sunLight.color.r, _sunLight.color.g, _sunLight.color.b, _sunLight.color.a) * intensity);

			_sunShaftsMaterial.SetTexture("_ColorBuffer", blurred);

			// Use Pass 0 for Screen blend mode (final composite)
			Graphics.Blit(source, destination, _sunShaftsMaterial, 0);

			// Cleanup
			RenderTexture.ReleaseTemporary(brightPass);
			if (blurred != brightPass) RenderTexture.ReleaseTemporary(blurred);
		}
	}
}
