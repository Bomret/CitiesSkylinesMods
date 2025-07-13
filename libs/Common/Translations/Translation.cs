#pragma warning disable CA1819 // Properties should not return arrays

using System.Xml.Serialization;

namespace Common.Translations
{
    public sealed class Translation
    {
        [XmlAttribute]
        public string ID { get; set; }

        [XmlAttribute]
        public string Value { get; set; }
    }
}