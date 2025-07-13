using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using ColossalFramework;
using ColossalFramework.Importers;
using UnityEngine;

namespace Common
{
	public interface ILutProvider
	{
		Texture3D GetLut(string name);
	}

	public sealed class LutProvider : ILutProvider
	{
		readonly DirectoryInfo _lutsDir;
		readonly Dictionary<string, Texture3D> _luts = new Dictionary<string, Texture3D>(StringComparer.OrdinalIgnoreCase);

		public LutProvider(ModData mod)
		{
			_lutsDir = mod.Directory.CreateSubdirectory("Assets").CreateSubdirectory("Luts");
		}

		public Texture3D GetLut(string name)
		{
			if (_luts.TryGetValue(name, out var lut))
			{
				return lut;
			}

			lut = LoadLut(name);

			_luts.Add(name, lut);

			return lut;
		}

		Texture3D LoadLut(string name)
		{
			var files = _lutsDir.GetFiles();
			var lutFile = files.SingleOrDefault(f => f.Name.Equals($"{name}.png", StringComparison.OrdinalIgnoreCase));
			if (lutFile is null)
			{
				return null;
			}

			var image = new Image();
			image.LoadFromFile(lutFile.FullName, Image.SupportedFileFormat.PNG, 0u);
			var lut = Texture3DWrapper.Convert(image);

			return lut;
		}
	}
}
