using NaturalLighting.Settings;
using UnityEngine;

namespace NaturalLighting.Features.SunShafts
{
	sealed class SunshaftsFeature : Feature<ModSettings>
	{
		readonly ILogger _logger;
		readonly IModProvider _modProvider;

		Camera _mainCamera;
		Light _sunLight;
		bool _currentSunshaftsEnabled;
		bool _hasStoredOriginalValues;

		float _originalLightIntensity;
		LightShadows _originalShadowType;
		Color _originalLightColor;
		bool _originalFogEnabled;
		FogMode _originalFogMode;
		float _originalFogDensity;
		Color _originalFogColor;

		Material _sunShaftShaderMaterial;
		Material _simpleClearShaderMaterial;
		SunShaftsImageEffect _sunShaftsComponent;
		GameObject _sunTransformGO;
		ShaderProvider _shaderProvider;

		// Sunshaft configuration values
		const float ENHANCED_LIGHT_INTENSITY = 1.2f;      // Moderate brightness increase
		const float FOG_DENSITY_MULTIPLIER = 1.2f;        // Subtle atmospheric enhancement
		const float FOG_COLOR_BLEND = 0.3f;               // Gentle sun color influence
		const float SUNSHAFT_INTENSITY = 2.0f;            // God ray strength (restored from debugging)
		const float SUNSHAFT_THRESHOLD = 0.5f;            // Threshold for ray visibility (lowered for more rays)
		const float SUNSHAFT_BLUR_RADIUS = 3.0f;          // Ray blur amount (increased for visibility)
		const int SUNSHAFT_BLUR_ITERATIONS = 3;           // Blur quality (increased for better effect)

		public SunshaftsFeature(IModProvider modProvider, ILogger logger)
		{
			_modProvider = modProvider;
			_logger = logger;
		}

		public override void OnLoaded(ModSettings settings)
		{
			_logger.LogFormat(LogType.Log, "[NaturalLighting] Sunshafts.OnLoaded called with EnableSunshafts: {0}", settings.EnableSunshafts);

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
			}
			catch (System.Exception e)
			{
				_logger.LogFormat(LogType.Error, "[NaturalLighting] Sunshafts.OnLoaded failed: {0}", e.Message);
			}
		}

		public override void OnSettingsChanged(ModSettings settings)
		{
			if (_currentSunshaftsEnabled == settings.EnableSunshafts) return;

			EnableSunshafts(settings.EnableSunshafts);
			_currentSunshaftsEnabled = settings.EnableSunshafts;
		}

		public override void OnUnloading() => EnableSunshafts(false);

		protected override void OnDispose()
		{
			EnableSunshafts(false);
			CleanupShaders();
			_shaderProvider?.Dispose();
		}

		void InitializeComponents()
		{
			_mainCamera = Camera.main ?? Object.FindObjectOfType<Camera>();

			// Find the main directional light (sun) - try exact name first like reference
			var lights = Object.FindObjectsOfType<Light>();
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

			_logger.LogFormat(LogType.Log, "[NaturalLighting] Sunshafts: Initialized - Camera: {0}, Sun Light: {1} (name: '{2}')",
				_mainCamera != null, _sunLight != null, _sunLight?.name ?? "null");
		}

		void LoadSunshaftShaders()
		{
			try
			{
				_logger.LogFormat(LogType.Log, "[NaturalLighting] Sunshafts: Attempting to load shaders from shader bundle");

				var sunShaftsCompositeShader = _shaderProvider.GetShader("SunShaftsComposite", "sunshafts");
				var simpleClearShader = _shaderProvider.GetShader("SimpleClear", "sunshafts");

				if (sunShaftsCompositeShader != null && simpleClearShader != null)
				{
					_sunShaftShaderMaterial = new Material(sunShaftsCompositeShader);
					_simpleClearShaderMaterial = new Material(simpleClearShader);

					_logger.LogFormat(LogType.Log, "[NaturalLighting] Sunshafts: Successfully loaded custom shaders");
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
				ConfigureSunshafts();
			}
			else
			{
				DisableSunshafts();
			}
		}

		void ConfigureSunshafts()
		{
			// Apply moderate lighting enhancements
			if (_sunLight != null)
			{
				_sunLight.intensity = ENHANCED_LIGHT_INTENSITY;
				_sunLight.shadows = LightShadows.Soft;
				_sunLight.color = Color.Lerp(_originalLightColor, new Color(1.0f, 0.95f, 0.85f), 0.3f);

				_logger.LogFormat(LogType.Log, "[NaturalLighting] Sunshafts: Enhanced sun light (intensity: {0})", ENHANCED_LIGHT_INTENSITY);
			}

			// Apply subtle atmospheric enhancements
			RenderSettings.fog = true;
			RenderSettings.fogMode = FogMode.ExponentialSquared;
			RenderSettings.fogDensity = _originalFogDensity * FOG_DENSITY_MULTIPLIER;

			if (_sunLight != null)
			{
				RenderSettings.fogColor = Color.Lerp(_originalFogColor, _sunLight.color, FOG_COLOR_BLEND);
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
			try
			{
				// Remove existing component if present
				if (_sunShaftsComponent != null)
				{
					Object.DestroyImmediate(_sunShaftsComponent);
				}

				_sunShaftsComponent = _mainCamera.gameObject.AddComponent<SunShaftsImageEffect>();

				// Create a simple sunTransform GameObject like the reference implementation
				_sunTransformGO = new GameObject("SunTransform");
				var sunTransform = _sunTransformGO.transform;

				// No need to position it here - it will be positioned every frame in OnRenderImage
				_logger.LogFormat(LogType.Log, "[NaturalLighting] Sunshafts: Created sun transform GameObject");

				_sunShaftsComponent.Initialize(_sunLight, sunTransform, _sunShaftShaderMaterial, _simpleClearShaderMaterial,
					SUNSHAFT_INTENSITY, SUNSHAFT_THRESHOLD, SUNSHAFT_BLUR_RADIUS,
					SUNSHAFT_BLUR_ITERATIONS, _logger);

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

			// Restore original light settings
			if (_sunLight != null)
			{
				_sunLight.intensity = _originalLightIntensity;
				_sunLight.shadows = _originalShadowType;
				_sunLight.color = _originalLightColor;
			}

			// Restore original fog settings
			RenderSettings.fog = _originalFogEnabled;
			RenderSettings.fogMode = _originalFogMode;
			RenderSettings.fogDensity = _originalFogDensity;
			RenderSettings.fogColor = _originalFogColor;

			_logger.LogFormat(LogType.Log, "[NaturalLighting] Sunshafts: Restored all original settings");
		}

		void CleanupShaders()
		{
			if (_sunShaftShaderMaterial != null)
			{
				Object.DestroyImmediate(_sunShaftShaderMaterial);
				_sunShaftShaderMaterial = null;
			}

			if (_simpleClearShaderMaterial != null)
			{
				Object.DestroyImmediate(_simpleClearShaderMaterial);
				_simpleClearShaderMaterial = null;
			}
		}

	}
}
