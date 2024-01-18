using System;

namespace OptionsFramework.Attributes
{
	[AttributeUsage(AttributeTargets.Property)]
	public abstract class AbstractOptionsAttribute : Attribute
	{
		public string Description { get; }
		public string Group { get; }

		protected AbstractOptionsAttribute(string description, string group)
		{
			Description = description;
			Group = group;
		}
	}
}