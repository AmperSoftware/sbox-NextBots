using Sandbox;
using System;

namespace Amper.NextBot;

partial class NextBotLocomotion
{
	Vector3 LookAtPosition;
	Entity LookAtSubject;
	Vector3 LookAtVelocity;

	public LookAtPriorityType LookAtPriority;
	public CountdownTimer LookAtExpireTimer = new();    // How long until this look at expires
	public CountdownTimer LookAtTrackingTimer = new();

	public IntervalTimer LookAtDurationTimer = new();	// How long we have been looking at something
	public IntervalTimer HeadSteadyTimer = new();       // How long we have kept our view steady at the object

	public float AimRate;
	public Rotation LastEyeRotation;

	public bool IsSightedIn;

	public virtual float GetHeadAimSteadyMaxRate() => nb_head_aim_steady_max_rate;

	public virtual void UpkeepAim()
	{
		var deltaT = Time.Delta;
		if ( deltaT < 0.00001f )
			return;

		UpkeepAimSteady();

		// if our current look-at has expired, don't change our aim further
		if ( IsSightedIn && LookAtExpireTimer.IsElapsed() )
			return;
		
		// If we have a subject, update look at point.
		if ( LookAtSubject.IsValid() )
		{
			UpdateLookAtPositionFromSubject( LookAtSubject );
		}

		MoveAimTowardsTarget();
		CheckSightedOnTarget();
	}

	public const float OnTargetTolerance = 0.98f;

	[ConVar.Server] public static float nb_test { get; set; } = 0;

	public virtual void MoveAimTowardsTarget()
	{
		var forward = Bot.EyeRotation.Forward;
		var toTarget = LookAtPosition - Bot.EyePosition;
		toTarget = toTarget.Normal;
		var dot = forward.Dot( toTarget );

		// Get angles to move between
		var currentAngles = forward.EulerAngles;
		var desiredAngles = toTarget.EulerAngles;

		// rotate view at a rate proportional to how far we have to turn
		// max rate if we need to turn around
		// want first derivative continuity of rate as our aim hits to avoid pop
		var approachRate = GetHeadAimApproachRate();

		// Ease out approach rate if we're close to our destination.
		float easeOut = 0.7f;
		if ( dot > easeOut )
		{
			var t = dot.RemapVal( easeOut, 1, 1, .02f );
			approachRate *= MathF.Sin( 1.57f * t );
		}

		// Ease in when we start looking at.
		var easeInTime = 0.25f;
		if ( LookAtDurationTimer.GetElapsedTime() < easeInTime )
			approachRate *= LookAtDurationTimer.GetElapsedTime() / easeInTime;

		// Approach yaw and roll rotations.
		var yaw = ApproachAngle( desiredAngles.yaw, currentAngles.yaw, approachRate * Time.Delta );
		var pitch = ApproachAngle( desiredAngles.pitch, currentAngles.pitch, .5f * approachRate * Time.Delta );

		var angles = new Angles( pitch, yaw, 0 );
		Bot.NextBot.Locomotion.FaceTowards( angles );
	}

	public virtual void CheckSightedOnTarget()
	{
		var forward = Bot.EyeRotation.Forward;
		var toTarget = LookAtPosition - Bot.EyePosition;
		toTarget = toTarget.Normal;

		var dot = forward.Dot( toTarget );
		if ( dot > OnTargetTolerance )
		{
			if ( !IsSightedIn )
			{
				IsSightedIn = true;
				NextBots.Msg( NextBotDebugFlags.LookAt, $"{this} Sighted on Target!" );
			}
		}
		else
		{
			IsSightedIn = false;
		}
	}

	public virtual void UpdateLookAtPositionFromSubject( Entity subject )
	{
		if ( LookAtTrackingTimer.IsElapsed() )
		{
			// update subject tracking by periodically estimating linear aim velocity, allowing for "slop" between updates
			var desiredLookAtPos = subject.WorldSpaceBounds.Center;
			desiredLookAtPos += GetHeadAimSubjectLeadTime() * subject.Velocity;

			var errorVector = desiredLookAtPos - LookAtPosition;
			float error = errorVector.Length;
			errorVector = errorVector.Normal;

			float trackingInterval = GetHeadAimTrackingInterval();
			if ( trackingInterval < Time.Delta )
				trackingInterval = Time.Delta;

			float errorVel = error / trackingInterval;

			LookAtVelocity = (errorVel * errorVector) + subject.Velocity;
			LookAtTrackingTimer.Start( Rand.Float( 0.8f, 1.2f )* trackingInterval );
		}

		LookAtPosition += LookAtVelocity * Time.Delta;
	}

	public virtual void UpkeepAimSteady()
	{
		//
		// Update Aim Steady
		//

		var eyeRotation = Bot.EyeRotation;
		var forward = eyeRotation.Forward;
		var currentAngles = eyeRotation.Angles();

		var deltaDiff = eyeRotation.Distance( LastEyeRotation );
		AimRate = MathF.Abs( deltaDiff / Time.Delta );
		LastEyeRotation = eyeRotation;

		TrackAimSteady();

		if ( NextBots.IsDebugging( NextBotDebugFlags.LookAt ) )
		{
			if ( IsHeadSteady() )
			{
				var maxTime = 3;
				var t = GetHeadSteadyDuration() / maxTime;
				t = Math.Clamp( t, 0, 1 );
				DebugOverlay.Sphere( Bot.EyePosition, t * 10, Color.Cyan );
			}

			float thickness = IsHeadSteady() ? 2 : 3;
			int r = IsSightedIn ? 255 : 0;
			int g = LookAtSubject.IsValid() ? 255 : 0;
			DebugOverlay.Arrow( Bot.EyePosition, LookAtPosition, thickness, Color.FromBytes( r, g, 1 ) );
		}
	}

	public virtual bool TrackAimSteady()
	{
		if ( AimRate < GetHeadAimSteadyMaxRate() )
		{
			if ( !HeadSteadyTimer.HasStarted() )
			{
				NextBots.Msg( NextBotDebugFlags.LookAt, $"{this} STEADY!" );
				HeadSteadyTimer.Start();
			}

			return true;
		}

		HeadSteadyTimer.Invalidate();
		return false;
	}

	public virtual void FaceTowards( Angles angles )
	{
		// player body follows view direction
		Bot.EyeRotation = angles.ToRotation();
	}

	public virtual void AimHeadTowards( Vector3 lookAtPos, LookAtPriorityType priority = LookAtPriorityType.Boring, float duration = 0, string reason = "" )
	{
		if ( !CanAimHead( priority, duration, reason ) )
			return;

		if ( duration <= 0 )
			duration = .1f;

		LookAtExpireTimer.Start( duration );

		// if given the same point, just update priority
		if ( LookAtPosition.Distance( lookAtPos ) < 1 ) 
		{
			LookAtPriority = priority;
			return;
		}

		LookAtPosition = lookAtPos;
		LookAtSubject = null;

		LookAtPriority = priority;
		LookAtDurationTimer.Start();

		IsSightedIn = false;

		NextBots.Msg( NextBotDebugFlags.LookAt, $"{Bot} Look At {lookAtPos} for {duration} seconds ('{reason}')" );
	}

	public virtual void AimHeadTowards( Entity subject, LookAtPriorityType priority = LookAtPriorityType.Boring, float duration = 0, string reason = "" )
	{
		if ( !CanAimHead( priority, duration, reason ) )
			return;

		if ( duration <= 0 )
			duration = .1f;

		LookAtExpireTimer.Start( duration );

		// if given the same point, just update priority
		if ( LookAtSubject == subject )
		{
			LookAtPriority = priority;
			return;
		}

		LookAtSubject = subject;

		LookAtPriority = priority;
		LookAtDurationTimer.Start();

		IsSightedIn = false;

		NextBots.Msg( NextBotDebugFlags.LookAt, $"{Bot} Look At {subject} for {duration} seconds ('{reason}')" );
	}

	public virtual bool CanAimHead( LookAtPriorityType priority = LookAtPriorityType.Boring, float duration = 0, string reason = "" )
	{
		if ( LookAtPriority == priority )
		{
			if ( !IsHeadSteady() || GetHeadSteadyDuration() < nb_head_aim_settle_duration )
			{
				NextBots.Msg( NextBotDebugFlags.LookAt, $"Look at '{reason}' rejected, previous aim not {(IsHeadSteady() ? "settled long enough" : "head-steady")}" );
				return false;
			}
		}

		// don't short-circuit if "sighted in" to avoid rapid view jitter
		if ( LookAtPriority > priority && !LookAtExpireTimer.IsElapsed() )
		{
			NextBots.Msg( NextBotDebugFlags.LookAt, $"Look at '{reason}' rejected, higher priority aim in process" );
			return false;
		}

		return true;
	}

	/// <summary>
	/// Is the aim steady on the target?
	/// </summary>
	public virtual bool IsHeadSteady() => HeadSteadyTimer.HasStarted();

	/// <summary>
	/// How long the aim has been steady on the target?
	/// </summary>
	public virtual float GetHeadSteadyDuration() => HeadSteadyTimer.HasStarted() ? HeadSteadyTimer.GetElapsedTime() : 0;
	public virtual float GetHeadAimApproachRate() => nb_head_aim_approach_rate;
	public virtual float GetHeadAimSubjectLeadTime() => 0;
	public virtual float GetHeadAimTrackingInterval() => nb_head_aim_tracking_interval;

	[ConVar.Server] public static float nb_head_aim_steady_max_rate { get; set; } = 100;
	[ConVar.Server] public static float nb_head_aim_settle_duration { get; set; } = 0.3f;
	[ConVar.Server] public static float nb_head_aim_resettle_angle { get; set; } = 100;
	[ConVar.Server] public static float nb_head_aim_resettle_time { get; set; } = 0.3f;
	[ConVar.Server] public static float nb_head_aim_approach_rate { get; set; } = 1000;
	[ConVar.Server] public static float nb_head_aim_tracking_interval { get; set; } = 0;


	// BUGBUG: Why doesn't this call angle diff?!?!?
	float ApproachAngle( float target, float value, float speed )
	{
		float delta = target - value;

		// Speed is assumed to be positive
		if ( speed < 0 )
			speed = -speed;

		if ( delta < -180 )
			delta += 360;
		else if ( delta > 180 )
			delta -= 360;

		if ( delta > speed )
			value += speed;
		else if ( delta < -speed )
			value -= speed;
		else
			value = target;

		return value;
	}


	// BUGBUG: Why do we need both of these?
	float AngleDiff( float destAngle, float srcAngle )
	{
		float delta;

		delta = (destAngle - srcAngle) % 360;
		if ( destAngle > srcAngle )
		{
			if ( delta >= 180 )
				delta -= 360;
		}
		else
		{
			if ( delta <= -180 )
				delta += 360;
		}
		return delta;
	}
}
