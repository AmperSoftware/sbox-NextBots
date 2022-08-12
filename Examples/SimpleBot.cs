using Sandbox;
using System.Linq;

namespace Amper.NextBot;

[Library( "npc_simplebot" )]
public partial class SimpleBot : AnimatedEntity, INextBot
{
	public NextBotController NextBot { get; set; }

	public override void Spawn()
	{
		base.Spawn();

		SetModel( "models/citizen/citizen.vmdl" );
		EyeLocalPosition = Vector3.Up * 64;
		Health = 100;

		EnableAllCollisions = true;
		EnableHitboxes = true;
		SetupPhysicsFromAABB( PhysicsMotionType.Keyframed, new Vector3( -24, -24, 0 ), new Vector3( 24, 24, 72 ) );

		NextBot = new( this )
		{
			Vision = new NextBotVision( this ),
			Animator = new NextBotAnimator( this ),
			Locomotion = new NextBotGroundLocomotion( this )
			{
				DesiredSpeed = 240
			},
			Intention = new NextBotIntention<SimpleBot, SimpleBotBehavior>( this )
		};
	}

	public override void TakeDamage( DamageInfo info )
	{
		base.TakeDamage( info );
		NextBot?.InvokeEvent( new NextBotEventInjured { DamageInfo = info } );
	}
}

public class SimpleBotBehavior : NextBotAction<SimpleBot>
{
	Entity Target;

	CountdownTimer repathTimer = new();
	NextBotPathFollower Path = new();

	public override ActionResult<SimpleBot> Update( SimpleBot me, float interval )
	{
		// We don't have target, do nothing.
		if ( Target == null )
		{
			// first entity we're aware of we will follow.
			var firstKnown = me.NextBot.Vision.KnownEntities.FirstOrDefault();
			if ( firstKnown != null )
			{
				Target = firstKnown.Entity;
			}

			if ( Target == null )
				return Continue();
		}

		var known = me.NextBot.Vision.GetKnown( Target );
		if ( known == null )
		{
			// Forget about the entity.
			Target = null;
			return Continue();
		}

		if ( me.NextBot.IsRangeGreaterThan( Target, 100 ) )
		{
			if ( repathTimer.IsElapsed() )
			{
				Path.Build( me, known.LastKnownPosition );
				repathTimer.Start( Rand.Float( .2f, .4f ) );
			}

			Path.Update( me );
		}

		return Continue();
	}
}
