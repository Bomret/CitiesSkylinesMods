using System;

namespace OptionsFramework.Attributes
{
	[AttributeUsage(AttributeTargets.Property)]
	public abstract class DropDownAttribute : AbstractOptionsAttribute
	{
		protected DropDownAttribute(string description, string group) : base(
			description, group)
		{
		}
	}
}