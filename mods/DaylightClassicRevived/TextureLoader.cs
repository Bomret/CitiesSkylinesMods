using System.Reflection;
using ColossalFramework;
using UnityEngine;

namespace DaylightClassicRevived
{
	public static class TextureLoader
	{
		public static Texture3DWrapper LoadTextureFromEmbeddedResource(string path, string name)
		{
			var tex1 = LoadTextureFromAssembly(path, false);
			tex1.name = name;

			var wrapper = ScriptableObject.CreateInstance<Texture3DWrapper>();
			wrapper.name = name;
			wrapper.texture = Texture3DWrapper.Convert(tex1);

			return wrapper;
		}

		static Texture2D LoadTextureFromAssembly(string path, bool readOnly = true)
		{
			var assembly = Assembly.GetExecutingAssembly();

			using (var textureStream = assembly.GetManifestResourceStream(path))
			{
				var buf = new byte[textureStream.Length];  //declare arraysize
				textureStream.Read(buf, 0, buf.Length); // read from stream to byte array

				var tex = new Texture2D(2, 2, TextureFormat.ARGB32, false);
				tex.LoadImage(buf);
				tex.Apply(false, readOnly);

				return tex;
			}
		}


	}
}