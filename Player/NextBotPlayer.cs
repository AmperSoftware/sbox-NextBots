using Sandbox;
using System.Linq;
using System.Collections.Generic;

namespace Amper.NextBot;

public abstract partial class NextBotPlayer : Bot
{
	public INextBot Bot { get; set; }

	public NextBotPlayer() : this( null ) { }
	public NextBotPlayer( string name ) : base( name )
	{
		Bot = Client.Pawn as INextBot;
		if ( Bot == null )
		{
			Log.Error( "Tried to create a NextBot on a pawn that doesn't support it." );
			return;
		}

		SetupNextBot( Bot );
	}

	public virtual void SetupNextBot( INextBot bot )
	{
		bot.NextBot = new( bot )
		{
			Locomotion = new NextBotPlayerLocomotion( bot ),
			Vision = new NextBotVision( bot ),
			Animator = new NextBotAnimator( bot )
		};
	}

	public override void BuildInput( InputBuilder builder )
	{
		if ( Bot != null && Bot.NextBot != null && Bot.NextBot.IsValid )
			Bot.NextBot.BuildInput( builder );

		base.BuildInput( builder );
	}

	[ConCmd.Admin( "bot_teleport" )]
	public static void Command_BotTeleport( string who )
	{
		if ( ConsoleSystem.Caller.Pawn is Entity player )
		{
			var all = Client.All.Where( x => x.IsBot ).Select( x => x.Pawn );
			IEnumerable<Entity> targets = null;

			if ( who == "all" ) targets = all;

			var tr = Trace.Ray( player.EyePosition, player.EyePosition + player.EyeRotation.Forward * 1000 )
				.Radius( 2 )
				.WithoutTags( "player" )
				.Run();

			if ( tr.Hit && targets != null )
			{
				foreach ( var bot in targets )
				{
					bot.Position = tr.EndPosition;
				}
			}
		}
	}
}
