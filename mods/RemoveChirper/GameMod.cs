using System;
using ICities;

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

			c.DestroyBuiltinChirper();
		}
	}
}