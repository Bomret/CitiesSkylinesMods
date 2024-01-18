using System;

namespace OptionsFramework.Attributes
{
	[AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct)]
	public sealed class ModOptionsAttribute : Attribute
	{
		public string ModName { get; }

		public ModOptionsAttribute(string modName)
		{
			ModName = modName;
		}
	}
}