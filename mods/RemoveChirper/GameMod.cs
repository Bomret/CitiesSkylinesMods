using System;
using ColossalFramework;
using ColossalFramework.Plugins;
using ICities;
using UnityEngine;

namespace Bomret.RemoveChirper
{
	public sealed class GameMod : ChirperExtensionBase, IUserMod
	{
		public string Name { get { return "Remove Chirper"; } }
		public string Description { get { return "Completely removes Chirper from the game.\nby Bomret"; } }

		public override void OnCreated(IChirper c)
		{
			if (c is null) throw new ArgumentNullException(nameof(c));

			base.OnCreated(c);

			Debug.Log("[RemoveChirper] Removing Chirper");

			try
			{
				c.DestroyBuiltinChirper();
			}
			catch (Exception err)
			{
				Debug.Log("[RemoveChirper] Error removing Chirper");
				Debug.LogException(err);
			}
		}
	}
}