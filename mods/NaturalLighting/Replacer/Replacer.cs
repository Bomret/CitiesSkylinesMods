using OptionsFramework;
using UnityEngine;

namespace NaturalLighting.Replacer
{
	abstract class Replacer : MonoBehaviour
	{
		protected IOptionsStore<Options> OptionsStore { get; }

		protected Replacer()
		{
			OptionsStore = XmlOptionsStoreProvider.Instance.GetOrCreate<Options>();
		}
	}
}