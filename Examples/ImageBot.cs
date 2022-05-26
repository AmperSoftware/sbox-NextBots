namespace Amper.NextBot;

#if false
public abstract partial class ImageBot : AnimatedEntity, INextBot
{
	public NextBotController NextBot { get; set; }

	public override void Spawn()
	{
		base.Spawn();

		SetModel( "models/other/nb_citizen.vmdl" );
		EyeLocalPosition = Vector3.Up * 64;

		EnableAllCollisions = true;
		EnableHitboxes = true;
		SetupPhysicsFromModel( PhysicsMotionType.Keyframed );
		SetInteractsAs( CollisionLayer.Debris );
		CollisionGroup = CollisionGroup.Debris;

		NextBot = new( this )
		{
			Vision = new NextBotVision( this )
			{
				FieldOfView = 180,
			},
			Locomotion = new NextBotGroundLocomotion( this )
			{
				DesiredSpeed = 750,
				Acceleration = 3,
				AirAcceleration = 3
			},
			Intention = new NextBotIntention<CitizenBot, CitizenBotBehavior>( this ),
			Animator = new NextBotAnimator( this )
		};
	}

	public override void TakeDamage( DamageInfo info )
	{
		base.TakeDamage( info );
		NextBot?.InvokeEvent( new NextBotEventInjured { DamageInfo = info } );
	}
}

public class CitizenBotBehavior : NextBotAction<CitizenBot>
{
	Entity Target;

	CountdownTimer repathTimer = new();
	NextBotPathFollower Path = new();

	public override ActionResult<CitizenBot> Update( CitizenBot me, float interval )
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


		if ( repathTimer.IsElapsed() )
		{
			Path.Build( me, known.LastKnownPosition );
			repathTimer.Start( Rand.Float( .2f, .4f ) );
		}
		Path.Update( me );

		if ( me.NextBot.IsRangeLessThan( Target, 150 ) )
		{
			var info = DamageInfo.Generic( 9999 )
				.WithAttacker( me )
				.WithPosition( me.Position )
				.WithFlag( DamageFlags.Acid );

			Target.TakeDamage( info );
		}

		me.NextBot.Locomotion.FaceTowards( known.LastKnownPosition + known.Entity.EyeLocalPosition );

		return Continue();
	}

	public override EventDesiredResult<CitizenBot> OnStuck( CitizenBot me, NextBotEventStuck args )
	{
		me.NextBot.Locomotion.Jump();
		return base.OnStuck( me, args );
	}
}

#endif
