using System.IO;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Unity Editor script to build natural lighting shaders into an AssetBundle.
/// This script creates the NaturalLighting AssetBundle for the Cities: Skylines NaturalLighting mod.
/// Compatible with Unity 5.6.7f1
/// </summary>
public class SunShaftsBuilder
{
	// Unity 5.6 prefers lowercase bundle names
	private const string BUNDLE_NAME = "naturallighting";

	[MenuItem("NaturalLighting/Build Natural Lighting Shader Bundle (All Platforms)")]
	static void BuildSunshaftsShaderBundleAllPlatforms()
	{
		// Define all supported platforms for Cities: Skylines (Windows and macOS only)
		// Using Unity 5.6.7f1 compatible BuildTarget values
		BuildTarget[] platforms = new BuildTarget[]
		{
			BuildTarget.StandaloneWindows64,
			BuildTarget.StandaloneOSXUniversal
		};

		string[] platformNames = new string[] { "Windows", "macOS" };

		// Find all shader files in Assets/Shaders/ directory
		string[] shaderPaths = Directory.GetFiles("Assets/Shaders/", "*.shader", SearchOption.AllDirectories);

		if (shaderPaths.Length == 0)
		{
			Debug.LogError("No shader files found in Assets/Shaders/ directory");
			return;
		}

		Debug.Log("Found " + shaderPaths.Length + " shader(s) in Assets/Shaders/:");

		// Validate shaders exist and assign to bundle
		bool allShadersFound = true;
		foreach (string shaderPath in shaderPaths)
		{
			AssetImporter importer = AssetImporter.GetAtPath(shaderPath);
			if (importer != null)
			{
				importer.assetBundleName = BUNDLE_NAME;
				Debug.Log("Assigned " + shaderPath + " to " + BUNDLE_NAME + " bundle");
			}
			else
			{
				Debug.LogError("Could not find shader at " + shaderPath);
				allShadersFound = false;
			}
		}

		if (!allShadersFound)
		{
			Debug.LogError("Not all shaders were found. Please ensure all .shader files are in Assets/Shaders/");
			return;
		}

		Debug.Log("=== Building Cross-Platform Shader Bundles ===");

		// Build for each platform
		bool allBuildsSucceeded = true;
		for (int i = 0; i < platforms.Length; i++)
		{
			try
			{
				string outputDir = "Assets/StreamingAssets/" + platformNames[i] + "/";
				if (!Directory.Exists(outputDir))
				{
					Directory.CreateDirectory(outputDir);
				}

				Debug.Log("Building AssetBundle for " + platformNames[i] + "...");

				// Unity 5.6.7f1 compatible build settings
				BuildPipeline.BuildAssetBundles(
					outputDir,
					BuildAssetBundleOptions.None,  // Keep it simple for Unity 5.6
					platforms[i]
				);

				// Verify the bundle was created
				string bundlePath = outputDir + BUNDLE_NAME;
				if (File.Exists(bundlePath))
				{
					Debug.Log("✅ " + platformNames[i] + " bundle created successfully: " + bundlePath);
				}
				else
				{
					Debug.LogError("❌ " + platformNames[i] + " bundle creation failed");
					allBuildsSucceeded = false;
				}
			}
			catch (System.Exception e)
			{
				Debug.LogError("❌ Failed to build " + platformNames[i] + " bundle: " + e.Message);
				allBuildsSucceeded = false;
			}
		}

		if (allBuildsSucceeded)
		{
			Debug.Log("=== All Platform Bundles Built Successfully! ===");
			Debug.Log("Bundles created in:");
			Debug.Log("  - Assets/StreamingAssets/Windows/" + BUNDLE_NAME);
			Debug.Log("  - Assets/StreamingAssets/macOS/" + BUNDLE_NAME);
			Debug.Log("");
			Debug.Log("Copy the appropriate bundle to your mod's Assets/Shaders/NaturalLighting/ directory");
		}
		else
		{
			Debug.LogError("Some platform builds failed. Check the console for details.");
		}

		// Show the output folder
		EditorUtility.RevealInFinder("Assets/StreamingAssets/");
	}

	[MenuItem("NaturalLighting/Build Natural Lighting Shader Bundle (Current Platform Only)")]
	static void BuildSunshaftsShaderBundleCurrentPlatform()
	{
		// Detect current platform for Cities: Skylines compatibility (Windows and macOS only)
		BuildTarget targetPlatform;
		string platformName;
		string platformFolder;

#if UNITY_EDITOR_OSX
		targetPlatform = BuildTarget.StandaloneOSXUniversal;
		platformName = "macOS";
		platformFolder = "macOS";
#elif UNITY_EDITOR_WIN
		targetPlatform = BuildTarget.StandaloneWindows64;
		platformName = "Windows";
		platformFolder = "Windows";
#else
		targetPlatform = BuildTarget.StandaloneOSXUniversal;
		platformName = "macOS";
		platformFolder = "macOS";
#endif

		// Ensure output directory exists
		string outputDir = "Assets/StreamingAssets/" + platformFolder + "/";
		if (!Directory.Exists(outputDir))
		{
			Directory.CreateDirectory(outputDir);
		}

		// Find all shader files in Assets/Shaders/ directory
		string[] shaderPaths = Directory.GetFiles("Assets/Shaders/", "*.shader", SearchOption.AllDirectories);

		if (shaderPaths.Length == 0)
		{
			Debug.LogError("No shader files found in Assets/Shaders/ directory");
			return;
		}

		Debug.Log("Found " + shaderPaths.Length + " shader(s) in Assets/Shaders/:");

		bool allShadersFound = true;
		foreach (string shaderPath in shaderPaths)
		{
			AssetImporter importer = AssetImporter.GetAtPath(shaderPath);
			if (importer != null)
			{
				importer.assetBundleName = BUNDLE_NAME;
				Debug.Log("Assigned " + shaderPath + " to " + BUNDLE_NAME + " bundle");
			}
			else
			{
				Debug.LogError("Could not find shader at " + shaderPath);
				allShadersFound = false;
			}
		}

		if (!allShadersFound)
		{
			Debug.LogError("Not all shaders were found. Please ensure all .shader files are in Assets/Shaders/");
			return;
		}

		// Build the AssetBundle
		try
		{
			Debug.Log("Building AssetBundle for " + platformName + "...");

			// Unity 5.6.7f1 compatible build settings
			BuildPipeline.BuildAssetBundles(
				outputDir,
				BuildAssetBundleOptions.None,
				targetPlatform
			);

			Debug.Log("=== " + platformName + " Shader Bundle Built Successfully! ===");
			Debug.Log("Output location: " + outputDir + BUNDLE_NAME);
			Debug.Log("Copy this file to your Cities: Skylines mod's Assets/Shaders/NaturalLighting/ directory");

			// Show the output folder in Finder/Explorer
			EditorUtility.RevealInFinder(outputDir);
		}
		catch (System.Exception e)
		{
			Debug.LogError("Failed to build shader bundle: " + e.Message);
		}
	}

	[MenuItem("NaturalLighting/Validate Shader Setup")]
	static void ValidateShaderSetup()
	{
		Debug.Log("=== Validating Shader Setup ===");

		string[] shaderPaths = Directory.GetFiles("Assets/Shaders/", "*.shader", SearchOption.AllDirectories);

		if (shaderPaths.Length == 0)
		{
			Debug.LogError("❌ No shader files found in Assets/Shaders/ directory");
			return;
		}

		bool allValid = true;

		foreach (string shaderPath in shaderPaths)
		{
			Shader shader = AssetDatabase.LoadAssetAtPath(shaderPath, typeof(Shader)) as Shader;
			if (shader != null)
			{
				Debug.Log("✅ Found: " + shaderPath);
			}
			else
			{
				Debug.LogError("❌ Missing: " + shaderPath);
				allValid = false;
			}
		}

		if (allValid)
		{
			Debug.Log("✅ All " + shaderPaths.Length + " shader(s) are present and ready for bundle creation!");
		}
		else
		{
			Debug.LogError("❌ Some shaders are missing. Please add them to Assets/Shaders/ before building.");
		}
	}
}