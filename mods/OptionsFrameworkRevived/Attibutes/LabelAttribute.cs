using System;

namespace OptionsFramework.Attributes
{
	[AttributeUsage(AttributeTargets.Property)]
	public sealed class LabelAttribute : AbstractOptionsAttribute
	{
		public LabelAttribute(string description, string group) :
			base(description, group)
		{
		}
	}
}