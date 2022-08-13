using Sandbox;

namespace Amper.NextBot;
public interface INextBotLocomotion : INextBotComponent
{
	/// <summary>
	/// The speed at which the bot wants to move right now. 
	/// </summary>
	public float DesiredSpeed { get; set; }	

	public float MaxJumpHeight { get; set; }
	public float StepHeight { get; set; }
	public float DefaultSpeed { get; set; }

	/// <summary>
	/// Is the bot stuck right now?
	/// </summary>
	public bool IsStuck { get; }

	/// <summary>
	/// Move the bot towards a specific point.
	/// </summary>
	public void Approach( Vector3 point, float goalWeigth = 1 );
	public void Jump();
	public Vector3 Avoid( Vector3 point );


	public Vector3 GetGroundNormal();
	public Vector3 GetFeet();

	public bool IsClimbingOrJumping();
	public bool IsAscendingOrDescendingLadder();
	public bool IsAbleToClimb();
	public bool IsOnGround();

	public void FaceTowards( Angles lookAt );
	public void AimHeadTowards( Vector3 lookAtPos, LookAtPriorityType priority = LookAtPriorityType.Boring, float duration = 0, string reason = "" );
	public void AimHeadTowards( Entity subject, LookAtPriorityType priority = LookAtPriorityType.Boring, float duration = 0, string reason = "" );

	public void ClearStuckStatus( string reason );
}


public enum LookAtPriorityType
{
	Boring,
	/// <summary>
	/// Last known enemy location, dangerous sound location
	/// </summary>
	Interesting,
	/// <summary>
	/// A danger
	/// </summary>
	Important,
	/// <summary>
	/// An active threat to our safety
	/// </summary>
	Critical,
	/// <summary>
	/// Nothing can interrupt this look at - two simultaneous look ats with this priority is an error
	/// </summary>
	Mandatory
};


/// <summary>
/// This is the locomotion interface. This define how bot moves around in the world.
/// </summary>
public partial class NextBotLocomotion : NextBotComponent, INextBotLocomotion
{
	public NextBotLocomotion( INextBot me ) : base( me ) 
	{
		DesiredSpeed = DefaultSpeed;
	}

	public float DesiredSpeed { get; set; }

	public Vector3 Position { get; set; }
	public Vector3 Velocity { get; set; }
	public Entity GroundEntity { get; set; }
	public Vector3 MoveVector { get; set; }

	public override void Reset()
	{
		InterpolationEnabled = true;
		LookAtPosition = 0;
		LookAtSubject = null;

		LookAtPriority = LookAtPriorityType.Boring;
		LookAtExpireTimer.Invalidate();
		LookAtDurationTimer.Invalidate();
		HeadSteadyTimer.Invalidate();

		AimRate = 0;
		LastEyeRotation = Rotation.Identity;
		IsSightedIn = false;
	}

	public virtual void SetupFromBot( INextBot bot )
	{
		Position = bot.Position;
		Velocity = bot.Velocity;
		GroundEntity = bot.GroundEntity;
	}

	public virtual void ApplyToBot( INextBot bot )
	{
		bot.Position = Position;
		bot.Velocity = Velocity;
		bot.GroundEntity = GroundEntity;
	}

	public override void Update()
	{
		StuckMonitor();
		UpdateMovement();

		if ( NextBots.IsDebugging( NextBotDebugFlags.Locomotion ) )
		{
			Bot.NextBot.DisplayDebugText( "Locomotion: " );	
			Bot.NextBot.DisplayDebugText( $"- Stuck: {IsStuck}" );
			Bot.NextBot.DisplayDebugText( $"- Time Since Stuck: {TimeSinceStuck}" );
			Bot.NextBot.DisplayDebugText( $"- Time Since Move Requested: {TimeSinceMoveRequested}" );
		}

		if ( NextBots.IsDebugging( NextBotDebugFlags.LookAt ) )
		{
			Bot.NextBot.DisplayDebugText( "Look At: " );
			Bot.NextBot.DisplayDebugText( $"- Aim Rate: {AimRate}" );
			Bot.NextBot.DisplayDebugText( $"- Is Head Steady: {IsHeadSteady()}" );
			Bot.NextBot.DisplayDebugText( $"- Sighted In: {IsSightedIn}" );
			Bot.NextBot.DisplayDebugText( $"- Head Steady Time: {GetHeadSteadyDuration()}" );
			Bot.NextBot.DisplayDebugText( $"- Look At Duration: {LookAtDurationTimer.GetElapsedTime()}" );
			Bot.NextBot.DisplayDebugText( $"- Look At Expire Timer: {LookAtExpireTimer.GetRemainingTime()}" );

			DebugOverlay.Line( Bot.EyePosition, Bot.EyePosition + Bot.ViewVector * 100, Color.Cyan, 0.1f );
		}
	}

	public void UpdateMovement()
	{
		// Revert everything that interpolation did.
		InterpolationMoveToFraction( 1 );

		SetupFromBot( Bot );
		StartInterpolation();

		ProcessMovement();

		StopInterpolation();
		ApplyToBot( Bot );

		// Start interpolating from our previous position.
		InterpolationMoveToFraction( 0 );
	}

	public virtual void ProcessMovement() { }

	public override void Upkeep()
	{
		UpkeepAim();
		UpkeepInterpolate();
	}

	TimeSince TimeSinceMoveRequested { get; set; }

	public virtual float MaxJumpHeight { get; set; } = 32;
	public virtual float StepHeight { get; set; } = 18;
	public virtual float DefaultSpeed { get; set; } = 120;

	public virtual void Approach( Vector3 point, float goalWeigth = 1 )
	{
		TimeSinceMoveRequested = 0;
	}

	public virtual void Jump() { }
	public virtual Vector3 Avoid( Vector3 point ) => point;

	public virtual Vector3 GetFeet() => Bot.Entity.Position;
	public virtual Vector3 GetGroundNormal() => Vector3.Up;

	public virtual bool IsClimbingOrJumping() => false;
	public virtual bool IsAscendingOrDescendingLadder() => false;
	public virtual bool IsAbleToClimb() => true;
	public virtual bool IsOnGround() => Bot.GroundEntity.IsValid();

	public virtual TraceResult TraceBBox( Vector3 start, Vector3 end )
	{
		return SetupBBoxTrace( start, end, Bot.Mins, Bot.Maxs ).Run();
	}

	public virtual TraceResult TraceBBox( Vector3 start, Vector3 end, Vector3 mins, Vector3 maxs )
	{

		return SetupBBoxTrace( start, end, mins, maxs ).Run();
	}

	public virtual Trace SetupBBoxTrace( Vector3 start, Vector3 end )
	{
		return SetupBBoxTrace( start, end, Bot.Mins, Bot.Maxs );
	}

	public virtual Trace SetupBBoxTrace( Vector3 start, Vector3 end, Vector3 mins, Vector3 maxs )
	{
		return Trace.Ray( start, end )
			.Size( mins, maxs )

			// Collides with:
			.WithAnyTags( CollisionTags.Solid )
			.WithAnyTags( CollisionTags.Ladder )
			.WithAnyTags( CollisionTags.Clip )
			.WithAnyTags( CollisionTags.NPCClip )

			// Doesn't collide with:
			.WithoutTags( CollisionTags.NotSolid )
			.WithoutTags( CollisionTags.Debris )
			.WithoutTags( CollisionTags.Weapon )
			.WithoutTags( CollisionTags.Projectile )

			.Ignore( Bot.Entity );
	}

	public bool IsAttemptingToMove() => TimeSinceMoveRequested >= 0 && TimeSinceMoveRequested < 0.25f;
}
