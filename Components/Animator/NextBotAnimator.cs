using Sandbox;
using System;

namespace Amper.NextBot;

public interface INextBotAnimator { }

public class NextBotAnimator : NextBotComponent, INextBotAnimator
{
	AnimatedEntity Entity { get; set; }

	public NextBotAnimator( INextBot me ) : base( me ) 
	{
		Entity = me as AnimatedEntity;
		if ( Entity == null )
		{
			Log.Error( "NextBotAnimator only works on entities that extend AnimatedEntity" );
			return;
		}
	}

	public override void Update()
	{
		// Set grounded
		Entity.SetAnimParameter( "b_grounded", Bot.NextBot.Locomotion.IsOnGround() );

		var viewVector = Entity.EyeRotation.Forward;

		// Update model's rotation.
		UpdateRotation( viewVector );

		// Update model's look direction.
		UpdateLookAt( viewVector );

		var velocity = Entity.Velocity;

		UpdateMovement(  velocity );
	}

	public virtual void UpdateRotation( Vector3 viewVector )
	{
		if ( viewVector.IsNearlyZero() )
			return;

		Entity.Rotation = Rotation.LookAt( viewVector.WithZ( 0 ).Normal, Vector3.Up );
	}

	public virtual void UpdateLookAt( Vector3 viewVector )
	{
		var aimPos = Entity.EyePosition + viewVector * 200;

		Entity.SetAnimLookAt( "aim_eyes", aimPos );
		Entity.SetAnimLookAt( "aim_head", aimPos );
		Entity.SetAnimLookAt( "aim_body", aimPos );
	}

	public virtual void UpdateMovement( Vector3 velocity )
	{
		var forward = Entity.EyeRotation.Forward;
		var right = Entity.EyeRotation.Right;

		var forwardSpeed = forward.Dot( velocity );
		var rightSpeed = right.Dot( velocity );

		Entity.SetAnimParameter( "move_speed", velocity.Length );
		Entity.SetAnimParameter( "move_groundspeed", velocity.WithZ( 0 ).Length );

		Entity.SetAnimParameter( "move_x", forwardSpeed );
		Entity.SetAnimParameter( "move_y", rightSpeed );
		Entity.SetAnimParameter( "move_z", velocity.z );
	}
}
