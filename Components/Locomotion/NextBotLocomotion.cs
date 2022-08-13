using Sandbox;

namespace Amper.NextBot;



/// <summary>
/// This is the locomotion interface. This define how bot moves around in the world.
/// </summary>
public partial class NextBotLocomotion : NextBotComponent
{
	public NextBotLocomotion( INextBot me ) : base( me ) { }

	// Locomotion Configuration
	public NavAgentHull AgentHull;
	public float StepHeight = 18;
	public float MaxDropDistance = -1;
	public float MaxClimpDistance = -1;

	// Speed
	public float DesiredSpeed;
	public float WalkSpeed = 100;
	public float RunSpeed = 200;

	public Vector3 Position;
	public Vector3 Velocity;
	public Entity GroundEntity;
	public Vector3 MoveVector;

	public override void Reset()
	{
		// Interpolation
		InterpolationEnabled = true;

		// Look At
		LookAtPosition = 0;
		LookAtSubject = null;

		LookAtPriority = LookAtPriorityType.Boring;
		LookAtExpireTimer.Invalidate();
		LookAtDurationTimer.Invalidate();
		HeadSteadyTimer.Invalidate();

		AimRate = 0;
		LastEyeRotation = Rotation.Identity;
		IsSightedIn = false;

		// Move / Speed
		DesiredSpeed = WalkSpeed;
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
			Bot.NextBot.DisplayDebugText( $"- Desired Speed: {DesiredSpeed}" );
			Bot.NextBot.DisplayDebugText( $"- Step Height: {StepHeight}" );
			Bot.NextBot.DisplayDebugText( $"- MaxDropDistance: {MaxDropDistance}" );
			Bot.NextBot.DisplayDebugText( $"- MaxClimpDistance: {MaxClimpDistance}" );
			Bot.NextBot.DisplayDebugText( $"- Agent Hull: {AgentHull}" );
			Bot.NextBot.DisplayDebugText( "Stuck: " );
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

	public virtual void Approach( Vector3 point, float goalWeigth = 1 )
	{
		TimeSinceMoveRequested = 0;
	}

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

	//
	// Activities that Bot can do.
	//

	public virtual void Jump() { }

	public virtual void Run()
	{
		DesiredSpeed = RunSpeed;
	}

	public virtual void Walk()
	{
		DesiredSpeed = WalkSpeed;
	}

	public virtual void Stop()
	{
		DesiredSpeed = 0;
	}
}
