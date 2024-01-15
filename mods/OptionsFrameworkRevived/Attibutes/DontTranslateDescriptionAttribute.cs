using System;
using System.ComponentModel;

namespace OptionsFramework.Attibutes
{
	[AttributeUsage(AttributeTargets.All)]
	public sealed class DontTranslateDescriptionAttribute : DescriptionAttribute
	{
		public DontTranslateDescriptionAttribute(string description) :
			base(description)
		{

		}
	}
}