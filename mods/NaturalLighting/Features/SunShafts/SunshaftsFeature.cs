using NaturalLighting.Settings;
using UnityEngine;

namespace NaturalLighting.Features.SunShafts
{
	sealed class SunshaftsFeature : Feature<ModSettings>
	{
		readonly ILogger _logger;
		readonly IModProvider _modProvider;

		// Cached components for performance optimization
		Camera _mainCamera;
		Light _sunLight;
		bool _currentSunshaftsEnabled;
		bool _hasStoredOriginalValues;
		bool _isInitialized;

		float _originalLightIntensity;
		LightShadows _originalShadowType;
		Color _originalLightColor;
		bool _originalFogEnabled;
		FogMode _originalFogMode;
		float _originalFogDensity;
		Color _originalFogColor;

		Material _sunShaftShaderMaterial;
		SunShaftsImageEffect _sunShaftsComponent;
		GameObject _sunTransformGO;
		ShaderProvider _shaderProvider;

		// Performance optimization: Cache lighting state to avoid redundant operations
		bool _lightingEnhancementsApplied;
		bool _fogEnhancementsApplied;

		// Feature-specific configuration values (lighting and atmosphere enhancements)
		const float ENHANCED_LIGHT_INTENSITY = 1.2f;      // Moderate brightness increase
		const float FOG_DENSITY_MULTIPLIER = 1.2f;        // Subtle atmospheric enhancement
		const float FOG_COLOR_BLEND = 0.3f;               // Gentle sun color influence

		public SunshaftsFeature(IModProvider modProvider, ILogger logger)
		{
			_modProvider = modProvider;
			_logger = logger;
		}

		public override void OnLoaded(ModSettings settings)
		{
			_logger.LogFormat(LogType.Log, "[NaturalLighting] Sunshafts.OnLoaded called with EnableSunshafts: {0}", settings.EnableSunshafts);

			// Early exit if already initialized to avoid redundant work
			if (_isInitialized)
			{
				_logger.LogFormat(LogType.Log, "[NaturalLighting] Sunshafts: Already initialized, skipping");
				return;
			}

			var mod = _modProvider.GetCurrentMod();
			_shaderProvider = new ShaderProvider(mod);

			try
			{
				InitializeComponents();

				if (!_hasStoredOriginalValues)
				{
					StoreOriginalSettings();
					_hasStoredOriginalValues = true;
				}

				LoadSunshaftShaders();

				_currentSunshaftsEnabled = settings.EnableSunshafts;
				if (_currentSunshaftsEnabled)
				{
					EnableSunshafts(true);
				}
				else
				{
					_logger.LogFormat(LogType.Log, "[NaturalLighting] Sunshafts: Skipping enable because setting is disabled");
				}

				_isInitialized = true;
			}
			catch (System.Exception e)
			{
				_logger.LogFormat(LogType.Error, "[NaturalLighting] Sunshafts.OnLoaded failed: {0}", e.Message);
			}
		}

		public override void OnSettingsChanged(ModSettings settings)
		{
			// Early exit if no change needed
			if (_currentSunshaftsEnabled == settings.EnableSunshafts)
			{
				return;
			}

			// Log the change for debugging
			_logger.LogFormat(LogType.Log, "[NaturalLighting] Sunshafts: Settings changed from {0} to {1}",
				_currentSunshaftsEnabled, settings.EnableSunshafts);

			EnableSunshafts(settings.EnableSunshafts);
			_currentSunshaftsEnabled = settings.EnableSunshafts;
		}

		public override void OnUnloading() => EnableSunshafts(false);

		protected override void OnDispose()
		{
			EnableSunshafts(false);
			CleanupShaders();
			_shaderProvider?.Dispose();

			// Reset all cached state
			_isInitialized = false;
			_lightingEnhancementsApplied = false;
			_fogEnhancementsApplied = false;
			_mainCamera = null;
			_sunLight = null;
		}

		void InitializeComponents()
		{
			// Cache main camera once to avoid repeated lookups
			if (_mainCamera == null)
			{
				_mainCamera = Camera.main ?? Object.FindObjectOfType<Camera>();
			}

			// Find the main directional light (sun) - optimize by breaking early and caching transform
			if (_sunLight == null)
			{
				var lights = Object.FindObjectsOfType<Light>();

				// Try exact name first like reference
				foreach (var light in lights)
				{
					if (light.type == LightType.Directional && light.name == "Directional Light")
					{
						_sunLight = light;
						break;
					}
				}

				// Fallback to any light containing "Sun" in the name
				if (_sunLight == null)
				{
					foreach (var light in lights)
					{
						if (light.type == LightType.Directional && light.name.Contains("Sun"))
						{
							_sunLight = light;
							break;
						}
					}
				}

				// Final fallback to first directional light
				if (_sunLight == null)
				{
					foreach (var light in lights)
					{
						if (light.type == LightType.Directional)
						{
							_sunLight = light;
							break;
						}
					}
				}
			}

			_logger.LogFormat(LogType.Log, "[NaturalLighting] Sunshafts: Initialized - Camera: {0}, Sun Light: {1} (name: '{2}')",
				_mainCamera != null, _sunLight != null, _sunLight?.name ?? "null");
		}

		void LoadSunshaftShaders()
		{
			// Skip if material already loaded to avoid redundant operations
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
					_logger.LogFormat(LogType.Warning, "[NaturalLighting] Sunshafts: Shader not found - will use lighting enhancements only");
				}
			}
			catch (System.Exception e)
			{
				_logger.LogFormat(LogType.Warning, "[NaturalLighting] Sunshafts: Shader loading failed: {0}", e.Message);
			}
		}

		void StoreOriginalSettings()
		{
			// Store light settings
			if (_sunLight != null)
			{
				_originalLightIntensity = _sunLight.intensity;
				_originalShadowType = _sunLight.shadows;
				_originalLightColor = _sunLight.color;
			}

			// Store fog settings
			_originalFogEnabled = RenderSettings.fog;
			_originalFogMode = RenderSettings.fogMode;
			_originalFogDensity = RenderSettings.fogDensity;
			_originalFogColor = RenderSettings.fogColor;

			_logger.LogFormat(LogType.Log, "[NaturalLighting] Sunshafts: Stored original settings");
		}

		void EnableSunshafts(bool enable)
		{
			_logger.LogFormat(LogType.Log, "[NaturalLighting] Sunshafts.EnableSunshafts: {0}", enable ? "Enabled" : "Disabled");

			if (enable)
			{
				// Validate components before enabling, reinitialize if needed
				if (!ValidateComponents())
				{
					_logger.LogFormat(LogType.Log, "[NaturalLighting] Sunshafts: Components invalid, attempting reinitialization");
					InitializeComponents();

					if (!ValidateComponents())
					{
						_logger.LogFormat(LogType.Error, "[NaturalLighting] Sunshafts: Failed to initialize components, aborting enable");
						return;
					}
				}

				ConfigureSunshafts();
			}
			else
			{
				DisableSunshafts();
			}
		}

		void ConfigureSunshafts()
		{
			// Apply moderate lighting enhancements (avoid redundant operations)
			if (_sunLight != null && !_lightingEnhancementsApplied)
			{
				_sunLight.intensity = ENHANCED_LIGHT_INTENSITY;
				_sunLight.shadows = LightShadows.Soft;
				_sunLight.color = Color.Lerp(_originalLightColor, new Color(1.0f, 0.95f, 0.85f), 0.3f);
				_lightingEnhancementsApplied = true;

				_logger.LogFormat(LogType.Log, "[NaturalLighting] Sunshafts: Enhanced sun light (intensity: {0})", ENHANCED_LIGHT_INTENSITY);
			}

			// Apply subtle atmospheric enhancements (avoid redundant operations)
			if (!_fogEnhancementsApplied)
			{
				RenderSettings.fog = true;
				RenderSettings.fogMode = FogMode.ExponentialSquared;
				RenderSettings.fogDensity = _originalFogDensity * FOG_DENSITY_MULTIPLIER;

				if (_sunLight != null)
				{
					RenderSettings.fogColor = Color.Lerp(_originalFogColor, _sunLight.color, FOG_COLOR_BLEND);
				}
				_fogEnhancementsApplied = true;
			}

			// Add sunshafts image effect if we have custom shaders
			if (_sunShaftShaderMaterial != null && _mainCamera != null)
			{
				AddSunshaftsImageEffect();
			}
			else
			{
				_logger.LogFormat(LogType.Log, "[NaturalLighting] Sunshafts: Using lighting enhancements only (no custom shaders)");
			}
		}

		void AddSunshaftsImageEffect()
		{
			// Validate preconditions to avoid unnecessary operations
			if (_sunShaftShaderMaterial == null || _mainCamera == null)
			{
				_logger.LogFormat(LogType.Warning, "[NaturalLighting] Sunshafts: Cannot add image effect - missing prerequisites");
				return;
			}

			// Skip if component already exists and is properly configured
			if (_sunShaftsComponent != null && _sunTransformGO != null)
			{
				_logger.LogFormat(LogType.Log, "[NaturalLighting] Sunshafts: Image effect already active, skipping");
				return;
			}

			try
			{
				// Remove existing component if present
				if (_sunShaftsComponent != null)
				{
					Object.DestroyImmediate(_sunShaftsComponent);
					_sunShaftsComponent = null;
				}

				// Cleanup existing transform if present
				if (_sunTransformGO != null)
				{
					Object.DestroyImmediate(_sunTransformGO);
					_sunTransformGO = null;
				}

				_sunShaftsComponent = _mainCamera.gameObject.AddComponent<SunShaftsImageEffect>();

				// Create a simple sunTransform GameObject
				_sunTransformGO = new GameObject("SunTransform");
				var sunTransform = _sunTransformGO.transform;

				// No need to position it here - it will be positioned every frame in OnRenderImage
				_logger.LogFormat(LogType.Log, "[NaturalLighting] Sunshafts: Created sun transform GameObject");

				_sunShaftsComponent.Initialize(_sunLight, sunTransform, _sunShaftShaderMaterial, _logger);

				_logger.LogFormat(LogType.Log, "[NaturalLighting] Sunshafts: Added custom image effect");
			}
			catch (System.Exception e)
			{
				_logger.LogFormat(LogType.Error, "[NaturalLighting] Sunshafts: Failed to add image effect: {0}", e.Message);
			}
		}

		void DisableSunshafts()
		{
			// Remove image effect
			if (_sunShaftsComponent != null)
			{
				Object.DestroyImmediate(_sunShaftsComponent);
				_sunShaftsComponent = null;
			}

			// Cleanup sunTransform GameObject
			if (_sunTransformGO != null)
			{
				Object.DestroyImmediate(_sunTransformGO);
				_sunTransformGO = null;
			}

			// Restore original light settings (only if they were modified)
			if (_sunLight != null && _lightingEnhancementsApplied)
			{
				_sunLight.intensity = _originalLightIntensity;
				_sunLight.shadows = _originalShadowType;
				_sunLight.color = _originalLightColor;
				_lightingEnhancementsApplied = false;
			}

			// Restore original fog settings (only if they were modified)
			if (_fogEnhancementsApplied)
			{
				RenderSettings.fog = _originalFogEnabled;
				RenderSettings.fogMode = _originalFogMode;
				RenderSettings.fogDensity = _originalFogDensity;
				RenderSettings.fogColor = _originalFogColor;
				_fogEnhancementsApplied = false;
			}

			_logger.LogFormat(LogType.Log, "[NaturalLighting] Sunshafts: Restored all original settings");
		}

		void CleanupShaders()
		{
			if (_sunShaftShaderMaterial != null)
			{
				Object.DestroyImmediate(_sunShaftShaderMaterial);
				_sunShaftShaderMaterial = null;
			}
		}

		/// <summary>
		/// Validates that all required components are available and properly initialized
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
			}
			catch (System.Exception)
			{
				_logger.LogFormat(LogType.Warning, "[NaturalLighting] Sunshafts: Components have been destroyed, reinitializing");
				_mainCamera = null;
				_sunLight = null;
				return false;
			}

			return true;
		}
	}
}
