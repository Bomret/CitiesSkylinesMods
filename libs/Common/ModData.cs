using System;
using System.IO;

namespace Common
{
	public readonly struct ModData
	{
		public string NameOrSteamId { get; }
		public DirectoryInfo Directory { get; }

		public bool IsEnabled => _isEnabled();

		readonly Func<bool> _isEnabled;

		public ModData(
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
