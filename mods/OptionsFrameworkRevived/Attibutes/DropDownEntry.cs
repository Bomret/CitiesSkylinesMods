namespace OptionsFramework.Attributes
{
	public sealed class DropDownEntry
	{
		public string Code { get; }
		public string Description { get; }

		public DropDownEntry(string code, string description)
		{
			Code = code;
			Description = description;
		}
	}
}