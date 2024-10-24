using System;
using System.Reflection;
using ICities;
using UnityEngine;

namespace RemoveChirper
{
	public sealed class GameMod : ChirperExtensionBase, IUserMod
	{
		public string Name => $"{_modName} {_version}";
		public string Description { get { return "Completely removes Chirper from the game.\nby Bomret"; } }

		readonly string _modName = "Remove Chirper";
		readonly string _version = Assembly.GetExecutingAssembly().GetName().Version.ToString(3);

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