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

		// Shader pass indices and iteration constants
		const int SHADER_PASS_SCREEN_BLEND = 0;              // Pass 0: Screen blend for final composite
		const int SHADER_PASS_RADIAL_BLUR = 1;               // Pass 1: Radial blur iterations
		const int SHADER_PASS_BRIGHT_PASS = 2;               // Pass 2: Bright pass with depth texture
		const int MIN_BLUR_ITERATIONS = 1;                   // Minimum blur iterations allowed
		const int MAX_BLUR_ITERATIONS = 4;                   // Maximum blur iterations allowed

		// RenderTexture and pooling constants
		const int RENDERTEXTURE_DEPTH_BUFFER = 0;            // No depth buffer for RenderTextures
		const int INVALID_TEXTURE_SIZE = -1;                 // Invalid texture size marker

		Light _sunLight;
		Transform _sunTransform;
		Material _sunShaftsMaterial;
		ILogger _logger;
		Camera _camera; // Cached camera component to avoid GetComponent calls
		Transform _cameraTransform; // Cached camera transform for frequent access
		Transform _sunLightTransform; // Cached sun light transform for frequent access

		float _intensity;
		float _threshold;
		float _blurRadius;
		int _blurIterations;

		DistanceBasedQualityScaling _distanceOptimization;

		// RenderTexture pooling to avoid allocation overhead
		RenderTexture _pooledBrightPass;
		RenderTexture _pooledBlurTexture1;
		RenderTexture _pooledBlurTexture2;
		int _lastPooledWidth = INVALID_TEXTURE_SIZE;
		int _lastPooledHeight = INVALID_TEXTURE_SIZE;

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

			// Cache the camera component to avoid repeated GetComponent calls
			_camera = GetComponent<Camera>();
			_cameraTransform = _camera.transform;
			_sunLightTransform = _sunLight.transform;
		}

		// RenderTexture pooling methods to reduce allocation overhead
		void EnsurePooledTextures(int width, int height)
		{
			// Only recreate textures if dimensions changed
			if (_lastPooledWidth != width || _lastPooledHeight != height)
			{
				ReleasePooledTextures();

				_pooledBrightPass = new RenderTexture(width, height, RENDERTEXTURE_DEPTH_BUFFER) { filterMode = FilterMode.Bilinear };
				_pooledBlurTexture1 = new RenderTexture(width, height, RENDERTEXTURE_DEPTH_BUFFER) { filterMode = FilterMode.Bilinear };
				_pooledBlurTexture2 = new RenderTexture(width, height, RENDERTEXTURE_DEPTH_BUFFER) { filterMode = FilterMode.Bilinear };

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

			_lastPooledWidth = INVALID_TEXTURE_SIZE;
			_lastPooledHeight = INVALID_TEXTURE_SIZE;
		}

		void OnDestroy()
		{
			ReleasePooledTextures();
		}

		Vector4 GetCachedThresholdVector()
		{
			if (_lastThreshold != _threshold)
			{
				_cachedThresholdVector = new Vector4(_threshold, _threshold, _threshold, _threshold);
				_lastThreshold = _threshold;
			}
			return _cachedThresholdVector;
		}

		static Vector4 CreateBlurRadiusVector(float radius)
		{
			return BLUR_RADIUS_BASE_PATTERN * radius;
		}

		static Vector4 CreateDynamicBlurRadiusVector(float radius)
		{
			return new Vector4(radius, radius, 0f, 0f);
		}

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
				if (_camera == null)
				{
					Graphics.Blit(source, destination);
					return;
				}

				// Update sunTransform position every frame relative to camera position
				_sunTransform.position = _cameraTransform.position - _sunLightTransform.forward * SUN_DISTANCE;

				// Calculate sun screen position using the updated transform
				var sunScreenPosition = _camera.WorldToViewportPoint(_sunTransform.position);

				// Enable depth texture mode
				_camera.depthTextureMode |= DepthTextureMode.Depth;

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

			EnsurePooledTextures(rtWidth, rtHeight);

			var blurStep = _blurRadius * BLUR_SCALE_FACTOR;

			// Step 1: Bright pass - extract bright areas using Pass 2 (with depth texture)
			ExecuteBrightPass(source, sunScreenPosition);

			// Step 2: Radial blur iterations using Pass 1
			iterations = Mathf.Clamp(iterations, MIN_BLUR_ITERATIONS, MAX_BLUR_ITERATIONS);
			var blurredTexture = ExecuteBlurIterations(iterations, blurStep);

			// Step 3: Final composite using Pass 0 (Screen blend)
			ExecuteFinalComposite(source, destination, blurredTexture, intensity);
		}

		void ExecuteBrightPass(RenderTexture source, Vector3 sunScreenPosition)
		{
			// Set parameters for bright pass (only update if changed)
			var sunPosition = new Vector4(sunScreenPosition.x, sunScreenPosition.y, sunScreenPosition.z, SUN_MAX_RADIUS);
			var sunThreshold = GetCachedThresholdVector();

			SetBlurRadiusIfChanged(CreateBlurRadiusVector(_blurRadius));
			SetSunPositionIfChanged(sunPosition);
			SetSunThresholdIfChanged(sunThreshold);

			// Use Pass 2 for bright pass (with depth texture)
			Graphics.Blit(source, _pooledBrightPass, _sunShaftsMaterial, SHADER_PASS_BRIGHT_PASS);
		}

		RenderTexture ExecuteBlurIterations(int iterations, float blurStep)
		{
			var baseBlurRadius = _blurRadius * BLUR_RESOLUTION_SCALE;
			SetBlurRadiusIfChanged(CreateDynamicBlurRadiusVector(baseBlurRadius));

			var currentSource = _pooledBrightPass;
			var currentTarget = _pooledBlurTexture1;

			for (var i = 0; i < iterations; i++)
			{
				// First blur pass
				Graphics.Blit(currentSource, currentTarget, _sunShaftsMaterial, SHADER_PASS_RADIAL_BLUR); // Pass 1: Radial blur

				// Precalculated blur radius for current iteration
				var dynamicBlurRadius = blurStep * (i * 2f + 1f);
				SetBlurRadiusIfChanged(CreateDynamicBlurRadiusVector(dynamicBlurRadius));

				// Second blur pass - swap source and target
				var tempSwap = currentSource;
				currentSource = currentTarget;
				currentTarget = (currentTarget == _pooledBlurTexture1) ? _pooledBlurTexture2 : _pooledBlurTexture1;

				Graphics.Blit(currentSource, currentTarget, _sunShaftsMaterial, SHADER_PASS_RADIAL_BLUR); // Pass 1: Radial blur

				// Precalculated blur radius for next iteration
				var nextBlurRadius = blurStep * (i * 2f + 2f);
				SetBlurRadiusIfChanged(CreateDynamicBlurRadiusVector(nextBlurRadius));

				// Prepare for next iteration
				tempSwap = currentSource;
				currentSource = currentTarget;
				currentTarget = tempSwap;
			}

			return currentSource;
		}

		void ExecuteFinalComposite(RenderTexture source, RenderTexture destination, RenderTexture blurredTexture, float intensity)
		{
			var sunColor = (Vector4)_sunLight.color * intensity;
			SetSunColorIfChanged(sunColor);

			_sunShaftsMaterial.SetTexture("_ColorBuffer", blurredTexture);

			// Use Pass 0 for Screen blend mode (final composite)
			Graphics.Blit(source, destination, _sunShaftsMaterial, SHADER_PASS_SCREEN_BLEND);
		}
	}
}
