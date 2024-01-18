#pragma warning disable CA1819 // Properties should not return arrays

using System.Xml.Serialization;

namespace NaturalLighting
{
	public sealed class Language
	{
		[XmlAttribute]
		public string Tag { get; set; }

		[XmlAttribute]
		public string Name { get; set; }

		public Translation[] Translations { get; set; }
	}

	public sealed class Translation
	{
		[XmlAttribute]
		public string ID { get; set; }

		[XmlAttribute]
		public string Value { get; set; }
	}
}