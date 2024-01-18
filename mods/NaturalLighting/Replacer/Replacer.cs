using NaturalLighting.Settings;
using UnityEngine;

namespace NaturalLighting.Replacer
{
	abstract class Replacer : MonoBehaviour
	{
		protected ModSettingsStore ModSettingsStore { get; }

		protected Replacer()
		{
			ModSettingsStore = ModSettingsStore.GetOrCreate();
		}
	}
}