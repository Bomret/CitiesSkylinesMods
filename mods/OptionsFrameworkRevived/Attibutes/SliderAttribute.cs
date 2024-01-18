using System;

namespace OptionsFramework.Attributes
{
	[AttributeUsage(AttributeTargets.Property)]
	public sealed class SliderAttribute : AbstractOptionsAttribute
	{
		public float Min { get; }
		public float Max { get; }
		public float Step { get; }

		public SliderAttribute(string description, float min, float max, float step, string group = null) : base(description, group)
		{
			Min = min;
			Max = max;
			Step = step;
		}
	}
}
