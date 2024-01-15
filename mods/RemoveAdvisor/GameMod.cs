using System;
using ColossalFramework.UI;
using ICities;
using UnityEngine;

namespace Bomret.RemoveAdvisor
{
	public sealed class GameMod : LoadingExtensionBase, IUserMod
	{
		public string Name { get { return "Remove Advisor"; } }
		public string Description { get { return "Completely removes the in-game Advisor from the game.\nby Bomret"; } }

		public override void OnLevelLoaded(LoadMode mode)
		{
			base.OnLevelLoaded(mode);

			try
			{
				RemoveAdvisor();
			}
			catch (Exception err)
			{
				Debug.Log("[RemoveAdvisor] Error removing Advisor");
				Debug.LogException(err);
			}
		}

		static void RemoveAdvisor()
		{
			var advisorButtonObject = GameObject.Find("AdvisorButton");
			if (advisorButtonObject != null)
			{
				Debug.Log("[RemoveAdvisor] Removing Advisor button");
				var advisorButtonComponent = advisorButtonObject.GetComponent<UIComponent>();
				UnityEngine.Object.Destroy(advisorButtonComponent.gameObject);
			}
			else
			{
				Debug.Log("[RemoveAdvisor] Could not find Advisor button");
			}

			// FIXME: The TutorialAdvisorPanel is used for ALL info views -.-, so removing it botches the game. Need to find a different way.
			if (TutorialAdvisorPanel.instance != null)
			{
				Debug.Log("[RemoveAdvisor] Removing Advisor panel");
				UnityEngine.Object.Destroy(TutorialAdvisorPanel.instance.gameObject);
			}
			else
			{
				Debug.Log("[RemoveAdvisor] Could not find Advisor panel");
			}
		}
	}
}