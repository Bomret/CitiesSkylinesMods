using Common;
using NaturalLighting.Settings;
using UnityEngine;

namespace NaturalLighting.Features.SunShafts
{
	/// <summary>
	/// Manages volumetric sunshaft rendering effects for the Natural Lighting mod.
	/// Looks up the Unity components on every enable, loads shaders when needed, and enables/disables
	/// sunshaft image effects based on user settings.
	/// </summary>
	sealed class SunshaftsFeature : Feature<ModSettings>
	{
		Camera _mainCamera;
		Light _sunLight;

		bool _currentSunshaftsEnabled;

		Material _sunShaftShaderMaterial;
		SunShaftsEffect _sunShaftsComponent;

		IShaderProvider _shaderProvider;

		/// <summary>
		/// Initializes a new instance of the SunshaftsFeature class.
		/// </summary>
		/// <param name="modProvider">Provider for accessing mod resources and metadata.</param>
		/// <param name="logger">Logger for diagnostic output.</param>
		public SunshaftsFeature(ILogger logger) : base(logger) { }

		/// <summary>
		/// Called when the mod is loaded. Initializes the shader provider and enables
		/// sunshafts if the setting is enabled.
		/// </summary>
		/// <param name="settings">Current mod settings containing sunshaft preferences.</param>
		public override void OnLoaded(IServiceProvider serviceProvider, ModSettings settings)
		{
			Logger.LogFormat(LogType.Log, "[NaturalLighting] Sunshafts.OnLoaded called with EnableSunshafts: {0}", settings.UseSunshafts);

			_shaderProvider = serviceProvider.GetObj<IShaderProvider>();

			_currentSunshaftsEnabled = settings.UseSunshafts;
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
			if (_currentSunshaftsEnabled == settings.UseSunshafts) return;

			Logger.LogFormat(LogType.Log, "[NaturalLighting] Sunshafts: Settings changed from {0} to {1}",
				_currentSunshaftsEnabled, settings.UseSunshafts);

			if (settings.UseSunshafts)
			{
				EnableSunshafts();
			}
			else
			{
				DisableSunshafts();
			}

			_currentSunshaftsEnabled = settings.UseSunshafts;
		}

		/// <summary>
		/// Finds the main camera, keeping a cached one only while it is still alive, and takes the sun
		/// light the game itself provides through <see cref="DayNightProperties.sunLightSource"/>.
		/// Runs on every enable, so objects destroyed with a previous level are found again.
		/// This method is idempotent and can be called multiple times safely.
		/// </summary>
		void InitializeComponents()
		{
			if (_mainCamera == null)
			{
				_mainCamera = Camera.main ?? Object.FindObjectOfType<Camera>();
			}

			// Unity's == null also treats a day-night object destroyed with a previous level as missing.
			var dayNightProperties = DayNightProperties.instance;
			_sunLight = dayNightProperties != null ? dayNightProperties.sunLightSource : null;

			if (_mainCamera != null && _sunLight != null)
			{
				Logger.LogFormat(LogType.Log, "[NaturalLighting] Sunshafts: Initialized - Camera: Found, Sun Light: Found (name: '{0}')",
					_sunLight.name);
			}
			else
			{
				Logger.LogFormat(LogType.Warning, "[NaturalLighting] Sunshafts: Initialization incomplete - Camera: {0}, Sun Light: {1}",
					_mainCamera != null, _sunLight != null);
			}
		}

		/// <summary>
		/// Loads the custom sunshaft composite shader from the mod's shader bundle.
		/// Creates a Material instance from the loaded shader for use in image effects.
		/// Loads the shader only while no material exists.
		/// </summary>
		void LoadSunshaftShaders()
		{
			if (_sunShaftShaderMaterial != null)
			{
				Logger.LogFormat(LogType.Log, "[NaturalLighting] Sunshafts: Shaders already loaded, skipping");
				return;
			}

			try
			{
				Logger.LogFormat(LogType.Log, "[NaturalLighting] Sunshafts: Attempting to load shaders from shader bundle");

				var sunShaftsCompositeShader = _shaderProvider.GetShader("SunShafts", "naturallighting");

				if (sunShaftsCompositeShader != null)
				{
					_sunShaftShaderMaterial = new Material(sunShaftsCompositeShader);
					Logger.LogFormat(LogType.Log, "[NaturalLighting] Sunshafts: Successfully loaded custom shaders");
				}
				else
				{
					Logger.LogFormat(LogType.Warning, "[NaturalLighting] Sunshafts: Shader not found");
				}
			}
			catch (System.Exception e)
			{
				Logger.LogFormat(LogType.Warning, "[NaturalLighting] Sunshafts: Shader loading failed: {0}", e.Message);
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

				return;
			}

			Logger.LogFormat(LogType.Warning, "[NaturalLighting] Sunshafts: Cannot add image effect - missing shaders or camera");
		}

		/// <summary>
		/// Adds the SunShaftsImageEffect component to the main camera and configures it
		/// with the loaded shader material and scene lighting information.
		/// Skips the add while an effect is still alive on the camera.
		/// </summary>
		void AddSunshaftsImageEffect()
		{
			if (_sunShaftShaderMaterial == null || _mainCamera == null)
			{
				Logger.LogFormat(LogType.Warning, "[NaturalLighting] Sunshafts: Cannot add image effect - missing prerequisites");
				return;
			}

			// Unity's == null also treats an effect destroyed with the camera on a level load as missing.
			if (_sunShaftsComponent != null)
			{
				Logger.LogFormat(LogType.Log, "[NaturalLighting] Sunshafts: Image effect already active, skipping");
				return;
			}

			try
			{
				_sunShaftsComponent = _mainCamera.gameObject.AddComponent<SunShaftsEffect>();

				_sunShaftsComponent.Initialize(_sunLight, _sunShaftShaderMaterial, Logger);

				Logger.LogFormat(LogType.Log, "[NaturalLighting] Sunshafts: Added custom image effect");
			}
			catch (System.Exception e)
			{
				Logger.LogFormat(LogType.Error, "[NaturalLighting] Sunshafts: Failed to add image effect: {0}", e.Message);
			}
		}

		/// <summary>
		/// Validates that all required components are available and properly initialized.
		/// Checks if Unity objects are still valid (not destroyed).
		/// </summary>
		/// <returns>True if components are valid, false otherwise</returns>
		bool ValidateComponents()
		{
			if (_mainCamera == null)
			{
				Logger.LogFormat(LogType.Warning, "[NaturalLighting] Sunshafts: Main camera not found");
				return false;
			}

			if (_sunLight == null)
			{
				Logger.LogFormat(LogType.Warning, "[NaturalLighting] Sunshafts: Sun light not found");
				return false;
			}

			// Validate that Unity objects are still valid (not destroyed)
			try
			{
				var _ = _mainCamera.transform; // This will throw if destroyed
				_ = _sunLight.transform;  // This will throw if destroyed

				return true;
			}
			catch (System.Exception)
			{
				Logger.LogFormat(LogType.Warning, "[NaturalLighting] Sunshafts: Components have been destroyed");
				return false;
			}
		}

		/// <summary>
		/// Enables sunshaft rendering by initializing required components and applying image effects.
		/// Looks up the components on every call, so a camera or light destroyed with a previous level
		/// is replaced. Loads shaders only while no material exists.
		/// Validates all prerequisites before enabling the effects.
		/// </summary>
		void EnableSunshafts()
		{
			Logger.LogFormat(LogType.Log, "[NaturalLighting] Sunshafts.EnableSunshafts: Enabled");

			InitializeComponents();

			if (_sunShaftShaderMaterial == null)
			{
				Logger.LogFormat(LogType.Log, "[NaturalLighting] Sunshafts: Loading shaders");
				LoadSunshaftShaders();
			}

			if (!ValidateComponents())
			{
				Logger.LogFormat(LogType.Warning, "[NaturalLighting] Sunshafts: Validation failed, cannot enable");

				_mainCamera = null;
				_sunLight = null;

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

			_mainCamera = null;
			_sunLight = null;
		}

		/// <summary>
		/// Disables sunshaft rendering by removing image effect components from the camera.
		/// Destroys the effect component to prevent memory leaks.
		/// </summary>
		void DisableSunshafts()
		{
			if (_sunShaftsComponent != null)
			{
				Object.DestroyImmediate(_sunShaftsComponent);
				_sunShaftsComponent = null;
			}

			Logger.LogFormat(LogType.Log, "[NaturalLighting] Sunshafts: Disabled image effect");
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
