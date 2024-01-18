using System;

namespace OptionsFramework.Attributes
{
	[AttributeUsage(AttributeTargets.Property)]
	public sealed class ButtonAttribute : AbstractOptionsAttribute
	{
		public ButtonAttribute(string description, string group) : base(description, group)
		{

		}
	}
}