using UnityEngine;

namespace NaturalLighting.Features
{
	public class SunShaftsImageEffect : MonoBehaviour
	{
		Light _sunLight;
		Transform _sunTransform;
		Material _sunShaftsMaterial;
		Material _simpleClearMaterial;
		ILogger _logger;

		float _intensity;
		float _threshold;
		float _blurRadius;
		int _blurIterations;

		public void Initialize(Light sunLight, Transform sunTransform, Material sunShaftsMaterial, Material simpleClearMaterial,
			float intensity, float threshold, float blurRadius, int blurIterations, ILogger logger)
		{
			_sunLight = sunLight;
			_sunTransform = sunTransform;
			_sunShaftsMaterial = sunShaftsMaterial;
			_simpleClearMaterial = simpleClearMaterial;
			_intensity = intensity;
			_threshold = threshold;
			_blurRadius = blurRadius;
			_blurIterations = blurIterations;
			_logger = logger;
		}

		void OnRenderImage(RenderTexture source, RenderTexture destination)
		{
			if (_sunShaftsMaterial == null || _sunLight == null)
			{
				Graphics.Blit(source, destination);
				return;
			}

			try
			{
				var camera = GetComponent<Camera>();
				if (camera == null)
				{
					Graphics.Blit(source, destination);
					return;
				}

				// Update sunTransform position every frame relative to camera position
				if (_sunTransform != null)
				{
					_sunTransform.position = camera.transform.position - _sunLight.transform.forward * 2000f;
				}

				// Calculate sun screen position using the updated transform
				Vector3 sunScreenPosition;
				if (_sunTransform != null)
				{
					sunScreenPosition = camera.WorldToViewportPoint(_sunTransform.position);
				}
				else
				{
					// Fallback: calculate directly
					Vector3 sunWorldPosition = camera.transform.position - (_sunLight.transform.forward * 2000f);
					sunScreenPosition = camera.WorldToViewportPoint(sunWorldPosition);
				}

				// Enable depth texture mode
				camera.depthTextureMode |= DepthTextureMode.Depth;

				// Only render effect when sun is in front of camera (z > 0)
				if (sunScreenPosition.z > 0)
				{
					ApplySunShaftsEffect(source, destination, sunScreenPosition);
				}
				else
				{
					// Sun not visible (behind camera), just pass through
					Graphics.Blit(source, destination);
				}
			}
			catch (System.Exception e)
			{
				_logger?.LogFormat(LogType.Error, "[NaturalLighting] SunShaftsImageEffect error: {0}", e.Message);
				Graphics.Blit(source, destination);
			}
		}

		void ApplySunShaftsEffect(RenderTexture source, RenderTexture destination, Vector3 sunScreenPosition)
		{
			// Use quarter resolution for performance (like reference implementation)
			int rtWidth = source.width / 4;
			int rtHeight = source.height / 4;

			// Step 1: Bright pass - extract bright areas using Pass 2 (with depth texture)
			RenderTexture brightPass = RenderTexture.GetTemporary(rtWidth, rtHeight, 0);
			brightPass.filterMode = FilterMode.Bilinear;

			// Set parameters like the reference implementation BEFORE bright pass
			_sunShaftsMaterial.SetVector("_BlurRadius4", new Vector4(1f, 1f, 0f, 0f) * _blurRadius);
			_sunShaftsMaterial.SetVector("_SunPosition", new Vector4(sunScreenPosition.x, sunScreenPosition.y, sunScreenPosition.z, 0.75f)); // Include Z and maxRadius (reference value)
			_sunShaftsMaterial.SetVector("_SunThreshold", new Vector4(_threshold, _threshold, _threshold, _threshold));

			// Use Pass 2 for bright pass (with depth texture)
			Graphics.Blit(source, brightPass, _sunShaftsMaterial, 2);

			// Clear borders to prevent artifacts (critical step from reference)
			if (_simpleClearMaterial != null)
			{
				DrawBorder(brightPass, _simpleClearMaterial);
			}

			// Step 2: Radial blur iterations using Pass 1
			_blurIterations = Mathf.Clamp(_blurIterations, 1, 4);

			// Base blur radius calculation like reference
			float baseBlurRadius = _blurRadius * (1f / 768f);
			_sunShaftsMaterial.SetVector("_BlurRadius4", new Vector4(baseBlurRadius, baseBlurRadius, 0f, 0f));
			_sunShaftsMaterial.SetVector("_SunPosition", new Vector4(sunScreenPosition.x, sunScreenPosition.y, sunScreenPosition.z, 0.75f));

			RenderTexture blurred = brightPass;

			for (int i = 0; i < _blurIterations; i++)
			{
				// First blur pass
				RenderTexture temp1 = RenderTexture.GetTemporary(rtWidth, rtHeight, 0);
				temp1.filterMode = FilterMode.Bilinear;
				Graphics.Blit(blurred, temp1, _sunShaftsMaterial, 1); // Pass 1: Radial blur

				if (blurred != brightPass) RenderTexture.ReleaseTemporary(blurred);

				// Calculate dynamic blur radius like the reference
				float blurRadius1 = _blurRadius * (((i * 2f + 1f) * 6f) / 768f);
				_sunShaftsMaterial.SetVector("_BlurRadius4", new Vector4(blurRadius1, blurRadius1, 0f, 0f));

				// Second blur pass
				blurred = RenderTexture.GetTemporary(rtWidth, rtHeight, 0);
				blurred.filterMode = FilterMode.Bilinear;
				Graphics.Blit(temp1, blurred, _sunShaftsMaterial, 1); // Pass 1: Radial blur

				RenderTexture.ReleaseTemporary(temp1);

				// Update blur radius for next iteration
				float blurRadius2 = _blurRadius * (((i * 2f + 2f) * 6f) / 768f);
				_sunShaftsMaterial.SetVector("_BlurRadius4", new Vector4(blurRadius2, blurRadius2, 0f, 0f));
			}

			// Step 3: Final composite using Pass 0 (Screen blend)
			_sunShaftsMaterial.SetVector("_SunColor",
				new Vector4(_sunLight.color.r, _sunLight.color.g, _sunLight.color.b, _sunLight.color.a) * _intensity);

			// Use _ColorBuffer like the reference implementation
			_sunShaftsMaterial.SetTexture("_ColorBuffer", blurred);

			// Use Pass 0 for Screen blend mode (final composite)
			Graphics.Blit(source, destination, _sunShaftsMaterial, 0);

			// Cleanup
			RenderTexture.ReleaseTemporary(brightPass);
			if (blurred != brightPass) RenderTexture.ReleaseTemporary(blurred);
		}

		static void DrawBorder(RenderTexture dest, Material material)
		{
			RenderTexture.active = dest;
			GL.PushMatrix();
			GL.LoadOrtho();

			for (int pass = 0; pass < material.passCount; pass++)
			{
				material.SetPass(pass);

				GL.Begin(GL.QUADS);

				// Draw border quads to clear edges (prevents artifacts)
				float borderX = 1f / dest.width;
				float borderY = 1f / dest.height;

				// Left border
				GL.TexCoord2(0f, 0f); GL.Vertex3(0f, 0f, 0.1f);
				GL.TexCoord2(1f, 0f); GL.Vertex3(borderX, 0f, 0.1f);
				GL.TexCoord2(1f, 1f); GL.Vertex3(borderX, 1f, 0.1f);
				GL.TexCoord2(0f, 1f); GL.Vertex3(0f, 1f, 0.1f);

				// Right border
				GL.TexCoord2(0f, 0f); GL.Vertex3(1f - borderX, 0f, 0.1f);
				GL.TexCoord2(1f, 0f); GL.Vertex3(1f, 0f, 0.1f);
				GL.TexCoord2(1f, 1f); GL.Vertex3(1f, 1f, 0.1f);
				GL.TexCoord2(0f, 1f); GL.Vertex3(1f - borderX, 1f, 0.1f);

				// Top border
				GL.TexCoord2(0f, 0f); GL.Vertex3(0f, 1f - borderY, 0.1f);
				GL.TexCoord2(1f, 0f); GL.Vertex3(1f, 1f - borderY, 0.1f);
				GL.TexCoord2(1f, 1f); GL.Vertex3(1f, 1f, 0.1f);
				GL.TexCoord2(0f, 1f); GL.Vertex3(0f, 1f, 0.1f);

				// Bottom border
				GL.TexCoord2(0f, 0f); GL.Vertex3(0f, 0f, 0.1f);
				GL.TexCoord2(1f, 0f); GL.Vertex3(1f, 0f, 0.1f);
				GL.TexCoord2(1f, 1f); GL.Vertex3(1f, borderY, 0.1f);
				GL.TexCoord2(0f, 1f); GL.Vertex3(0f, borderY, 0.1f);

				GL.End();
			}

			GL.PopMatrix();
		}
	}
}
