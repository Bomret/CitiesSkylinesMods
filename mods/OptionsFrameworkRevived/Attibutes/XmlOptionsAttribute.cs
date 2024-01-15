using System;

namespace OptionsFramework.Attibutes
{
	[AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct)]
	public sealed class XmlOptionsAttribute : Attribute
	{
		//file name in local app data
		public string FileName { get; }

		//file name in Cities: Skylines folder
		public string LegacyFileName { get; }

		public XmlOptionsAttribute(string fileName, string legacyFileName = "")
		{
			FileName = fileName;
			LegacyFileName = legacyFileName;
		}
	}
}