﻿using System;
using Sandbox;

namespace Amper.NextBot;

/// <summary>
/// Allows the bot to traverse on the ground.
/// </summary>
public class NextBotGroundLocomotion : NextBotLocomotion
{
	public NextBotGroundLocomotion( INextBot me ) : base( me ) { }

	public float DefaultSpeed = 100;
	public float RunSpeed = 200;

	public float MaxStandableAngle = 45;
	public float Acceleration = 15;
	public float AirAcceleration = 10;
	public float Gravity = 800;
	public float JumpImpulse = 312;

	public override void Reset()
	{
		base.Reset();
		DesiredSpeed = DefaultSpeed;
	}

	public override void ProcessMovement()
	{
		// Calculate final movement direction.
		ApplyAccumulatedApproach();
		AddGravity();

		if ( IsOnGround() )
		{
			Velocity = Velocity.WithZ( 0 );
			WalkMove();
		}
		else
		{
			AirMove();
		}

		UpdateGround();
		AddGravity();

		// If we are on ground, no downward velocity.
		if ( IsOnGround() )
		{
			Velocity = Velocity.WithZ( 0 );
		}

		if ( IsStuck )
		{
			StayOnGround();
		}
	}

	public void AddGravity()
	{
		Velocity += new Vector3( 0, 0, -Gravity *.5f ) * CurrentInterval;
	}

	public virtual void WalkMove()
	{
		Velocity = Velocity.WithZ( 0 );
		Accelerate( MoveVector, DesiredSpeed, Acceleration );
		Velocity = Velocity.WithZ( 0 );

		if ( Velocity.Length < 1 )
		{
			Velocity = 0;
			return;
		}

		Move( CurrentInterval, StepHeight );
		StayOnGround();
	}

	public virtual void AirMove()
	{
		Accelerate( MoveVector, DesiredSpeed, AirAcceleration );
		Move( CurrentInterval );
	}

	public void Accelerate( Vector3 wishDir, float wishSpeed, float acceleration )
	{
		// See if we are changing direction a bit
		var wishVelocity = wishDir * wishSpeed;
		var oldVelocity = Velocity;

		Velocity = Velocity.LerpTo( wishVelocity, CurrentInterval * acceleration );
		Velocity = Velocity.WithZ( oldVelocity.z );
	}

	public virtual void Move( float timeDelta = -1, float stepHeight = 0 )
	{
		if ( timeDelta < 0 )
			timeDelta = Time.Delta;

		var mover = new MoveHelper( Position, Velocity );
		mover.Trace = SetupBBoxTrace( 0, 0 );
		mover.MaxStandableAngle = MaxStandableAngle;

		mover.TryMoveWithStep( timeDelta, StepHeight );

		Position = mover.Position;
		Velocity = mover.Velocity;
	}

	CountdownTimer jumpTimer = new();
	bool IsJumping;

	public override void Jump()
	{
		base.Jump();

		if ( IsClimbingOrJumping() )
			return;

		StayOnGround();
		Bot.Velocity += Vector3.Up * JumpImpulse;
		Bot.GroundEntity = null;

		jumpTimer.Start( 0.5f );
		IsJumping = true;
	}

	public override bool IsClimbingOrJumping()
	{
		if ( !IsJumping )
			return false;

		if ( jumpTimer.IsElapsed() && IsOnGround() )
		{
			IsJumping = false;
			return false;
		}

		return true;
	}

	/// <summary>
	/// Try to keep a walking player on the ground when running down slopes etc
	/// </summary>
	public virtual void StayOnGround()
	{
		var start = Position + Vector3.Up * 2;
		var end = Position + Vector3.Down * StepHeight;

		// See how far up we can go without getting stuck
		var tr = TraceBBox( Position, start );
		start = tr.EndPosition;

		// Now trace down from a known safe position
		tr = TraceBBox( start, end );

		if ( tr.Fraction <= 0 ) return;
		if ( tr.Fraction >= 1 ) return;
		if ( tr.StartedSolid ) return;
		if ( Vector3.GetAngle( Vector3.Up,tr.Normal ) >= MaxStandableAngle ) return;

		Position = tr.EndPosition;
	}

	public void ApplyAccumulatedApproach()
	{
		var curPos = GetFeet();
		var rawPos = curPos;
		var deltaT = CurrentInterval;

		if ( deltaT <= 0 )
			return;

		if ( AccumulatedApproachWeight > 0 )
		{
			var approachDelta = AccumulatedApproachVector / AccumulatedApproachWeight;
			rawPos += approachDelta;

			AccumulatedApproachVector = 0;
			AccumulatedApproachWeight = 0;
		}

		var endPos = rawPos.WithZ( GetFeet().z );

		MoveVector = (endPos - curPos).WithZ( 0 ).Normal;
	}

	public virtual void UpdateGround()
	{
		var point = Position - Vector3.Up * 2;
		var bumpOrigin = Position;

		var tr = TraceBBox( bumpOrigin, point );

		if ( tr.Entity != null && Vector3.GetAngle( Vector3.Up, tr.Normal ) < MaxStandableAngle )
		{
			// Hit something we can stand on.
			UpdateGroundEntity( tr );
		}
		else
		{
			ClearGroundEntity();
		}
	}

	public void ClearGroundEntity()
	{
		if ( GroundEntity == null )
			return;

		GroundEntity = null;
	}

	public virtual void UpdateGroundEntity( TraceResult tr )
	{
		var newGround = tr.Entity;
		var oldGround = GroundEntity;

		if ( oldGround == null && newGround != null )
		{
			Bot.NextBot.InvokeEvent<NextBotEventLandOnGround>();
		}
		else if ( oldGround != null && newGround == null )
		{
			Bot.NextBot.InvokeEvent<NextBotEventLeaveGround>();
		}

		GroundEntity = newGround;
	}

	Vector3 AccumulatedApproachVector { get; set; }
	float AccumulatedApproachWeight { get; set; }
	public override void Approach( Vector3 point, float goalWeight = 1 )
	{
		base.Approach( point );

		var toTarget = point - GetFeet();
		AccumulatedApproachVector += toTarget;
		AccumulatedApproachWeight += goalWeight;
	}

	public override void Run()
	{
		DesiredSpeed = RunSpeed;
	}

	public override void Walk()
	{
		DesiredSpeed = DefaultSpeed;
	}

	public override void Stop()
	{
		DesiredSpeed = 0;
	}
}
