using NaturalLighting.Settings;
using UnityEngine;

namespace NaturalLighting.Features.SunShafts
{
	/// <summary>
	/// Manages volumetric sunshaft rendering effects for the Natural Lighting mod.
	/// Provides lazy initialization of Unity components and shaders, enabling/disabling
	/// sunshaft image effects based on user settings.
	/// </summary>
	sealed class SunshaftsFeature : Feature<ModSettings>
	{
		readonly ILogger _logger;
		readonly IModProvider _modProvider;

		Camera _mainCamera;
		Light _sunLight;

		bool _currentSunshaftsEnabled;
		bool _isInitialized;

		Material _sunShaftShaderMaterial;
		SunShaftsImageEffect _sunShaftsComponent;
		GameObject _sunTransform;
		ShaderProvider _shaderProvider;

		/// <summary>
		/// Initializes a new instance of the SunshaftsFeature class.
		/// </summary>
		/// <param name="modProvider">Provider for accessing mod resources and metadata.</param>
		/// <param name="logger">Logger for diagnostic output.</param>
		public SunshaftsFeature(IModProvider modProvider, ILogger logger)
		{
			_modProvider = modProvider;
			_logger = logger;
		}

		/// <summary>
		/// Called when the mod is loaded. Initializes the shader provider and enables
		/// sunshafts if the setting is enabled.
		/// </summary>
		/// <param name="settings">Current mod settings containing sunshaft preferences.</param>
		public override void OnLoaded(ModSettings settings)
		{
			_logger.LogFormat(LogType.Log, "[NaturalLighting] Sunshafts.OnLoaded called with EnableSunshafts: {0}", settings.EnableSunshafts);

			var mod = _modProvider.GetCurrentMod();
			_shaderProvider = new ShaderProvider(mod);

			_currentSunshaftsEnabled = settings.EnableSunshafts;
			if (_currentSunshaftsEnabled)
			{
				EnableSunshafts();
			}
		}

		/// <summary>
		/// Called when mod settings are changed during runtime. Enables or disables
		/// sunshafts based on the new setting value.
		/// </summary>
		/// <param name="settings">Updated mod settings.</param>
		public override void OnSettingsChanged(ModSettings settings)
		{
			if (_currentSunshaftsEnabled == settings.EnableSunshafts) return;

			_logger.LogFormat(LogType.Log, "[NaturalLighting] Sunshafts: Settings changed from {0} to {1}",
				_currentSunshaftsEnabled, settings.EnableSunshafts);

			if (settings.EnableSunshafts)
			{
				EnableSunshafts();
			}
			else
			{
				DisableSunshafts();
			}

			_currentSunshaftsEnabled = settings.EnableSunshafts;
		}

		/// <summary>
		/// Lazily initializes Unity components required for sunshaft rendering.
		/// Finds the main camera and directional sun light in the scene.
		/// Sets _isInitialized to true only when both components are successfully found.
		/// This method is idempotent and can be called multiple times safely.
		/// </summary>
		void InitializeComponents()
		{
			if (_mainCamera == null)
			{
				_mainCamera = Camera.main ?? Object.FindObjectOfType<Camera>();
			}

			if (_sunLight == null)
			{
				var lights = Object.FindObjectsOfType<Light>();

				foreach (var light in lights)
				{
					// Find the main directional light (sun)
					if (light.type == LightType.Directional && light.name == "Directional Light")
					{
						_sunLight = light;
						break;
					}
				}
			}

			if (_mainCamera != null && _sunLight != null)
			{
				_isInitialized = true;
				_logger.LogFormat(LogType.Log, "[NaturalLighting] Sunshafts: Initialized - Camera: Found, Sun Light: Found (name: '{0}')",
					_sunLight.name);
			}
			else
			{
				_logger.LogFormat(LogType.Warning, "[NaturalLighting] Sunshafts: Initialization incomplete - Camera: {0}, Sun Light: {1}",
					_mainCamera != null, _sunLight != null);
			}
		}

		/// <summary>
		/// Loads the custom sunshaft composite shader from the mod's shader bundle.
		/// Creates a Material instance from the loaded shader for use in image effects.
		/// Uses lazy loading - only loads shaders when first needed.
		/// </summary>
		void LoadSunshaftShaders()
		{
			if (_sunShaftShaderMaterial != null)
			{
				_logger.LogFormat(LogType.Log, "[NaturalLighting] Sunshafts: Shaders already loaded, skipping");
				return;
			}

			try
			{
				_logger.LogFormat(LogType.Log, "[NaturalLighting] Sunshafts: Attempting to load shaders from shader bundle");

				var sunShaftsCompositeShader = _shaderProvider.GetShader("SunShaftsComposite", "sunshafts");

				if (sunShaftsCompositeShader != null)
				{
					_sunShaftShaderMaterial = new Material(sunShaftsCompositeShader);
					_logger.LogFormat(LogType.Log, "[NaturalLighting] Sunshafts: Successfully loaded custom shaders");
				}
				else
				{
					_logger.LogFormat(LogType.Warning, "[NaturalLighting] Sunshafts: Shader not found");
				}
			}
			catch (System.Exception e)
			{
				_logger.LogFormat(LogType.Warning, "[NaturalLighting] Sunshafts: Shader loading failed: {0}", e.Message);
			}
		}

		/// <summary>
		/// Configures and applies sunshaft image effects to the main camera.
		/// Validates that required shaders and camera are available before proceeding.
		/// </summary>
		void ConfigureSunshafts()
		{
			if (_sunShaftShaderMaterial != null && _mainCamera != null)
			{
				AddSunshaftsImageEffect();
			}
			else
			{
				_logger.LogFormat(LogType.Warning, "[NaturalLighting] Sunshafts: Cannot add image effect - missing shaders or camera");
			}
		}

		/// <summary>
		/// Adds the SunShaftsImageEffect component to the main camera and configures it
		/// with the loaded shader material and scene lighting information.
		/// Handles cleanup of existing components before creating new ones.
		/// </summary>
		void AddSunshaftsImageEffect()
		{
			if (_sunShaftShaderMaterial == null || _mainCamera == null)
			{
				_logger.LogFormat(LogType.Warning, "[NaturalLighting] Sunshafts: Cannot add image effect - missing prerequisites");
				return;
			}

			if (_sunShaftsComponent != null && _sunTransform != null)
			{
				_logger.LogFormat(LogType.Log, "[NaturalLighting] Sunshafts: Image effect already active, skipping");
				return;
			}

			try
			{
				if (_sunShaftsComponent != null)
				{
					Object.DestroyImmediate(_sunShaftsComponent);
					_sunShaftsComponent = null;
				}

				if (_sunTransform != null)
				{
					Object.DestroyImmediate(_sunTransform);
					_sunTransform = null;
				}

				_sunShaftsComponent = _mainCamera.gameObject.AddComponent<SunShaftsImageEffect>();

				_sunTransform = new GameObject("SunTransform");
				var sunTransform = _sunTransform.transform;

				_sunShaftsComponent.Initialize(_sunLight, sunTransform, _sunShaftShaderMaterial, _logger);

				_logger.LogFormat(LogType.Log, "[NaturalLighting] Sunshafts: Added custom image effect");
			}
			catch (System.Exception e)
			{
				_logger.LogFormat(LogType.Error, "[NaturalLighting] Sunshafts: Failed to add image effect: {0}", e.Message);
			}
		}

		/// <summary>
		/// Validates that all required components are available and properly initialized.
		/// Checks if Unity objects are still valid (not destroyed) and resets references if needed.
		/// </summary>
		/// <returns>True if components are valid, false otherwise</returns>
		bool ValidateComponents()
		{
			if (_mainCamera == null)
			{
				_logger.LogFormat(LogType.Warning, "[NaturalLighting] Sunshafts: Main camera not found");
				return false;
			}

			if (_sunLight == null)
			{
				_logger.LogFormat(LogType.Warning, "[NaturalLighting] Sunshafts: Sun light not found");
				return false;
			}

			// Validate that Unity objects are still valid (not destroyed)
			try
			{
				var _ = _mainCamera.transform; // This will throw if destroyed
				var __ = _sunLight.transform;  // This will throw if destroyed
				return true;
			}
			catch (System.Exception)
			{
				_logger.LogFormat(LogType.Warning, "[NaturalLighting] Sunshafts: Components have been destroyed");
				_mainCamera = null;
				_sunLight = null;
				return false;
			}
		}

		/// <summary>
		/// Enables sunshaft rendering by initializing required components and applying image effects.
		/// Uses lazy loading for components and shaders - only initializes when actually needed.
		/// Validates all prerequisites before enabling the effects.
		/// </summary>
		void EnableSunshafts()
		{
			_logger.LogFormat(LogType.Log, "[NaturalLighting] Sunshafts.EnableSunshafts: Enabled");

			if (!_isInitialized)
			{
				_logger.LogFormat(LogType.Log, "[NaturalLighting] Sunshafts: Initializing components for first use");
				InitializeComponents();
			}

			if (_sunShaftShaderMaterial == null)
			{
				_logger.LogFormat(LogType.Log, "[NaturalLighting] Sunshafts: Loading shaders for first use");
				LoadSunshaftShaders();
			}

			if (!ValidateComponents())
			{
				_logger.LogFormat(LogType.Error, "[NaturalLighting] Sunshafts: Required components not available, cannot enable");
				return;
			}

			ConfigureSunshafts();
		}

		/// <summary>
		/// Called when the mod is being unloaded. Disables sunshaft effects to clean up resources.
		/// </summary>
		public override void OnUnloading() => DisableSunshafts();

		/// <summary>
		/// Called when the feature is being disposed. Performs complete cleanup of all resources
		/// including components, shaders, and state tracking variables.
		/// </summary>
		protected override void OnDispose()
		{
			DisableSunshafts();
			CleanupShaders();
			_shaderProvider?.Dispose();

			_isInitialized = false;
			_mainCamera = null;
			_sunLight = null;
		}

		/// <summary>
		/// Disables sunshaft rendering by removing image effect components from the camera.
		/// Cleans up Unity GameObjects and components to prevent memory leaks.
		/// </summary>
		void DisableSunshafts()
		{
			if (_sunShaftsComponent != null)
			{
				Object.DestroyImmediate(_sunShaftsComponent);
				_sunShaftsComponent = null;
			}

			if (_sunTransform != null)
			{
				Object.DestroyImmediate(_sunTransform);
				_sunTransform = null;
			}

			_logger.LogFormat(LogType.Log, "[NaturalLighting] Sunshafts: Disabled image effect");
		}

		/// <summary>
		/// Cleans up shader materials and releases associated GPU resources.
		/// Called during disposal to prevent memory leaks.
		/// </summary>
		void CleanupShaders()
		{
			if (_sunShaftShaderMaterial != null)
			{
				Object.DestroyImmediate(_sunShaftShaderMaterial);
				_sunShaftShaderMaterial = null;
			}
		}
	}
}
