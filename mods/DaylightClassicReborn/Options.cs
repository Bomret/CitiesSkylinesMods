using System.Xml.Serialization;
using OptionsFramework.Attibutes;

namespace DaylightClassicReborn
{
	[XmlOptions("CSL-DaylightClassic", "CSL-DaylightClassic")]
	public sealed class Options
	{
		const string BASIC = "DC_OPTIONS_BASIC";
		const string ADVANCED = "DC_OPTIONS_ADVANCED";
		const string SHORTCUTS = "DC_ACTIONS_SHORTCUTS";

		[XmlElement("stockLuts")]
		[Checkbox("DC_OPTION_LUTS", BASIC, typeof(DaylightClassic), nameof(DaylightClassic.ReplaceLuts))]
		public bool StockLuts { set; get; } = true;

		[XmlElement("sunlightColor")]
		[Checkbox("DC_OPTION_SUN_COLOR", BASIC, typeof(DaylightClassic), nameof(DaylightClassic.ReplaceSunlightColor))]
		public bool SunlightColor { set; get; } = true;

		[XmlElement("sunlightIntensity")]
		[Checkbox("DC_OPTION_SUN_INTENSITY", BASIC, typeof(DaylightClassic), nameof(DaylightClassic.ReplaceSunlightIntensity))]
		public bool SunlightIntensity { set; get; } = true;

		[XmlElement("sunPosition")]
		[Checkbox("DC_OPTION_SUN_POSITION", BASIC, typeof(DaylightClassic), nameof(DaylightClassic.ReplaceLatLong))]
		public bool SunPosition { set; get; } = true;

		[XmlElement("fogEffect")]
		[Checkbox("DC_OPTION_FOG_EFFECT", ADVANCED, typeof(DaylightClassic), nameof(DaylightClassic.ReplaceFogEffect))]
		public bool FogEffect { set; get; } = true;

		[XmlElement("softerShadows")]
		[Checkbox("DC_OPTION_SOFTERSHADOWS", ADVANCED, typeof(DaylightClassic))]
		public bool SofterShadows { set; get; } = true;

		[XmlElement("fogColor")]
		[Checkbox("DC_OPTION_FOG_COLOR", ADVANCED, typeof(DaylightClassic))]
		public bool FogColor { set; get; } = true;

		[XmlElement("allowClassicFogEffectIfDayNightIsOn")]
		[Checkbox("DC_OPTION_CLASSIC_EFFECT_IF_CYCLE_ENABLED", ADVANCED, typeof(DaylightClassic))]
		public bool AllowClassicFogEffectIfDayNightIsOn { set; get; } = true;

		[XmlIgnore]
		[Button("DC_ACTION_TO_CLASSIC", SHORTCUTS, typeof(GameMod), nameof(GameMod.AllToClassic))]
		public object AllToClassicButton { set; get; } = null;

		[XmlIgnore]
		[Button("DC_ACTION_TO_AD", SHORTCUTS, typeof(GameMod), nameof(GameMod.AllToAfterDark))]
		public object AllToAffterDarkButton { set; get; } = null;
	}
}