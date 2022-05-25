using System;

namespace Amper.NextBot;

public interface INextBotIntention { }

public class NextBotIntention<T, U> : NextBotComponent, INextBotIntention where T : INextBot where U : NextBotAction<T>, new()
{
	NextBotBehavior<T> Behavior { get; set; }
	T Me { get; set; }

	public NextBotIntention( INextBot bot ) : base( bot )
	{
		Me = (T)bot;
		Reset();
	}

	public override void Update()
	{
		Behavior.Update( Me, CurrentInterval );
	}

	public override void Reset()
	{
		Behavior?.Stop();
		Behavior = new NextBotBehavior<T>( new U() );
	}

	public override void OnEvent( NextBotEvent args )
	{
		base.OnEvent( args );
		Behavior?.OnEvent( args );
	}

}
