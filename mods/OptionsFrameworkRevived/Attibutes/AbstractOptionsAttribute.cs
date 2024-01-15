using System;
using System.Reflection;

namespace OptionsFramework.Attibutes
{
	[AttributeUsage(AttributeTargets.Property)]
	public abstract class AbstractOptionsAttribute : Attribute
	{
		public string Description { get; }
		public string Group { get; }

		readonly Type _actionClass;
		readonly string _actionMethod;

		static readonly object[] _emptyParameters = new object[] { };

		protected AbstractOptionsAttribute(string description, string group, Type actionClass, string actionMethod)
		{
			Description = description;
			Group = group;
			_actionClass = actionClass;
			_actionMethod = actionMethod;
		}

		public Action<T> Action<T>()
		{
			if (_actionClass == null || _actionMethod == null)
			{
				return s => { };
			}

			var method = _actionClass.GetMethod(_actionMethod, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static);
			if (method == null)
			{
				return s => { };
			}

			return s =>
			{
				method.Invoke(null, new object[] { s });
			};
		}

		public Action Action()
		{
			if (_actionClass == null || _actionMethod == null)
			{
				return () => { };
			}

			var method = _actionClass.GetMethod(_actionMethod, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static);
			if (method == null)
			{
				return () => { };
			}

			return () =>
			{
				method.Invoke(null, _emptyParameters);
			};
		}
	}
}