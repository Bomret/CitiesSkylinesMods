using Common;
using NaturalLighting.Settings;
using UnityEngine;

namespace NaturalLighting.Features.ChromaticAberration
{
	/// <summary>
	/// Manages chromatic aberration post-processing effect for Natural Lighting mod.
	/// 
	/// This feature applies a realistic chromatic aberration effect that simulates the color fringing
	/// that occurs in real camera lenses. The effect splits the RGB color channels and shifts them
	/// radially from the center of the screen, creating a subtle optical distortion that enhances
	/// visual realism when used appropriately.
	/// 
	/// The feature follows the same pattern as SunShaftsFeature, using lazy initialization and
	/// a separate image effect component for the actual rendering.
	/// </summary>
	sealed class ChromaticAberrationFeature : Feature<ModSettings>
	{
		// Core components
		Camera _mainCamera;
		Material _chromaticAberrationMaterial;
		ChromaticAberrationEffect _imageEffectComponent;

		IShaderProvider _shaderProvider;

		// State tracking
		bool _currentChromaticAberrationEnabled;
		bool _isInitialized;

		/// <summary>
		/// Initializes a new instance of the ChromaticAberrationFeature class.
		/// </summary>
		/// <param name="logger">Logger for diagnostic output and error reporting.</param>
		public ChromaticAberrationFeature(ILogger logger) : base(logger) { }

		/// <summary>
		/// Called when the mod is loaded. Initializes the shader provider and enables
		/// chromatic aberration if the setting is enabled.
		/// </summary>
		/// <param name="settings">Current mod settings containing chromatic aberration preferences.</param>
		public override void OnLoaded(IServiceProvider serviceProvider, ModSettings settings)
		{
			Logger.LogFormat(LogType.Log, "[NaturalLighting] ChromaticAberration.OnLoaded called with UseChromaticAberration: {0}", settings.UseChromaticAberration);

			_shaderProvider = serviceProvider.GetObj<IShaderProvider>();

			_currentChromaticAberrationEnabled = settings.UseChromaticAberration;
			if (_currentChromaticAberrationEnabled)
			{
				EnableChromaticAberration();
			}
		}

		/// <summary>
		/// Called when mod settings are changed during runtime. Enables or disables
		/// chromatic aberration based on the new setting value.
		/// </summary>
		/// <param name="settings">Updated mod settings.</param>
		public override void OnSettingsChanged(ModSettings settings)
		{
			if (_currentChromaticAberrationEnabled == settings.UseChromaticAberration) return;

			Logger.LogFormat(LogType.Log, "[NaturalLighting] ChromaticAberration: Settings changed from {0} to {1}",
				_currentChromaticAberrationEnabled, settings.UseChromaticAberration);

			if (settings.UseChromaticAberration)
			{
				EnableChromaticAberration();
			}
			else
			{
				DisableChromaticAberration();
			}

			_currentChromaticAberrationEnabled = settings.UseChromaticAberration;
		}

		/// <summary>
		/// Called when the mod is being unloaded. Disables chromatic aberration to restore
		/// the original rendering pipeline.
		/// </summary>
		public override void OnUnloading()
		{
			DisableChromaticAberration();
			Logger.LogFormat(LogType.Log, "[NaturalLighting] ChromaticAberration feature unloaded");
		}

		/// <summary>
		/// Called when the feature is being disposed. Cleans up shader resources and
		/// removes the post-processing effect from the camera.
		/// </summary>
		protected override void OnDispose()
		{
			DisableChromaticAberration();
			CleanupResources();

			_mainCamera = null;
		}

		/// <summary>
		/// Lazily initializes Unity components required for chromatic aberration rendering.
		/// Finds the main camera in the scene and loads the shader.
		/// Sets _isInitialized to true only when all components are successfully found.
		/// This method is idempotent and can be called multiple times safely.
		/// </summary>
		void InitializeComponents()
		{
			if (_mainCamera == null)
			{
				_mainCamera = Camera.main ?? UnityEngine.Object.FindObjectOfType<Camera>();
			}

			if (_mainCamera != null)
			{
				_isInitialized = true;
				Logger.LogFormat(LogType.Log, "[NaturalLighting] ChromaticAberration: Initialized - Camera: Found");
			}
			else
			{
				Logger.LogFormat(LogType.Warning, "[NaturalLighting] ChromaticAberration: Initialization incomplete - Camera: not found");
			}
		}

		/// <summary>
		/// Loads the chromatic aberration shader from the mod's assets.
		/// Creates a Material instance from the loaded shader for use in image effects.
		/// Uses lazy loading - only loads shaders when first needed.
		/// </summary>
		void LoadChromaticAberrationShader()
		{
			if (_chromaticAberrationMaterial != null)
			{
				Logger.LogFormat(LogType.Log, "[NaturalLighting] ChromaticAberration: Shader already loaded, skipping");
				return;
			}

			try
			{
				Logger.LogFormat(LogType.Log, "[NaturalLighting] ChromaticAberration: Attempting to load shader");

				var chromaticAberrationShader = _shaderProvider.GetShader("ChromaticAberration", "naturallighting");

				if (chromaticAberrationShader != null)
				{
					_chromaticAberrationMaterial = new Material(chromaticAberrationShader)
					{
						hideFlags = HideFlags.HideAndDontSave
					};
					Logger.LogFormat(LogType.Log, "[NaturalLighting] ChromaticAberration: Successfully loaded shader");
				}
				else
				{
					Logger.LogFormat(LogType.Warning, "[NaturalLighting] ChromaticAberration: Shader not found");
				}
			}
			catch (System.Exception e)
			{
				Logger.LogFormat(LogType.Warning, "[NaturalLighting] ChromaticAberration: Shader loading failed: {0}", e.Message);
			}
		}

		/// <summary>
		/// Enables the chromatic aberration effect by initializing components and adding
		/// the image effect to the main camera.
		/// </summary>
		void EnableChromaticAberration()
		{
			InitializeComponents();
			LoadChromaticAberrationShader();

			if (_isInitialized)
			{
				ConfigureChromaticAberration();
			}
			else
			{
				Logger.LogFormat(LogType.Warning, "[NaturalLighting] ChromaticAberration: Cannot enable - initialization failed");
			}
		}

		/// <summary>
		/// Configures and applies chromatic aberration image effects to the main camera.
		/// Validates that required shader and camera are available before proceeding.
		/// </summary>
		void ConfigureChromaticAberration()
		{
			if (_chromaticAberrationMaterial != null && _mainCamera != null)
			{
				AddChromaticAberrationImageEffect();
				return;
			}

			Logger.LogFormat(LogType.Warning, "[NaturalLighting] ChromaticAberration: Cannot add image effect - missing shader or camera");
		}

		/// <summary>
		/// Adds the ChromaticAberrationImageEffect component to the main camera and configures it
		/// with the loaded shader material.
		/// Handles cleanup of existing components before creating new ones.
		/// </summary>
		void AddChromaticAberrationImageEffect()
		{
			if (_chromaticAberrationMaterial == null || _mainCamera == null)
			{
				Logger.LogFormat(LogType.Warning, "[NaturalLighting] ChromaticAberration: Cannot add image effect - missing prerequisites");
				return;
			}

			if (_imageEffectComponent != null)
			{
				Logger.LogFormat(LogType.Log, "[NaturalLighting] ChromaticAberration: Image effect already active, skipping");
				return;
			}

			try
			{
				// Check for and clean up any existing component
				var existingComponent = _mainCamera.gameObject.GetComponent<ChromaticAberrationEffect>();
				if (existingComponent != null)
				{
					UnityEngine.Object.DestroyImmediate(existingComponent);
				}

				// Add and initialize the image effect component
				_imageEffectComponent = _mainCamera.gameObject.AddComponent<ChromaticAberrationEffect>();
				_imageEffectComponent.Initialize(_chromaticAberrationMaterial, Logger);

				Logger.LogFormat(LogType.Log, "[NaturalLighting] ChromaticAberration: Image effect successfully added to camera");
			}
			catch (System.Exception ex)
			{
				Logger.LogFormat(LogType.Error, "[NaturalLighting] ChromaticAberration: Failed to add image effect: {0}", ex.Message);
			}
		}

		/// <summary>
		/// Disables the chromatic aberration effect by removing the image effect component
		/// from the camera.
		/// </summary>
		void DisableChromaticAberration()
		{
			if (_imageEffectComponent != null)
			{
				try
				{
					UnityEngine.Object.DestroyImmediate(_imageEffectComponent);
					_imageEffectComponent = null;
					Logger.LogFormat(LogType.Log, "[NaturalLighting] ChromaticAberration: Image effect removed from camera");
				}
				catch (System.Exception ex)
				{
					Logger.LogFormat(LogType.Error, "[NaturalLighting] ChromaticAberration: Error removing image effect: {0}", ex.Message);
				}
			}
		}

		/// <summary>
		/// Cleans up shader and material resources.
		/// </summary>
		void CleanupResources()
		{
			if (_chromaticAberrationMaterial != null)
			{
				try
				{
					Object.DestroyImmediate(_chromaticAberrationMaterial);
					_chromaticAberrationMaterial = null;
				}
				catch (System.Exception ex)
				{
					Logger.LogFormat(LogType.Error, "[NaturalLighting] ChromaticAberration: Error cleaning up material: {0}", ex.Message);
				}
			}
		}
	}
}
