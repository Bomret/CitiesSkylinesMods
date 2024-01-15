using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace OptionsFramework.Attibutes
{
	[AttributeUsage(AttributeTargets.Property)]
	public sealed class DynamicDropDownAttribute : DropDownAttribute
	{
		readonly Func<DropDownEntry<string>[]> _populator;

		public DynamicDropDownAttribute(string description, Type populatorClass, string populatorMethod, string group = null, Type actionClass = null, string actionMethod = null) :
			base(description, group, actionClass, actionMethod)
		{
			_populator = () =>
			{
				var method = populatorClass.GetMethod(populatorMethod, BindingFlags.Public | BindingFlags.Static);
				return (DropDownEntry<string>[])method.Invoke(null, new object[] { });
			};
		}

		public IList<DropDownEntry<string>> GetItems(Func<string, string> translator = null)
		{
			var entries = _populator.Invoke();
			return (from DropDownEntry<string> entry in entries
					let code = entry.Code
					let description = entry.Description
					let translatedDesctiption = translator == null ? description : translator.Invoke(description)
					select new DropDownEntry<string>(code, translatedDesctiption)).ToArray();
		}
	}
}