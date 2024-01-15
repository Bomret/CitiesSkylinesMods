﻿using System;

namespace OptionsFramework.Attibutes
{
	[AttributeUsage(AttributeTargets.Property)]
	public sealed class TextfieldAttribute : AbstractOptionsAttribute
	{
		public TextfieldAttribute(string description, string group = null, Type actionClass = null,
			string actionMethod = null) : base(description, group, actionClass, actionMethod)
		{
		}
	}
}