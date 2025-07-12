using System;
using UnityEngine;

namespace NaturalLighting.Features.SunShafts
{
	/// <summary>
	/// High-performance volumetric sun shafts image effect for Unity 5.6.7f1.
	/// 
	/// This component implements a multi-pass rendering pipeline to create realistic sun shafts:
	/// 1. Bright Pass: Extracts bright areas from the scene using depth texture information
	/// 2. Radial Blur: Applies iterative radial blur from the sun's screen position
	/// 3. Screen Blend: Composites the blurred result back onto the original image
	/// 
	/// The shader uses a hard-coded 6-sample radial blur pattern optimized for Unity 5.6.7f1's
	/// GPU capabilities, with mathematical constants defined in <see cref="SunShaftsEffectConstants"/>.
	/// </summary>
	sealed class SunShaftsImageEffect : MonoBehaviour
	{
		// Core rendering components
		Light _sunLight;                                      // The directional light representing the sun
		Transform _sunTransform;                              // Transform used for sun position calculations
		Material _sunShaftsMaterial;                          // Material containing the sun shafts shader
		ILogger _logger;                                      // Logger for error reporting

		// Cached components for performance optimization
		Camera _camera;                                       // Cached to avoid GetComponent calls
		Transform _cameraTransform;                           // Cached for frequent position access
		Transform _sunLightTransform;                         // Cached for frequent direction access

		// Effect parameters
		float _intensity;                                     // Sun shafts intensity multiplier
		float _threshold;                                     // Brightness threshold for bright pass
		float _blurRadius;                                    // Base blur radius for radial blur
		int _blurIterations;                                  // Number of blur iterations to perform

		// Quality and optimization systems
		DistanceBasedQualityScaling _distanceOptimization;   // Adaptive quality system based on sun distance

		// RenderTexture pooling to eliminate per-frame allocations
		RenderTexture _pooledBrightPass;
		RenderTexture _pooledBlurTexture1;
		RenderTexture _pooledBlurTexture2;
		int _lastPooledWidth = SunShaftsEffectConstants.INVALID_TEXTURE_SIZE;
		int _lastPooledHeight = SunShaftsEffectConstants.INVALID_TEXTURE_SIZE;

		// Cached shader parameters to avoid redundant GPU calls
		Vector4 _cachedSunPosition;
		Vector4 _cachedBlurRadius;
		Vector4 _cachedSunThreshold;
		Vector4 _cachedSunColor;
		Vector4 _cachedThresholdVector;

		// Pre-calculated Vector4 patterns to avoid allocations
		static readonly Vector4 BLUR_RADIUS_BASE_PATTERN = new Vector4(1f, 1f, 0f, 0f);
		float _lastThreshold = float.MinValue;

		/// <summary>
		/// Initializes the sun shafts image effect with the specified parameters.
		/// This method caches components and sets up the distance-based quality optimization system.
		/// </summary>
		/// <param name="sunLight">The directional light representing the sun</param>
		/// <param name="sunTransform">Transform used for sun position calculations</param>
		/// <param name="sunShaftsMaterial">Material containing the optimized sun shafts shader</param>
		/// <param name="intensity">Base intensity multiplier for the sun shafts effect</param>
		/// <param name="threshold">Brightness threshold for the bright pass filter</param>
		/// <param name="blurRadius">Base blur radius for radial blur operations</param>
		/// <param name="blurIterations">Maximum number of blur iterations to perform</param>
		/// <param name="logger">Logger instance for error reporting</param>
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

			// Cache components to eliminate repeated lookups
			_camera = GetComponent<Camera>();
			_cameraTransform = _camera.transform;
			_sunLightTransform = _sunLight.transform;
		}

		/// <summary>
		/// Ensures that pooled RenderTextures are available and correctly sized for the current resolution.
		/// Only recreates textures when dimensions change to avoid unnecessary GPU memory allocations.
		/// </summary>
		/// <param name="width">Required texture width</param>
		/// <param name="height">Required texture height</param>
		void EnsurePooledTextures(int width, int height)
		{
			// Recreate textures only when dimensions change
			if (_lastPooledWidth != width || _lastPooledHeight != height)
			{
				ReleasePooledTextures();

				_pooledBrightPass = new RenderTexture(width, height, SunShaftsEffectConstants.RENDERTEXTURE_DEPTH_BUFFER) { filterMode = FilterMode.Bilinear };
				_pooledBlurTexture1 = new RenderTexture(width, height, SunShaftsEffectConstants.RENDERTEXTURE_DEPTH_BUFFER) { filterMode = FilterMode.Bilinear };
				_pooledBlurTexture2 = new RenderTexture(width, height, SunShaftsEffectConstants.RENDERTEXTURE_DEPTH_BUFFER) { filterMode = FilterMode.Bilinear };

				_lastPooledWidth = width;
				_lastPooledHeight = height;
			}
		}

		/// <summary>
		/// Releases all pooled RenderTextures and resets their size tracking.
		/// Called when textures need to be recreated or during cleanup.
		/// </summary>
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

			_lastPooledWidth = SunShaftsEffectConstants.INVALID_TEXTURE_SIZE;
			_lastPooledHeight = SunShaftsEffectConstants.INVALID_TEXTURE_SIZE;
		}

		/// <summary>
		/// Unity lifecycle method called when the component is destroyed.
		/// Ensures proper cleanup of pooled RenderTextures to prevent memory leaks.
		/// </summary>
		void OnDestroy()
		{
			ReleasePooledTextures();
		}

		/// <summary>
		/// Returns a cached Vector4 containing the threshold value in all components.
		/// Only recreates the vector when the threshold value changes to avoid allocations.
		/// </summary>
		/// <returns>Vector4 with threshold value in all components (threshold, threshold, threshold, threshold)</returns>
		Vector4 GetCachedThresholdVector()
		{
			if (_lastThreshold != _threshold)
			{
				_cachedThresholdVector = new Vector4(_threshold, _threshold, _threshold, _threshold);
				_lastThreshold = _threshold;
			}

			return _cachedThresholdVector;
		}

		/// <summary>
		/// Creates a blur radius Vector4 by multiplying the base pattern with the given radius.
		/// This avoids allocation by reusing the static base pattern (1, 1, 0, 0).
		/// </summary>
		/// <param name="radius">Blur radius value to apply</param>
		/// <returns>Vector4 suitable for shader blur radius parameter</returns>
		static Vector4 CreateBlurRadiusVector(float radius)
		{
			return BLUR_RADIUS_BASE_PATTERN * radius;
		}

		/// <summary>
		/// Creates a dynamic blur radius Vector4 for iterative blur operations.
		/// Used when the radius needs to be calculated per iteration.
		/// </summary>
		/// <param name="radius">Dynamic blur radius value</param>
		/// <returns>Vector4 with radius in X and Y components, zeros in Z and W</returns>
		static Vector4 CreateDynamicBlurRadiusVector(float radius)
		{
			return new Vector4(radius, radius, 0f, 0f);
		}

		/// <summary>
		/// Sets the sun position shader parameter only if it has changed since the last call.
		/// This optimization avoids redundant GPU driver calls that can impact performance.
		/// </summary>
		/// <param name="newSunPosition">New sun position vector to set</param>
		void SetSunPositionIfChanged(Vector4 newSunPosition)
		{
			if (_cachedSunPosition != newSunPosition)
			{
				_cachedSunPosition = newSunPosition;
				_sunShaftsMaterial.SetVector("_SunPosition", newSunPosition);
			}
		}

		/// <summary>
		/// Sets the blur radius shader parameter only if it has changed since the last call.
		/// Reduces GPU driver overhead by avoiding redundant parameter updates.
		/// </summary>
		/// <param name="newBlurRadius">New blur radius vector to set</param>
		void SetBlurRadiusIfChanged(Vector4 newBlurRadius)
		{
			if (_cachedBlurRadius != newBlurRadius)
			{
				_cachedBlurRadius = newBlurRadius;
				_sunShaftsMaterial.SetVector("_BlurRadius4", newBlurRadius);
			}
		}

		/// <summary>
		/// Sets the sun threshold shader parameter only if it has changed since the last call.
		/// Optimizes performance by caching the threshold vector between frames.
		/// </summary>
		/// <param name="newSunThreshold">New sun threshold vector to set</param>
		void SetSunThresholdIfChanged(Vector4 newSunThreshold)
		{
			if (_cachedSunThreshold != newSunThreshold)
			{
				_cachedSunThreshold = newSunThreshold;
				_sunShaftsMaterial.SetVector("_SunThreshold", newSunThreshold);
			}
		}

		/// <summary>
		/// Sets the sun color shader parameter only if it has changed since the last call.
		/// Prevents unnecessary GPU state changes when the sun color remains constant.
		/// </summary>
		/// <param name="newSunColor">New sun color vector to set</param>
		void SetSunColorIfChanged(Vector4 newSunColor)
		{
			if (_cachedSunColor != newSunColor)
			{
				_cachedSunColor = newSunColor;
				_sunShaftsMaterial.SetVector("_SunColor", newSunColor);
			}
		}

		/// <summary>
		/// Unity's image effect entry point. Called automatically for each frame when attached to a Camera.
		/// Orchestrates the entire sun shafts rendering pipeline with error handling and early exits.
		/// </summary>
		/// <param name="source">Source render texture from the camera</param>
		/// <param name="destination">Destination render texture to write the final result</param>
		void OnRenderImage(RenderTexture source, RenderTexture destination)
		{
			try
			{
				if (_camera == null)
				{
					Graphics.Blit(source, destination);
					return;
				}

				// Position sun transform relative to camera for depth calculations
				_sunTransform.position = _cameraTransform.position - _sunLightTransform.forward * SunShaftsEffectConstants.SUN_DISTANCE;

				// Convert sun world position to screen space
				var sunScreenPosition = _camera.WorldToViewportPoint(_sunTransform.position);

				// Enable depth texture for bright pass filtering
				_camera.depthTextureMode |= DepthTextureMode.Depth;

				if (sunScreenPosition.z <= 0 ||
					!_distanceOptimization.TryCalculateQualitySettings(sunScreenPosition, _blurIterations, out var quality) ||
					!quality.HasValue)
				{
					// Sun not visible or quality settings disable effect
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

		/// <summary>
		/// Executes the complete sun shafts rendering pipeline with the specified quality settings.
		/// Coordinates the three main phases: bright pass, blur iterations, and final composite.
		/// </summary>
		/// <param name="source">Source render texture from the camera</param>
		/// <param name="destination">Destination render texture for the final composite</param>
		/// <param name="sunScreenPosition">Sun position in screen space coordinates</param>
		/// <param name="quality">Quality settings determining resolution and iteration count</param>
		void ApplySunShaftsEffect(RenderTexture source, RenderTexture destination, Vector3 sunScreenPosition, QualitySettings quality)
		{
			var rtWidth = source.width / quality.ResolutionDivisor;
			var rtHeight = source.height / quality.ResolutionDivisor;
			var iterations = quality.BlurIterations;
			var intensity = _intensity * quality.IntensityMultiplier;

			EnsurePooledTextures(rtWidth, rtHeight);

			var blurStep = _blurRadius * SunShaftsEffectConstants.BLUR_SCALE_FACTOR;

			// Phase 1: Extract bright areas with depth filtering
			ExecuteBrightPass(source, sunScreenPosition);

			// Phase 2: Apply progressive radial blur
			iterations = Mathf.Clamp(iterations, SunShaftsEffectConstants.MIN_BLUR_ITERATIONS, SunShaftsEffectConstants.MAX_BLUR_ITERATIONS);
			var blurredTexture = ExecuteBlurIterations(iterations, blurStep);

			// Phase 3: Composite with original image
			ExecuteFinalComposite(source, destination, blurredTexture, intensity);
		}

		/// <summary>
		/// Executes the bright pass phase, extracting bright areas from the source image.
		/// Uses depth texture information to enhance the effect and sets up shader parameters.
		/// </summary>
		/// <param name="source">Source render texture to extract bright areas from</param>
		/// <param name="sunScreenPosition">Sun position in screen space for parameter calculation</param>
		void ExecuteBrightPass(RenderTexture source, Vector3 sunScreenPosition)
		{
			// Configure shader parameters (cached to avoid redundant GPU calls)
			var sunPosition = new Vector4(sunScreenPosition.x, sunScreenPosition.y, sunScreenPosition.z, SunShaftsEffectConstants.SUN_MAX_RADIUS);
			var sunThreshold = GetCachedThresholdVector();

			SetBlurRadiusIfChanged(CreateBlurRadiusVector(_blurRadius));
			SetSunPositionIfChanged(sunPosition);
			SetSunThresholdIfChanged(sunThreshold);

			Graphics.Blit(source, _pooledBrightPass, _sunShaftsMaterial, SunShaftsEffectConstants.SHADER_PASS_BRIGHT_PASS);
		}

		/// <summary>
		/// Executes the iterative radial blur phase using a ping-pong technique between pooled textures.
		/// Applies progressively stronger blur with each iteration to create the volumetric sun shafts effect.
		/// </summary>
		/// <param name="iterations">Number of blur iterations to perform (clamped between MIN_BLUR_ITERATIONS and MAX_BLUR_ITERATIONS)</param>
		/// <param name="blurStep">Base blur step size for each iteration</param>
		/// <returns>Final blurred texture ready for compositing</returns>
		RenderTexture ExecuteBlurIterations(int iterations, float blurStep)
		{
			var baseBlurRadius = _blurRadius * SunShaftsEffectConstants.BLUR_RESOLUTION_SCALE;
			SetBlurRadiusIfChanged(CreateDynamicBlurRadiusVector(baseBlurRadius));

			var currentSource = _pooledBrightPass;
			var currentTarget = _pooledBlurTexture1;

			for (var i = 0; i < iterations; i++)
			{
				// First blur pass
				Graphics.Blit(currentSource, currentTarget, _sunShaftsMaterial, SunShaftsEffectConstants.SHADER_PASS_RADIAL_BLUR);

				// Calculate progressive blur radius
				var dynamicBlurRadius = blurStep * (i * 2f + 1f);
				SetBlurRadiusIfChanged(CreateDynamicBlurRadiusVector(dynamicBlurRadius));

				// Ping-pong between textures for second pass
				var tempSwap = currentSource;
				currentSource = currentTarget;
				currentTarget = (currentTarget == _pooledBlurTexture1) ? _pooledBlurTexture2 : _pooledBlurTexture1;

				Graphics.Blit(currentSource, currentTarget, _sunShaftsMaterial, SunShaftsEffectConstants.SHADER_PASS_RADIAL_BLUR);

				// Prepare blur radius for next iteration
				var nextBlurRadius = blurStep * (i * 2f + 2f);
				SetBlurRadiusIfChanged(CreateDynamicBlurRadiusVector(nextBlurRadius));

				tempSwap = currentSource;
				currentSource = currentTarget;
				currentTarget = tempSwap;
			}

			return currentSource;
		}

		/// <summary>
		/// Executes the final composite phase, blending the blurred sun shafts with the original image.
		/// Uses screen blend mode to create the final volumetric lighting effect.
		/// </summary>
		/// <param name="source">Original source image from the camera</param>
		/// <param name="destination">Final destination texture to write the composite result</param>
		/// <param name="blurredTexture">Blurred sun shafts texture from the blur iterations phase</param>
		/// <param name="intensity">Final intensity multiplier for the effect</param>
		void ExecuteFinalComposite(RenderTexture source, RenderTexture destination, RenderTexture blurredTexture, float intensity)
		{
			var sunColor = (Vector4)_sunLight.color * intensity;
			SetSunColorIfChanged(sunColor);

			_sunShaftsMaterial.SetTexture("_ColorBuffer", blurredTexture);
			Graphics.Blit(source, destination, _sunShaftsMaterial, SunShaftsEffectConstants.SHADER_PASS_SCREEN_BLEND);
		}
	}
}
