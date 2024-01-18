using System;

namespace OptionsFramework.Attributes
{
	[AttributeUsage(AttributeTargets.Property)]
	public sealed class TextfieldAttribute : AbstractOptionsAttribute
	{
		public TextfieldAttribute(string description, string group = null) : base(description, group)
		{
		}
	}
}