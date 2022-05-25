using Sandbox;

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
			Body = new NextBotPlayerBody( bot )
		};
	}

	public override void BuildInput( InputBuilder builder )
	{
		if ( Bot != null && Bot.NextBot != null && Bot.NextBot.IsValid )
			Bot.NextBot.BuildInput( builder );

		base.BuildInput( builder );
	}
}
