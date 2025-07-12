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
		bool _isInitialized;

		Material _sunShaftShaderMaterial;
		SunShaftsImageEffect _sunShaftsComponent;
		GameObject _sunTransformGO;
		ShaderProvider _shaderProvider;

		public SunshaftsFeature(IModProvider modProvider, ILogger logger)
		{
			_modProvider = modProvider;
			_logger = logger;
		}

		public override void OnLoaded(ModSettings settings)
		{
			_logger.LogFormat(LogType.Log, "[NaturalLighting] Sunshafts.OnLoaded called with EnableSunshafts: {0}", settings.EnableSunshafts);

			if (_isInitialized)
			{
				_logger.LogFormat(LogType.Log, "[NaturalLighting] Sunshafts: Already initialized, skipping");
				return;
			}

			var mod = _modProvider.GetCurrentMod();
			_shaderProvider = new ShaderProvider(mod);

			_currentSunshaftsEnabled = settings.EnableSunshafts;
			if (!_currentSunshaftsEnabled) return;

			EnableSunshafts();
		}

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

			_logger.LogFormat(LogType.Log, "[NaturalLighting] Sunshafts: Initialized - Camera: {0}, Sun Light: {1} (name: '{2}')",
				_mainCamera != null, _sunLight != null, _sunLight?.name ?? "null");

			// Mark as initialized if we have at least the essential components
			if (_mainCamera != null || _sunLight != null)
			{
				_isInitialized = true;
			}
		}

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

		void AddSunshaftsImageEffect()
		{
			if (_sunShaftShaderMaterial == null || _mainCamera == null)
			{
				_logger.LogFormat(LogType.Warning, "[NaturalLighting] Sunshafts: Cannot add image effect - missing prerequisites");
				return;
			}

			if (_sunShaftsComponent != null && _sunTransformGO != null)
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

				if (_sunTransformGO != null)
				{
					Object.DestroyImmediate(_sunTransformGO);
					_sunTransformGO = null;
				}

				_sunShaftsComponent = _mainCamera.gameObject.AddComponent<SunShaftsImageEffect>();

				_sunTransformGO = new GameObject("SunTransform");
				var sunTransform = _sunTransformGO.transform;

				_sunShaftsComponent.Initialize(_sunLight, sunTransform, _sunShaftShaderMaterial, _logger);

				_logger.LogFormat(LogType.Log, "[NaturalLighting] Sunshafts: Added custom image effect");
			}
			catch (System.Exception e)
			{
				_logger.LogFormat(LogType.Error, "[NaturalLighting] Sunshafts: Failed to add image effect: {0}", e.Message);
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

		void EnableSunshafts()
		{
			_logger.LogFormat(LogType.Log, "[NaturalLighting] Sunshafts.EnableSunshafts: Enabled");

			if (_mainCamera == null || _sunLight == null)
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

		public override void OnUnloading() => DisableSunshafts();

		protected override void OnDispose()
		{
			DisableSunshafts();
			CleanupShaders();
			_shaderProvider?.Dispose();

			_isInitialized = false;
			_mainCamera = null;
			_sunLight = null;
		}

		void DisableSunshafts()
		{
			if (_sunShaftsComponent != null)
			{
				Object.DestroyImmediate(_sunShaftsComponent);
				_sunShaftsComponent = null;
			}

			if (_sunTransformGO != null)
			{
				Object.DestroyImmediate(_sunTransformGO);
				_sunTransformGO = null;
			}

			_logger.LogFormat(LogType.Log, "[NaturalLighting] Sunshafts: Disabled image effect");
		}

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
