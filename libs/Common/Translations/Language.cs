#pragma warning disable CA1819 // Properties should not return arrays

using System.Xml.Serialization;

namespace Common.Translations
{
	public sealed class Language
	{
		[XmlAttribute]
		public string Tag { get; set; }

		[XmlAttribute]
		public string Name { get; set; }

		public Translation[] Translations { get; set; }
	}
}