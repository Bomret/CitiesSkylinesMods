using System;

namespace OptionsFramework.Attibutes
{
	[AttributeUsage(AttributeTargets.Property)]
	public sealed class SliderAttribute : AbstractOptionsAttribute
	{
		public float Min { get; }
		public float Max { get; }
		public float Step { get; }

		public SliderAttribute(string description, float min, float max, float step, string group = null, Type actionClass = null, string actionMethod = null) : base(description, group, actionClass, actionMethod)
		{
			Min = min;
			Max = max;
			Step = step;
		}
	}
}
