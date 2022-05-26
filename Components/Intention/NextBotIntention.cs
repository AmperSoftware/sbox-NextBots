using System;

namespace Amper.NextBot;

public interface INextBotIntention { }

public class NextBotIntention<Actor, InitialAction> : NextBotComponent, INextBotIntention where Actor : INextBot where InitialAction : NextBotAction<Actor>, new()
{
	NextBotBehavior<Actor> Behavior { get; set; }
	Actor Me { get; set; }

	public NextBotIntention( INextBot bot ) : base( bot )
	{
		Me = (Actor)bot;
		Reset();
	}

	public override void Update()
	{
		Behavior.Update( Me, CurrentInterval );
	}

	public override void Reset()
	{
		Behavior?.Stop();
		Behavior = new NextBotBehavior<Actor>( new InitialAction() );
	}

	public override void OnEvent( NextBotEvent args )
	{
		base.OnEvent( args );
		Behavior?.OnEvent( args );
	}

}
