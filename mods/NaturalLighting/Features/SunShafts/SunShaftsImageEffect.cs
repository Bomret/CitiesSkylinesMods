using System;
using UnityEngine;

namespace NaturalLighting.Features.SunShafts
{
	sealed class SunShaftsImageEffect : MonoBehaviour
	{
		// Constants for sun shafts effect calculations
		const float SUN_DISTANCE = 2000f;                    // Distance of sun transform from camera
		const float SUN_MAX_RADIUS = 0.75f;                  // Maximum radius for sun falloff
		const float REFERENCE_RESOLUTION = 768f;             // Baseline resolution for blur scaling
		const float SHADER_SAMPLE_COUNT = 6f;                // Number of samples in optimized radial blur shader
		const float BLUR_RESOLUTION_SCALE = 1f / REFERENCE_RESOLUTION; // Base blur scaling factor
		const float BLUR_SCALE_FACTOR = SHADER_SAMPLE_COUNT / REFERENCE_RESOLUTION; // Combined blur scaling factor

		Light _sunLight;
		Transform _sunTransform;
		Material _sunShaftsMaterial;
		ILogger _logger;

		float _intensity;
		float _threshold;
		float _blurRadius;
		int _blurIterations;

		DistanceBasedQualityScaling _distanceOptimization;

		// RenderTexture pooling to avoid allocation overhead
		RenderTexture _pooledBrightPass;
		RenderTexture _pooledBlurTexture1;
		RenderTexture _pooledBlurTexture2;
		int _lastPooledWidth = -1;
		int _lastPooledHeight = -1;

		// Cached shader parameters to avoid redundant SetVector calls
		Vector4 _cachedSunPosition;
		Vector4 _cachedBlurRadius;
		Vector4 _cachedSunThreshold;
		Vector4 _cachedSunColor;

		// Pre-calculated Vector4 patterns to avoid allocations in hot path
		static readonly Vector4 BLUR_RADIUS_BASE_PATTERN = new Vector4(1f, 1f, 0f, 0f);
		Vector4 _cachedThresholdVector;
		float _lastThreshold = float.MinValue;

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

		// RenderTexture pooling methods to reduce allocation overhead
		void EnsurePooledTextures(int width, int height)
		{
			// Only recreate textures if dimensions changed
			if (_lastPooledWidth != width || _lastPooledHeight != height)
			{
				ReleasePooledTextures();

				_pooledBrightPass = new RenderTexture(width, height, 0) { filterMode = FilterMode.Bilinear };
				_pooledBlurTexture1 = new RenderTexture(width, height, 0) { filterMode = FilterMode.Bilinear };
				_pooledBlurTexture2 = new RenderTexture(width, height, 0) { filterMode = FilterMode.Bilinear };

				_lastPooledWidth = width;
				_lastPooledHeight = height;
			}
		}

		void ReleasePooledTextures()
		{
			if (_pooledBrightPass != null)
			{
				_pooledBrightPass.Release();
				DestroyImmediate(_pooledBrightPass);
				_pooledBrightPass = null;
			}

			if (_pooledBlurTexture1 != null)
			{
				_pooledBlurTexture1.Release();
				DestroyImmediate(_pooledBlurTexture1);
				_pooledBlurTexture1 = null;
			}

			if (_pooledBlurTexture2 != null)
			{
				_pooledBlurTexture2.Release();
				DestroyImmediate(_pooledBlurTexture2);
				_pooledBlurTexture2 = null;
			}

			_lastPooledWidth = -1;
			_lastPooledHeight = -1;
		}

		void OnDestroy()
		{
			ReleasePooledTextures();
		}

		// Helper method to get cached threshold vector
		Vector4 GetCachedThresholdVector()
		{
			if (_lastThreshold != _threshold)
			{
				_cachedThresholdVector = new Vector4(_threshold, _threshold, _threshold, _threshold);
				_lastThreshold = _threshold;
			}
			return _cachedThresholdVector;
		}

		// Helper method to create blur radius vector without allocation
		static Vector4 CreateBlurRadiusVector(float radius)
		{
			return BLUR_RADIUS_BASE_PATTERN * radius;
		}

		// Helper method to create dynamic blur radius vector
		static Vector4 CreateDynamicBlurRadiusVector(float radius)
		{
			return new Vector4(radius, radius, 0f, 0f);
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
				_sunTransform.position = camera.transform.position - _sunLight.transform.forward * SUN_DISTANCE;

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

			// Ensure pooled textures are available and correctly sized
			EnsurePooledTextures(rtWidth, rtHeight);

			// Precalculate blur radius constants to avoid repeated multiplication
			var blurStep = _blurRadius * BLUR_SCALE_FACTOR;

			// Step 1: Bright pass - extract bright areas using Pass 2 (with depth texture)
			var brightPass = _pooledBrightPass;

			// Set parameters for bright pass (only update if changed)
			var sunPosition = new Vector4(sunScreenPosition.x, sunScreenPosition.y, sunScreenPosition.z, SUN_MAX_RADIUS);
			var sunThreshold = GetCachedThresholdVector();

			SetBlurRadiusIfChanged(CreateBlurRadiusVector(_blurRadius));
			SetSunPositionIfChanged(sunPosition);
			SetSunThresholdIfChanged(sunThreshold);

			// Use Pass 2 for bright pass (with depth texture)
			Graphics.Blit(source, brightPass, _sunShaftsMaterial, 2);

			// Step 2: Radial blur iterations using Pass 1
			iterations = Mathf.Clamp(iterations, 1, 4);

			var baseBlurRadius = _blurRadius * BLUR_RESOLUTION_SCALE;
			SetBlurRadiusIfChanged(CreateDynamicBlurRadiusVector(baseBlurRadius));
			// Sun position already set above, no need to set again

			var currentSource = brightPass;
			var currentTarget = _pooledBlurTexture1;

			for (var i = 0; i < iterations; i++)
			{
				// First blur pass
				Graphics.Blit(currentSource, currentTarget, _sunShaftsMaterial, 1); // Pass 1: Radial blur

				// Precalculated blur radius for current iteration
				var dynamicBlurRadius = blurStep * (i * 2f + 1f);
				SetBlurRadiusIfChanged(CreateDynamicBlurRadiusVector(dynamicBlurRadius));

				// Second blur pass - swap source and target
				var tempSwap = currentSource;
				currentSource = currentTarget;
				currentTarget = (currentTarget == _pooledBlurTexture1) ? _pooledBlurTexture2 : _pooledBlurTexture1;

				Graphics.Blit(currentSource, currentTarget, _sunShaftsMaterial, 1); // Pass 1: Radial blur

				// Precalculated blur radius for next iteration
				var nextBlurRadius = blurStep * (i * 2f + 2f);
				SetBlurRadiusIfChanged(CreateDynamicBlurRadiusVector(nextBlurRadius));

				// Prepare for next iteration
				tempSwap = currentSource;
				currentSource = currentTarget;
				currentTarget = tempSwap;
			}

			// Step 3: Final composite using Pass 0 (Screen blend)
			var sunColor = (Vector4)_sunLight.color * intensity;
			SetSunColorIfChanged(sunColor);

			_sunShaftsMaterial.SetTexture("_ColorBuffer", currentSource);

			// Use Pass 0 for Screen blend mode (final composite)
			Graphics.Blit(source, destination, _sunShaftsMaterial, 0);
		}
	}
}
