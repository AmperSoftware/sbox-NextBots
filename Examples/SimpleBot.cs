using Sandbox;
using System.Linq;

namespace Amper.NextBot;

[Library( "npc_simplebot" )]
public partial class SimpleBot : AnimatedEntity, INextBot
{
	public NextBotController NextBot { get; set; }
	public Entity Target { get; set; }

	public override void Spawn()
	{
		base.Spawn();

		NextBot = new( this )
		{
			Locomotion = new NextBotLocomotion( this ),
			Vision = new NextBotVision( this ),
			Intention = new NextBotIntention<SimpleBot, SimpleBotIdle>( this )
		};


		SetModel( "models/bots/demo/bot_sentry_buster.vmdl" );
		Scale = 1.5f;
	}
}

public class SimpleBotIdle : NextBotAction<SimpleBot>
{
	public override ActionResult<SimpleBot> OnStart( SimpleBot me, NextBotAction<SimpleBot> priorAction = null )
	{
		return Continue();
	}

	public override ActionResult<SimpleBot> Update( SimpleBot me, float interval )
	{
		var ent = Entity.FindInSphere( me.Position, 500 ).FirstOrDefault( x => x is Player && x.Position.Distance( me.Position ) <= 500 );

		if ( ent != null )
		{
			me.Target = ent;
			return SuspendFor( new SimpleBotLook(), "I am seeing someone!" );
		}

		return Continue();
	}
}

public class SimpleBotLook : NextBotAction<SimpleBot>
{
	public override ActionResult<SimpleBot> OnStart( SimpleBot me, NextBotAction<SimpleBot> priorAction = null )
	{
		return Continue();
	}

	public override ActionResult<SimpleBot> Update( SimpleBot me, float interval )
	{
		if ( me.Target == null )
			return Done( "My target doesn't exist anymore." );

		if ( me.Position.Distance( me.Target.Position ) > 500 )
			return Done( "My target is too far away." );

		return Continue();
	}
}
