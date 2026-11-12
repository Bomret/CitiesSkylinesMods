using System;
using Common;
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
	sealed class SunShaftsEffect : MonoBehaviour
	{
		// Core rendering components
		Light _sunLight;                                      // The directional light representing the sun
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

		// Shader property ids, named after the uniforms in SunShafts.shader
		static readonly int SunPositionPropertyId = Shader.PropertyToID("_SunPosition");
		static readonly int BlurRadiusPropertyId = Shader.PropertyToID("_BlurRadius4");
		static readonly int SunThresholdPropertyId = Shader.PropertyToID("_SunThreshold");
		static readonly int SunColorPropertyId = Shader.PropertyToID("_SunColor");
		static readonly int ColorBufferPropertyId = Shader.PropertyToID("_ColorBuffer");

		// Pre-calculated Vector4 patterns to avoid allocations
		static readonly Vector4 BLUR_RADIUS_BASE_PATTERN = new Vector4(1f, 1f, 0f, 0f);

		/// <summary>
		/// Initializes the sun shafts image effect with the specified parameters.
		/// This method caches the camera and sun light components.
		/// Effect parameters are automatically loaded from SunShaftsEffectConstants.
		/// </summary>
		/// <param name="sunLight">The directional light representing the sun</param>
		/// <param name="sunShaftsMaterial">Material containing the optimized sun shafts shader</param>
		/// <param name="logger">Logger instance for error reporting</param>
		public void Initialize(Light sunLight, Material sunShaftsMaterial, ILogger logger)
		{
			_sunLight = sunLight;
			_sunShaftsMaterial = sunShaftsMaterial;
			_logger = logger;

			// Use default constants from SunShaftsEffectConstants
			_intensity = SunShaftsEffectConstants.DEFAULT_INTENSITY;
			_threshold = SunShaftsEffectConstants.DEFAULT_THRESHOLD;
			_blurRadius = SunShaftsEffectConstants.DEFAULT_BLUR_RADIUS;
			_blurIterations = SunShaftsEffectConstants.DEFAULT_BLUR_ITERATIONS;

			// Cache components to eliminate repeated lookups
			_camera = GetComponent<Camera>();
			_cameraTransform = _camera.transform;
			_sunLightTransform = _sunLight.transform;
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
		/// Unity's image effect entry point. Called automatically for each frame when attached to a Camera.
		/// Orchestrates the entire sun shafts rendering pipeline with error handling and early exits.
		/// </summary>
		/// <param name="source">Source render texture from the camera</param>
		/// <param name="destination">Destination render texture to write the final result</param>
		void OnRenderImage(RenderTexture source, RenderTexture destination)
		{
			try
			{
				if (_camera == null || _sunLight == null)
				{
					Graphics.Blit(source, destination);
					return;
				}

				// Place the sun at a fixed distance from the camera and convert it to screen space
				var sunScreenPosition = _camera.WorldToViewportPoint(
					_cameraTransform.position - _sunLightTransform.forward * SunShaftsEffectConstants.SUN_DISTANCE);

				// Enable depth texture for bright pass filtering
				_camera.depthTextureMode |= DepthTextureMode.Depth;

				var fade = CalculateScreenEdgeFade(sunScreenPosition);
				var rtWidth = source.width / SunShaftsEffectConstants.RESOLUTION_DIVISOR;
				var rtHeight = source.height / SunShaftsEffectConstants.RESOLUTION_DIVISOR;

				if (fade <= 0f || rtWidth <= 0 || rtHeight <= 0)
				{
					// Sun behind the camera, outside the fade window, or screen too small for the buffers
					Graphics.Blit(source, destination);
					return;
				}

				ApplySunShaftsEffect(source, destination, sunScreenPosition, rtWidth, rtHeight, _intensity * fade);
			}
			catch (Exception err)
			{
				_logger?.LogFormat(LogType.Error, "[NaturalLighting] SunShaftsImageEffect error: {0}", err.Message);
				Graphics.Blit(source, destination);
			}
		}

		/// <summary>
		/// Returns the factor that fades the effect out as the sun leaves the screen.
		/// It is 1 while the sun is on screen, falls with a smoothstep to 0 across
		/// <see cref="SunShaftsEffectConstants.SUN_FADE_MARGIN"/> past each edge, and takes the smaller
		/// per-axis factor at a corner. It is 0 for a sun behind the camera and for a non-finite position:
		/// every comparison is written so that NaN fails it.
		/// </summary>
		/// <param name="sunScreenPosition">Sun position in viewport coordinates</param>
		/// <returns>Fade factor between 0 and 1</returns>
		static float CalculateScreenEdgeFade(Vector3 sunScreenPosition)
		{
			if (!(sunScreenPosition.z > 0f))
			{
				return 0f;
			}

			return Mathf.Min(CalculateAxisFade(sunScreenPosition.x), CalculateAxisFade(sunScreenPosition.y));
		}

		/// <summary>
		/// Returns the fade factor along one viewport axis.
		/// </summary>
		/// <param name="position">Viewport coordinate on one axis</param>
		/// <returns>1 inside [0, 1], 0 outside the fade window, and a smoothstep in between</returns>
		static float CalculateAxisFade(float position)
		{
			const float margin = SunShaftsEffectConstants.SUN_FADE_MARGIN;

			if (!(position >= -margin && position <= 1f + margin))
			{
				return 0f;
			}

			if (position >= 0f && position <= 1f)
			{
				return 1f;
			}

			var distancePastEdge = position < 0f ? -position : position - 1f;

			return Mathf.SmoothStep(0f, 1f, 1f - distancePastEdge / margin);
		}

		/// <summary>
		/// Acquires a temporary render texture and sets its sampler state, since the pool may return a texture
		/// another user left with a different filter or wrap mode. Clamp makes blur samples past the edge
		/// continue the edge texels toward an off-screen sun instead of wrapping in the opposite edge.
		/// </summary>
		/// <param name="width">Texture width</param>
		/// <param name="height">Texture height</param>
		/// <returns>A temporary render texture the caller releases with <see cref="RenderTexture.ReleaseTemporary"/></returns>
		static RenderTexture AcquireTexture(int width, int height)
		{
			var texture = RenderTexture.GetTemporary(width, height, SunShaftsEffectConstants.RENDERTEXTURE_DEPTH_BUFFER);
			texture.filterMode = FilterMode.Bilinear;
			texture.wrapMode = TextureWrapMode.Clamp;

			return texture;
		}

		/// <summary>
		/// Executes the complete sun shafts rendering pipeline at the fixed quarter resolution.
		/// Coordinates the three main phases: bright pass, blur iterations, and final composite.
		/// Every render texture it acquires is released once the composite is done, also when a pass throws.
		/// </summary>
		/// <param name="source">Source render texture from the camera</param>
		/// <param name="destination">Destination render texture for the final composite</param>
		/// <param name="sunScreenPosition">Sun position in screen space coordinates</param>
		/// <param name="rtWidth">Width of the intermediate render textures</param>
		/// <param name="rtHeight">Height of the intermediate render textures</param>
		/// <param name="intensity">Effect intensity, already multiplied by the screen edge fade</param>
		void ApplySunShaftsEffect(RenderTexture source, RenderTexture destination, Vector3 sunScreenPosition, int rtWidth, int rtHeight, float intensity)
		{
			RenderTexture brightPass = null;
			RenderTexture blurTexture1 = null;
			RenderTexture blurTexture2 = null;

			try
			{
				brightPass = AcquireTexture(rtWidth, rtHeight);
				blurTexture1 = AcquireTexture(rtWidth, rtHeight);
				blurTexture2 = AcquireTexture(rtWidth, rtHeight);

				var blurStep = _blurRadius * SunShaftsEffectConstants.BLUR_SCALE_FACTOR;

				// Phase 1: Extract bright areas with depth filtering
				ExecuteBrightPass(source, brightPass, sunScreenPosition);

				// Phase 2: Apply progressive radial blur
				var blurredTexture = ExecuteBlurIterations(brightPass, blurTexture1, blurTexture2, _blurIterations, blurStep);

				// Phase 3: Composite with original image
				ExecuteFinalComposite(source, destination, blurredTexture, intensity);
			}
			finally
			{
				if (brightPass != null)
				{
					RenderTexture.ReleaseTemporary(brightPass);
				}

				if (blurTexture1 != null)
				{
					RenderTexture.ReleaseTemporary(blurTexture1);
				}

				if (blurTexture2 != null)
				{
					RenderTexture.ReleaseTemporary(blurTexture2);
				}
			}
		}

		/// <summary>
		/// Executes the bright pass phase, extracting bright areas from the source image.
		/// Uses depth texture information to enhance the effect and sets up shader parameters.
		/// </summary>
		/// <param name="source">Source render texture to extract bright areas from</param>
		/// <param name="brightPass">Render texture that receives the bright areas</param>
		/// <param name="sunScreenPosition">Sun position in screen space for parameter calculation</param>
		void ExecuteBrightPass(RenderTexture source, RenderTexture brightPass, Vector3 sunScreenPosition)
		{
			var sunPosition = new Vector4(sunScreenPosition.x, sunScreenPosition.y, sunScreenPosition.z, SunShaftsEffectConstants.SUN_MAX_RADIUS);
			var sunThreshold = new Vector4(_threshold, _threshold, _threshold, _threshold);

			_sunShaftsMaterial.SetVector(BlurRadiusPropertyId, CreateBlurRadiusVector(_blurRadius));
			_sunShaftsMaterial.SetVector(SunPositionPropertyId, sunPosition);
			_sunShaftsMaterial.SetVector(SunThresholdPropertyId, sunThreshold);

			Graphics.Blit(source, brightPass, _sunShaftsMaterial, SunShaftsEffectConstants.SHADER_PASS_BRIGHT_PASS);
		}

		/// <summary>
		/// Executes the iterative radial blur phase using a ping-pong technique between two blur textures.
		/// Applies progressively stronger blur with each iteration to create the volumetric sun shafts effect.
		/// </summary>
		/// <param name="brightPass">Bright pass result the first blur reads</param>
		/// <param name="blurTexture1">First ping-pong texture</param>
		/// <param name="blurTexture2">Second ping-pong texture</param>
		/// <param name="iterations">Number of blur iterations to perform</param>
		/// <param name="blurStep">Base blur step size for each iteration</param>
		/// <returns>Final blurred texture ready for compositing</returns>
		RenderTexture ExecuteBlurIterations(RenderTexture brightPass, RenderTexture blurTexture1, RenderTexture blurTexture2, int iterations, float blurStep)
		{
			var baseBlurRadius = _blurRadius * SunShaftsEffectConstants.BLUR_RESOLUTION_SCALE;
			_sunShaftsMaterial.SetVector(BlurRadiusPropertyId, CreateDynamicBlurRadiusVector(baseBlurRadius));

			var currentSource = brightPass;
			var currentTarget = blurTexture1;

			for (var i = 0; i < iterations; i++)
			{
				// First blur pass
				Graphics.Blit(currentSource, currentTarget, _sunShaftsMaterial, SunShaftsEffectConstants.SHADER_PASS_RADIAL_BLUR);

				// Calculate progressive blur radius
				var dynamicBlurRadius = blurStep * (i * 2f + 1f);
				_sunShaftsMaterial.SetVector(BlurRadiusPropertyId, CreateDynamicBlurRadiusVector(dynamicBlurRadius));

				// Ping-pong between textures for second pass
				currentSource = currentTarget;
				currentTarget = (currentTarget == blurTexture1) ? blurTexture2 : blurTexture1;

				Graphics.Blit(currentSource, currentTarget, _sunShaftsMaterial, SunShaftsEffectConstants.SHADER_PASS_RADIAL_BLUR);

				// Prepare blur radius for next iteration
				var nextBlurRadius = blurStep * (i * 2f + 2f);
				_sunShaftsMaterial.SetVector(BlurRadiusPropertyId, CreateDynamicBlurRadiusVector(nextBlurRadius));

				var tempSwap = currentSource;
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
			_sunShaftsMaterial.SetVector(SunColorPropertyId, sunColor);
			_sunShaftsMaterial.SetTexture(ColorBufferPropertyId, blurredTexture);
			Graphics.Blit(source, destination, _sunShaftsMaterial, SunShaftsEffectConstants.SHADER_PASS_SCREEN_BLEND);
		}
	}
}
