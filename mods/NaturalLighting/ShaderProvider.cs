using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace NaturalLighting
{
	interface IShaderProvider
	{
		AssetBundle GetOrLoadBundle(string name);
		Shader GetShader(string shaderName, string bundleName);
	}

	sealed class ShaderProvider : IShaderProvider, IDisposable
	{
		readonly ModInfo _mod;
		readonly DirectoryInfo _shadersDir;
		readonly Dictionary<string, AssetBundle> _loadedBundles = new Dictionary<string, AssetBundle>();
		readonly Dictionary<string, Shader> _loadedShaders = new Dictionary<string, Shader>();
		bool _disposedValue;

		public ShaderProvider(ModInfo mod)
		{
			_mod = mod;
			_shadersDir = new DirectoryInfo(Path.Combine(Path.Combine(mod.Directory.FullName, "Assets"), "Shaders"));
		}

		public Shader GetShader(string shaderName, string bundleName = null)
		{
			var key = bundleName != null ? $"{bundleName}:{shaderName}" : shaderName;

			if (_loadedShaders.TryGetValue(key, out var cachedShader) && cachedShader != null)
			{
				return cachedShader;
			}

			try
			{
				if (bundleName != null)
				{
					var bundle = GetOrLoadBundle(bundleName);
					if (bundle != null)
					{
						// First try to find by shader name using Unity's Shader.Find
						var shader = Shader.Find($"Hidden/{shaderName}");
						if (shader != null)
						{
							_loadedShaders[key] = shader;
							return shader;
						}

						// If that fails, try loading all assets and look for shaders
						var allAssets = bundle.LoadAllAssets<Shader>();
						foreach (var asset in allAssets)
						{
							if (asset.name.Contains(shaderName) || asset.name.EndsWith(shaderName, StringComparison.OrdinalIgnoreCase))
							{
								_loadedShaders[key] = asset;
								return asset;
							}
						}
					}
				}

				// Try loading from all available bundles
				foreach (var bundle in _loadedBundles.Values)
				{
					if (bundle != null)
					{
						var allAssets = bundle.LoadAllAssets<Shader>();
						foreach (var asset in allAssets)
						{
							if (asset.name.Contains(shaderName) || asset.name.EndsWith(shaderName, StringComparison.OrdinalIgnoreCase))
							{
								_loadedShaders[key] = asset;
								return asset;
							}
						}
					}
				}
			}
			catch (Exception ex)
			{
				Debug.LogError($"[NaturalLighting] Error loading shader {shaderName}: {ex.Message}");
			}

			return null;
		}

		public AssetBundle GetOrLoadBundle(string bundleName)
		{
			if (_loadedBundles.TryGetValue(bundleName, out var cachedBundle) && cachedBundle != null)
			{
				return cachedBundle;
			}

			try
			{
				// Detect platform at runtime
				string platformFolder;
				if (Application.platform == RuntimePlatform.WindowsPlayer || Application.platform == RuntimePlatform.WindowsEditor)
				{
					platformFolder = "Windows";
				}
				else if (Application.platform == RuntimePlatform.OSXPlayer || Application.platform == RuntimePlatform.OSXEditor)
				{
					platformFolder = "macOS";
				}
				else
				{
					throw new PlatformNotSupportedException($"[NaturalLighting] Unsupported platform: {Application.platform}. Only Windows and macOS are supported.");
				}

				// Try platform-specific folder
				var platformBundlePath = Path.Combine(Path.Combine(_shadersDir.FullName, platformFolder), bundleName);
				if (File.Exists(platformBundlePath))
				{
					var bundle = AssetBundle.LoadFromFile(platformBundlePath);
					if (bundle != null)
					{
						_loadedBundles[bundleName] = bundle;
						Debug.Log($"[NaturalLighting] Loaded shader bundle from {platformFolder}: {platformBundlePath}");
						return bundle;
					}
				}

				// Fallback to direct path
				var bundlePath = Path.Combine(_shadersDir.FullName, bundleName);
				if (File.Exists(bundlePath))
				{
					var bundle = AssetBundle.LoadFromFile(bundlePath);
					if (bundle != null)
					{
						_loadedBundles[bundleName] = bundle;
						Debug.Log($"[NaturalLighting] Loaded shader bundle from fallback path: {bundlePath}");
						return bundle;
					}
				}

				Debug.LogWarning($"[NaturalLighting] Could not find shader bundle '{bundleName}' in platform folder '{platformFolder}' or fallback location");
			}
			catch (Exception ex)
			{
				Debug.LogError($"[NaturalLighting] Error loading bundle {bundleName}: {ex.Message}");
			}

			return null;
		}

		void UnloadAll()
		{
			foreach (var bundle in _loadedBundles.Values)
			{
				if (bundle != null)
				{
					bundle.Unload(true);
				}
			}
			_loadedBundles.Clear();
			_loadedShaders.Clear();
		}

		private void Dispose(bool disposing)
		{
			if (_disposedValue)
			{
				return;
			}

			if (disposing)
			{
				UnloadAll();
			}

			_disposedValue = true;
		}

		public void Dispose()
		{
			// Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
			Dispose(disposing: true);
			GC.SuppressFinalize(this);
		}
	}
}
