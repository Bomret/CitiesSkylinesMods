using System;

namespace OptionsFramework.Attibutes
{
	[AttributeUsage(AttributeTargets.Property)]
	public sealed class ButtonAttribute : AbstractOptionsAttribute
	{
		public ButtonAttribute(string description, string group, Type actionClass = null, string actionMethod = null) :
			base(description, group, actionClass, actionMethod)
		{

		}
	}
}