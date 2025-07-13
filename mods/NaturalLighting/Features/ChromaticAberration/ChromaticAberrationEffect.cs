using Common;
using UnityEngine;

namespace NaturalLighting.Features.ChromaticAberration
{
	/// <summary>
	/// High-performance chromatic aberration image effect for Unity 5.6.7f1.
	/// 
	/// This component implements a single-pass post-processing effect that simulates the color fringing
	/// that occurs in real camera lenses. The effect splits the RGB color channels and shifts them
	/// radially from the center of the screen, creating a subtle optical distortion.
	/// 
	/// The shader uses optimized sampling patterns and is designed for Unity 5.6.7f1's
	/// GPU capabilities with predefined parameters optimized for Cities: Skylines.
	/// </summary>
	sealed class ChromaticAberrationEffect : MonoBehaviour
	{
		// Core rendering components
		Material _chromaticAberrationMaterial;                // Material containing the chromatic aberration shader
		ILogger _logger;                                      // Logger for error reporting

		// Cached components for performance optimization
		Camera _camera;                                       // Cached to avoid GetComponent calls

		// Shader property IDs (cached for performance)
		static readonly int AberrationPropertyId = Shader.PropertyToID("_Aberration");
		static readonly int DistortionPropertyId = Shader.PropertyToID("_Distortion");

		/// <summary>
		/// Initializes the chromatic aberration image effect with the specified material.
		/// This method caches the camera component and sets up the shader parameters.
		/// </summary>
		/// <param name="chromaticAberrationMaterial">Material containing the chromatic aberration shader</param>
		/// <param name="logger">Logger instance for error reporting</param>
		public void Initialize(Material chromaticAberrationMaterial, ILogger logger)
		{
			_chromaticAberrationMaterial = chromaticAberrationMaterial;
			_logger = logger;

			// Cache camera component to eliminate repeated lookups
			_camera = GetComponent<Camera>();

			// Configure shader parameters with optimized values
			SetupShaderParameters();

			_logger?.LogFormat(LogType.Log, "[NaturalLighting] ChromaticAberration image effect initialized");
		}

		/// <summary>
		/// Sets up the shader parameters with optimized values for Cities: Skylines.
		/// This method configures the chromatic aberration effect with predefined values
		/// that provide a subtle, realistic distortion effect.
		/// </summary>
		void SetupShaderParameters()
		{
			if (_chromaticAberrationMaterial != null)
			{
				_chromaticAberrationMaterial.SetFloat(AberrationPropertyId, ChromaticAberrationEffectConstants.DEFAULT_ABERRATION_STRENGTH);
				_chromaticAberrationMaterial.SetFloat(DistortionPropertyId, ChromaticAberrationEffectConstants.DEFAULT_DISTORTION_AMOUNT);
			}
		}

		/// <summary>
		/// Unity callback for post-processing effects. Called after the camera renders the scene.
		/// Applies the chromatic aberration effect to the rendered frame.
		/// </summary>
		/// <param name="source">The source render texture (current frame)</param>
		/// <param name="destination">The destination render texture (where to output the processed frame)</param>
		void OnRenderImage(RenderTexture source, RenderTexture destination)
		{
			if (_chromaticAberrationMaterial != null)
			{
				try
				{
					// Apply chromatic aberration effect using the shader
					Graphics.Blit(source, destination, _chromaticAberrationMaterial);
				}
				catch (System.Exception ex)
				{
					// Fallback: pass through without effect if shader fails
					_logger?.LogFormat(LogType.Warning, "[NaturalLighting] ChromaticAberration shader error: {0}", ex.Message);
					Graphics.Blit(source, destination);
				}
			}
			else
			{
				// Fallback: pass through without effect if material is null
				Graphics.Blit(source, destination);
			}
		}

		/// <summary>
		/// Unity callback when the component is being destroyed.
		/// Performs cleanup to prevent memory leaks.
		/// </summary>
		void OnDestroy()
		{
			_chromaticAberrationMaterial = null;
			_camera = null;
			_logger = null;
		}
	}
}
