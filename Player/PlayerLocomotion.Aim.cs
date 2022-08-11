using Sandbox;
using System;

namespace Amper.NextBot;

 partial class NextBotPlayerLocomotion
{
	Vector3 LookAtPosition;
	Entity LookAtSubject;
	Vector3 LookAtVelocity;
	QAngle PriorAngles;

	CountdownTimer LookAtTrackingTimer = new();

	Vector3 AnchorForward;
	CountdownTimer AnchorRepositionTimer = new();

	bool IsSightedIn;
	bool HasBeenSightedIn;

#if false
	public virtual void UpkeepAim()
	{
		return;

		var deltaT = Time.Delta;
		if ( deltaT < 0.00001f )
			return;

		var entity = Bot.Entity;

		var currentAngles = (QAngle)entity.EyeRotation;
		bool isSteady = true;

		var actualPitchRate = AngleDiff( currentAngles.x, PriorAngles.x );
		if ( MathF.Abs( actualPitchRate ) > nb_head_aim_steady_max_rate * deltaT )
		{
			isSteady = false;
		}
		else
		{
			float actualYawRate = AngleDiff( currentAngles.y, PriorAngles.y );

			if ( MathF.Abs( actualYawRate ) > nb_head_aim_steady_max_rate * deltaT )
			{
				isSteady = false;
			}
		}

		if ( isSteady )
		{
			if ( !HeadSteadyTimer.HasStarted() )
			{
				HeadSteadyTimer.Start();
			}
		}
		else
		{
			HeadSteadyTimer.Invalidate();
		}

		PriorAngles = currentAngles;

		// if our current look-at has expired, don't change our aim further
		if ( HasBeenSightedIn && LookAtExpireTimer.IsElapsed() )
			return;

		// simulate limited range of mouse movements
		// compute the angle change from "center"
		var forward = entity.EyeRotation.Forward;
		float deltaAngle = MathF.Acos( forward.Dot( AnchorForward ) ).RadianToDegree();
		if ( deltaAngle > nb_head_aim_resettle_angle )
		{
			// time to recenter our 'virtual mouse'
			AnchorRepositionTimer.Start( Rand.Float( 0.9f, 1.1f ) * nb_head_aim_resettle_time );
			AnchorForward = forward;
			return;
		}

		// if we're currently recentering our "virtual mouse", wait
		if ( AnchorRepositionTimer.HasStarted() && !AnchorRepositionTimer.IsElapsed() )
			return;

		AnchorRepositionTimer.Invalidate();

		var subject = LookAtSubject;
		if ( subject.IsValid() )
		{
			if ( LookAtTrackingTimer.IsElapsed() )
			{
				var desiredLookPos = subject.EyePosition;
				desiredLookPos += GetHeadAimSubjectLeadTime() * subject.Velocity;

				var errorVector = desiredLookPos - LookAtPosition;
				var error = errorVector.Length;

				var trackingInterval = GetHeadAimTrackingInterval();
				if ( trackingInterval < deltaT )
					trackingInterval = deltaT;

				var errorvel = error / trackingInterval;
				LookAtVelocity = (errorvel * errorVector) + subject.Velocity;
				LookAtTrackingTimer.Start( Rand.Float( 0.8f, 1.2f ) * trackingInterval );
			}

			LookAtPosition += LookAtVelocity * deltaT;
		}


		// aim view towards last look at point
		var to = LookAtPosition - entity.EyePosition;
		to = to.Normal;
		DebugOverlay.Sphere( LookAtPosition, 20, Color.Red, 2 * deltaT, false );
		Log.Info( LookAtPosition );

		QAngle desiredAngles = QAngle.FromVector( to );

		if ( NextBots.IsDebugging( NextBotDebugFlags.LookAt ) )
		{
			DebugOverlay.Line( entity.EyePosition, entity.EyePosition + 100 * forward, Color.Yellow, 2 * deltaT, false );
		}

		float onTargetTolerance = 0.98f;
		float dot = forward.Dot( to );
		if ( dot > onTargetTolerance )
		{
			IsSightedIn = true;

			if ( !HasBeenSightedIn )
			{
				HasBeenSightedIn = true;
				NextBots.Msg( NextBotDebugFlags.LookAt, $"{entity} SIGHTED IN!" );
			}
		}
		else
		{
			IsSightedIn = false;
		}

		var approachRate = 0;//nb_saccade_speed;
		float easeOut = 0.7f;
		if ( dot > easeOut )
		{
			var t = dot.RemapVal( easeOut, 1, 1, 0.02f );
			float halfPi = 1.57f;
			approachRate *= MathF.Sin( halfPi * t );
		}

		float easeInTime = 0.25f;
		if ( LookAtDurationTimer.GetElapsedTime() < easeInTime )
		{
			approachRate *= LookAtDurationTimer.GetElapsedTime() / easeInTime;
		}

		QAngle angles = default;
		angles.y = ApproachAngle( desiredAngles.y, currentAngles.y, approachRate * deltaT );
		angles.x = ApproachAngle( desiredAngles.y, desiredAngles.y, approachRate * deltaT * .5f );
		angles.z = 0;

		angles.x = AngleNormalize( angles.x );
		angles.y = AngleNormalize( angles.y );

		Bot.NextBot.FaceTowards( angles );
	}


	private float AngleDiff( float destAngle, float srcAngle )
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
	
	float anglemod( float a )
	{
		a = (360 / 65536) * ((int)(a * (65536 / 360.0f)) & 65535);
		return a;
	}

	float ApproachAngle( float target, float value, float speed )
	{
		target = anglemod( target );
		value = anglemod( value );

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
	float AngleNormalize( float angle )
	{
		angle = angle % 360.0f;
		if ( angle > 180 )
		{
			angle -= 360;
		}
		if ( angle < -180 )
		{
			angle += 360;
		}
		return angle;
	}
#endif
}
