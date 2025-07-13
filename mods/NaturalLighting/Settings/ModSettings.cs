namespace NaturalLighting.Settings
{
	public sealed class ModSettings
	{
		public bool IgnoreIncompatibleMods { get; set; }
		public bool UseNaturalSunlight { get; set; } = true;
		public bool UseSofterShadowsOnBuildings { get; set; } = true;
		public bool UseOwnLut { get; set; } = true;
		public bool UseSunshafts { get; set; } = true;
		public bool UseChromaticAberration { get; set; }
	}
}