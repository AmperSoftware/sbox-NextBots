using Sandbox;
using System.Collections.Generic;

namespace Amper.NextBot;

public partial class NextBots
{
	[ConVar.Replicated] public static float nb_update_frequency { get; set; } = 0.1f;

	public static NextBots Current => _current == null ? (_current = new NextBots()) : _current;
	static NextBots _current;

	public NextBots()
	{
		if ( IsDebugging() )
			Log.Info( "NextBots Manager Created!" );

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
			bot.Update();
	}

	void PurgeDeletedBots()
	{
		for ( var i = (Bots.Count - 1); i >= 0; i-- )
		{
			var bot = Bots[i];
			if ( bot == null || !bot.IsValid )
			{
				if ( IsDebugging() )
					Log.Info( $"[NextBot] Purged Bot {bot.Bot} (#{i})" );

				Bots.Remove( bot );
			}
		}
	}
}
