using System;
using System.IO;

namespace NaturalLighting
{
	readonly struct ModInfo
	{
		public string NameOrSteamId { get; }
		public DirectoryInfo Directory { get; }

		public bool IsEnabled => _isEnabled();

		readonly Func<bool> _isEnabled;

		public ModInfo(
			string nameOrSteamId,
			DirectoryInfo directory,
			Func<bool> isEnabled) : this()
		{
			NameOrSteamId = nameOrSteamId;
			Directory = directory;
			_isEnabled = isEnabled;
		}
	}
}
