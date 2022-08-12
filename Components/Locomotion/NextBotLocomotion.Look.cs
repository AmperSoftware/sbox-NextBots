using Sandbox;
using System;

namespace Amper.NextBot;

partial class NextBotLocomotion
{
	Vector3 LookAtPosition;
	Entity LookAtSubject;

	public LookAtPriorityType LookAtPriority;
	public CountdownTimer LookAtExpireTimer = new();	// How long until this look at expires

	public IntervalTimer LookAtDurationTimer = new();	// How long we have been looking at something
	public IntervalTimer HeadSteadyTimer = new();       // How long we have kept our view steady at the object

	public float AimRate;
	public Rotation PriorAim;

	public Vector3 AnchorForward;
	public CountdownTimer AnchorRepositionTimer = new();

	public bool IsSightedIn;

	public virtual float GetHeadAimSteadyMaxRate() => nb_head_aim_steady_max_rate;

	public virtual void UpkeepAim()
	{
		var deltaT = Time.Delta;
		var angles = Bot.EyeRotation;
		var forward = angles.Forward;

		var deltaDiff = angles.Distance( PriorAim );
		AimRate = MathF.Abs( deltaDiff / deltaT );
		PriorAim = angles;

		TrackAimSteady();

		if ( NextBots.IsDebugging( NextBotDebugFlags.LookAt ) )
		{
			if ( IsHeadSteady() )
			{
				var maxTime = 3;
				var t = GetHeadSteadyDuration() / maxTime;
				t = Math.Clamp( t, 0, 1 );
				DebugOverlay.Sphere( Bot.EyePosition, t * 10, Color.Cyan, 2 * deltaT );
			}
		}

		// if our current look-at has expired, don't change our aim further
		if ( IsSightedIn && LookAtExpireTimer.IsElapsed() )
			return;

		var deltaAngle = MathF.Acos( forward.Dot( AnchorForward ) ).RadianToDegree();
		if ( deltaAngle > nb_head_aim_resettle_angle )
		{
			AnchorRepositionTimer.Start( Rand.Float( 0.9f, 1.1f ) * nb_head_aim_resettle_time );
			AnchorForward = forward;
			return;
		}

		if ( AnchorRepositionTimer.HasStarted() && !AnchorRepositionTimer.IsElapsed() )
			return;

		AnchorRepositionTimer.Invalidate();
		
		// If we have a subject, update look at point.
		var subject = LookAtSubject;
		if ( subject.IsValid() )
		{
			LookAtPosition = subject.EyePosition;
		}

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

		var approachRate = nb_head_aim_max_approach_velocity;
		var desiredAngles = Rotation.LookAt( toTarget, Vector3.Up );
		var newAngles = Rotation.Lerp( angles, desiredAngles, approachRate * Time.Delta );
		FaceTowards( newAngles );
	}

	public const float OnTargetTolerance = 0.98f;

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

	public virtual void FaceTowards( Rotation angles )
	{
		// player body follows view direction
		Bot.EyeRotation = angles;
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

	[ConVar.Server] public static float nb_head_aim_steady_max_rate { get; set; } = 100;
	[ConVar.Server] public static float nb_head_aim_settle_duration { get; set; } = 0.3f;
	[ConVar.Server] public static float nb_head_aim_resettle_angle { get; set; } = 100;
	[ConVar.Server] public static float nb_head_aim_resettle_time { get; set; } = 0.3f;
	[ConVar.Server] public static float nb_head_aim_max_approach_velocity { get; set; } = 1000;
}
