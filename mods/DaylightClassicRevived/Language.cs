using System.Xml.Serialization;

namespace DaylightClassicRevived
{
	[XmlRoot(ElementName = "Language", Namespace = "", IsNullable = false)]
	public sealed class Language
	{
		[XmlAttribute("UniqueName")]
		public string UniqueName { get; set; }

		[XmlAttribute("ReadableName")]
		public string ReadableName { get; set; }

#pragma warning disable CA1819 // Properties should not return arrays
		[XmlArray("Translations", IsNullable = false)]
		[XmlArrayItem("Translation", IsNullable = false)]
		public Translation[] Translations { get; set; }
#pragma warning restore CA1819 // Properties should not return arrays
	}

	public sealed class Translation
	{
		[XmlAttribute("ID")]
		public string Key { get; set; }

		[XmlAttribute("String")]
		public string Value { get; set; }
	}
}