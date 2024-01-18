using System.Xml.Serialization;
using OptionsFramework.Attributes;

namespace NaturalLighting
{
	[ModOptions(modName: "NaturalLighting")]
	public sealed class Options
	{
		const string SUNLIGHT_OPTIONS = "NL_SUNLIGHT_OPTIONS";
		const string SHADOW_OPTIONS = "NL_SHADOW_OPTIONS";

		[XmlElement("useNaturalSunlightColor")]
		[Checkbox("NL_USE_NATURAL_SUNLIGHT", SUNLIGHT_OPTIONS)]
		public bool UseNaturalSunlight { set; get; } = true;

		[XmlElement("useSofterShadows")]
		[Checkbox("NL_USE_SOFTER_SHADOWS", SHADOW_OPTIONS)]
		public bool UseSofterShadows { set; get; } = true;
	}
}