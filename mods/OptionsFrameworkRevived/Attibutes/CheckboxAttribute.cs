using System;

namespace OptionsFramework.Attributes
{
	[AttributeUsage(AttributeTargets.Property)]
	public sealed class CheckboxAttribute : AbstractOptionsAttribute
	{
		public CheckboxAttribute(string description, string group = null) :
			base(description, group)
		{
		}
	}
}