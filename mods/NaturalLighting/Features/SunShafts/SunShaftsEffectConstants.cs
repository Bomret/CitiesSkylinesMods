namespace NaturalLighting.Features.SunShafts
{
	/// <summary>
	/// Static constants used by the SunShaftsImageEffect for consistent configuration
	/// and mathematical calculations across the volumetric sun shafts rendering pipeline.
	/// </summary>
	static class SunShaftsEffectConstants
	{
		// Sun positioning and falloff constants
		public const float SUN_DISTANCE = 2000f;                    // Distance of sun transform from camera
		public const float SUN_MAX_RADIUS = 0.75f;                  // Maximum radius for sun falloff

		// Core sunshaft effect parameters
		public const float DEFAULT_INTENSITY = 2.0f;                // Default god ray strength
		public const float DEFAULT_THRESHOLD = 0.5f;                // Default threshold for ray visibility
		public const float DEFAULT_BLUR_RADIUS = 3.0f;              // Default ray blur amount
		public const int DEFAULT_BLUR_ITERATIONS = 3;               // Default blur quality

		// Resolution and scaling constants
		public const float REFERENCE_RESOLUTION = 768f;             // Baseline resolution for blur scaling (tuned for optimal visual quality)
		public const float SHADER_SAMPLE_COUNT = 6f;                // Number of samples in optimized radial blur shader
		public const float BLUR_RESOLUTION_SCALE = 1f / REFERENCE_RESOLUTION; // Base blur scaling factor
		public const float BLUR_SCALE_FACTOR = SHADER_SAMPLE_COUNT / REFERENCE_RESOLUTION; // Combined blur scaling factor

		// Shader pass indices
		public const int SHADER_PASS_SCREEN_BLEND = 0;              // Pass 0: Screen blend for final composite
		public const int SHADER_PASS_RADIAL_BLUR = 1;               // Pass 1: Radial blur iterations
		public const int SHADER_PASS_BRIGHT_PASS = 2;               // Pass 2: Bright pass with depth texture

		// Blur iteration limits
		public const int MIN_BLUR_ITERATIONS = 1;                   // Minimum blur iterations allowed
		public const int MAX_BLUR_ITERATIONS = 4;                   // Maximum blur iterations allowed

		// RenderTexture configuration constants
		public const int RENDERTEXTURE_DEPTH_BUFFER = 0;            // No depth buffer for RenderTextures
		public const int INVALID_TEXTURE_SIZE = -1;                 // Invalid texture size marker
	}
}
