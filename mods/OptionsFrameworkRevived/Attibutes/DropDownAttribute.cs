﻿using System;

namespace OptionsFramework.Attibutes
{
	[AttributeUsage(AttributeTargets.Property)]
	public abstract class DropDownAttribute : AbstractOptionsAttribute
	{
		protected DropDownAttribute(string description, string group, Type actionClass, string actionMethod) : base(
			description, group, actionClass, actionMethod)
		{
		}
	}
}