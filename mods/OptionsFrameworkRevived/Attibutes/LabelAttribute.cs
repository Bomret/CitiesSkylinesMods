using System;

namespace OptionsFramework.Attibutes
{
	[AttributeUsage(AttributeTargets.Property)]
	public sealed class LabelAttribute : AbstractOptionsAttribute
	{
		public LabelAttribute(string description, string group) :
			base(description, group, null, null)
		{
		}
	}
}