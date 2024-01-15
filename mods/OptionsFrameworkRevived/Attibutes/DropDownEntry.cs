namespace OptionsFramework.Attibutes
{
	public readonly struct DropDownEntry<TKey>
	{
		public TKey Code { get; }
		public string Description { get; }

		public DropDownEntry(TKey code, string description)
		{
			Code = code;
			Description = description;
		}
	}
}