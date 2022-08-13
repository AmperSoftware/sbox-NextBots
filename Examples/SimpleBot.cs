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
		Tags.Add( "solid" );

		SetupPhysicsFromAABB( PhysicsMotionType.Keyframed, new Vector3( -16, -16, 0 ), new Vector3( 16, 16, 72 ) );

		NextBot = new( this )
		{
			Vision = new NextBotVision( this ),
			Animator = new NextBotAnimator( this ),
			Locomotion = new NextBotGroundLocomotion( this )
			{
				DesiredSpeed = 120,
				RunSpeed = 200
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
	CountdownTimer repathTimer = new();
	NextBotPathFollower Path = new();

	public override ActionResult<SimpleBot> Update( SimpleBot me, float interval )
	{
		var target = me.NextBot.Vision.KnownEntities.FirstOrDefault();
		if ( target == null )
			return Continue();

		if ( me.NextBot.IsRangeGreaterThan( target.Entity, 100 ) )
		{
			if ( repathTimer.IsElapsed() )
			{
				Path.Build( me, target.LastKnownPosition );
				repathTimer.Start( Rand.Float( .2f, .5f ) );
			}

			Path.Update( me );
			if ( me.NextBot.IsRangeGreaterThan( target.Entity, 200 ) )
			{
				me.NextBot.Locomotion.Run();
			}
			else
			{
				me.NextBot.Locomotion.Walk();
			}
		}

		me.NextBot.Locomotion.AimHeadTowards( target.Entity.EyePosition, LookAtPriorityType.Important, 1, "Look at Target" );

		return Continue();
	}
}
