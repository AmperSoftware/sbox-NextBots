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
	/// <summary>
	/// Instantaneously move our vison towards a point in space.
	/// </summary>
	public void FaceTowards( Vector3 point );
	/// <summary>
	/// Do a jump in place.
	/// </summary>
	public void Jump();
	/// <summary>
	/// Modifies the point to add avoidance.
	/// </summary>
	public Vector3 Avoid( Vector3 point );


	public Vector3 GetGroundNormal();
	public Vector3 GetFeet();

	public bool IsClimbingOrJumping();
	public bool IsAscendingOrDescendingLadder();
	public bool IsAbleToClimb();
	public bool IsOnGround();

	public void ClearStuckStatus( string reason );

}

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

	public override void Update()
	{
		StuckMonitor();

		if ( NextBots.IsDebugging( NextBotDebugFlags.Locomotion ) )
		{
			Bot.NextBot.DisplayDebugText( "Locomotion: " );
			Bot.NextBot.DisplayDebugText( $"- Stuck: {IsStuck}" );
			Bot.NextBot.DisplayDebugText( $"- Time Since Stuck: {TimeSinceStuck}" );
			Bot.NextBot.DisplayDebugText( $"- Time Since Move Requested: {TimeSinceMoveRequested}" );

			DebugOverlay.Line( Bot.EyePosition, Bot.EyePosition + Bot.ViewVector * 100, Color.Cyan, 0.1f );
		}
	}

	TimeSince TimeSinceMoveRequested { get; set; }
	public float WishSpeed { get; set; }

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
	public virtual bool IsOnGround() => Bot.GroundEntity != null;

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
			.HitLayer( CollisionLayer.All, false )
			.HitLayer( CollisionLayer.Solid, true )
			.HitLayer( CollisionLayer.GRATE, true )
			.HitLayer( CollisionLayer.PLAYER_CLIP, true )
			.HitLayer( CollisionLayer.WINDOW, true )
			.HitLayer( CollisionLayer.SKY, true )
			.Ignore( Bot.Entity );
	}

	public bool IsAttemptingToMove() => TimeSinceMoveRequested >= 0 && TimeSinceMoveRequested < 0.25f;
}
