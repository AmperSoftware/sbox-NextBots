using Sandbox;
using System.Collections.Generic;

namespace Amper.NextBot;

public partial class NextBots
{
	// This really should be 0.1f by default, otherwise too many agents will cause sever performance issues.
	// But to achieve that we need to somehow interpolate our bots' movement. 
	// When we achieve that, put this back to 0.1f.
	[ConVar.Replicated] public static float nb_update_frequency { get; set; } = 0.1f;
	public const string CollisionTag = "nextbot";

	public static NextBots Current => _current == null ? (_current = new NextBots()) : _current;
	static NextBots _current;

	public NextBots()
	{
		Event.Register( this );
	}

	/// <summary>
	/// All currenly active nextbots.
	/// </summary>
	public List<NextBotController> Bots = new();
	public int LastBotId = 0;

	/// <summary>
	/// Registers the bot in the manager.
	/// </summary>
	public static void Register( NextBotController nextBot )
	{
		if ( !Host.IsServer )
			return;

		// If bot is already registered, early out.
		if ( Current.IsRegistered( nextBot ) )
			return;

		Current.Bots.Add( nextBot );
	}

	public bool IsRegistered( NextBotController nextBot ) => Bots.Contains( nextBot );

	[Event.Tick.Server]
	public void Tick()
	{
		PurgeDeletedBots();

		// update all bots
		foreach ( var bot in Bots )
		{
			bot.Upkeep();
			bot.Update();
		}
	}

	void PurgeDeletedBots()
	{
		for ( var i = (Bots.Count - 1); i >= 0; i-- )
		{
			var bot = Bots[i];
			if ( !bot.IsValid() ) 
			{
				if ( IsDebugging() )
					Log.Info( $"[NextBot] Purged Bot {bot.Bot} (#{i})" );

				Bots.Remove( bot );
			}
		}
	}
}
