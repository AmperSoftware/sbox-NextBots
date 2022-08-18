using Sandbox;
using System.Collections.Generic;
using System;

namespace Amper.NextBot;

public class NextBotPathFollower : IValid
{
	public float GoalTolerance = 25;
	public float DropDistanceDropScale = 0.5f;
	public float MaxPathDistance = -1;
	public float MinLookAheadDistance = 300;
	public bool AllowPartialPaths = false;

	public List<NavPathSegment> Segments = new();
	Dictionary<NavPathSegment, int> SegmentIndexes = new();
	public float TotalLength;
	public int SegmentCount;
	public TimeSince Age;

	NavPathSegment Goal;

	public void Invalidate()
	{
		Segments.Clear();
		SegmentIndexes.Clear();

		TotalLength = 0;
		SegmentCount = 0;

		Goal = null;
	}

	private bool SetupPath( NavPath path )
	{
		if ( path == null )
			return false;

		Segments = path.Segments;

		int i = 0;
		foreach ( var segment in Segments )
			SegmentIndexes[segment] = i++;

		SegmentCount = path.Count;
		TotalLength = path.TotalLength;
		Age = path.Age;

		Goal = FirstSegment();

		return IsValid;
	}

	/// <summary>
	/// Compute shortest path from bot to goal.
	/// If returns true, path was found to the subject.
	/// If returns false, path may either be invalid (use IsValid() to check).
	/// </summary>
	public bool Build( INextBot bot, Vector3 goal, float maxPathLength = -1, bool includeGoalIfPathFails = true )
	{
		Invalidate();
		var start = bot.Position;

		// Cant compute path without a locomotion component.
		var mover = bot.NextBot.Locomotion;
		if ( mover == null )
		{
			Log.Error( "Can't build path without locomotion component." );
			return false;
		}

		var pathBuilder = NavMesh.PathBuilder( start )
			// Configuration from Locomotion Interface
			.WithStepHeight( mover.StepHeight )
			.WithAgentHull( mover.AgentHull )
			.WithMaxDropDistance( mover.MaxDropDistance )
			.WithMaxClimbDistance( mover.MaxClimpDistance )

			// Configuration from path
			.WithDropDistanceCostScale( DropDistanceDropScale )
			.WithMaxDistance( MaxPathDistance );

		// Allow partial paths to be built.
		if ( AllowPartialPaths )
		{
			pathBuilder = pathBuilder.WithPartialPaths();
		}

		// Build the path.
		var path = pathBuilder.Build( goal );
		if ( path == null )
			return false;

		if ( !SetupPath( path ) )
		{
			Invalidate();
			return false;
		}

		return true;
	}

	public void Update( INextBot bot )
	{
		// track most recent path followed
		bot.NextBot.Path = this;

		// no segments in path.
		if ( !IsValid || Goal == null ) 
			return;

		// draw path lines
		if ( NextBots.IsDebugging( NextBotDebugFlags.Path ) )
			DebugDraw( bot );

		AdjustSpeed( bot );

		// Check if maybe we have already reached our path.
		if ( !CanContinueMovingAlongPath( bot ) )
			return;

		var mover = bot.NextBot.Locomotion;
		var goalPos = Goal.Position;

		var forward = goalPos - mover.GetFeet();
		forward = forward.Normal;

		var normal = mover.GetGroundNormal();
		var left = new Vector3( -forward.y, forward.x, 0 );

		if ( NextBots.IsDebugging( NextBotDebugFlags.Path ) )
		{
			var axisSize = 25;
			var feet = mover.GetFeet();

			DebugOverlay.Line( feet, feet + axisSize * forward, Color.Red, 0.1f );
			DebugOverlay.Line( feet, feet + axisSize * normal, Color.Green, 0.1f );
			DebugOverlay.Line( feet, feet + axisSize * left, Color.Blue, 0.1f );
		}

		var lookAt = goalPos.WithZ( bot.EyePosition.z );

		bot.NextBot.Locomotion.AimHeadTowards( lookAt, LookAtPriorityType.Boring, 0.1f, "Body facing." );
		bot.NextBot.Locomotion.Approach( goalPos );
	}

	public bool CanContinueMovingAlongPath( INextBot bot )
	{
		var mover = bot.NextBot.Locomotion;

		NavPathSegment skipToGoal = null;
		if ( MinLookAheadDistance > 0 )
		{
			skipToGoal = Goal;
			var myFeet = mover.GetFeet();

			while ( skipToGoal != null && skipToGoal.SegmentType == NavNodeType.OnGround && mover.IsOnGround() )
			{
				if ( (skipToGoal.Position - myFeet).Length < MinLookAheadDistance )
				{
					var nextSegment = NextSegment( skipToGoal );

					// can't skip ahead to next segment - head towards current goal
					if ( nextSegment == null || nextSegment.SegmentType == NavNodeType.OnGround ) 
						break;

					// going uphill or up stairs tends to cause problems if we skip ahead, so don't
					if ( nextSegment.Position.z > myFeet.z + mover.StepHeight )
						break;


				}
				else
				{
					// goal is farther than min lookahead
					break;
				}
			}

			// didn't find any goal to skip to
			if ( skipToGoal == Goal )
				skipToGoal = null;
		}

		if ( IsAtGoal( bot ) )
		{
			var nextSegment = skipToGoal ?? NextSegment( Goal );

			if ( nextSegment == null )
			{
				if ( mover.IsOnGround() ) 
				{
					NextBots.Msg( NextBotDebugFlags.Path, $"{bot} finished moving to target - Success!" );
					bot.NextBot.InvokeEvent( new NextBotEventMoveToSuccess() );

					// don't invalidate if OnMoveToSuccess just recomputed a new path
					if ( Age > 0 )
					{
						Invalidate();
					}

					return false;
				}
			}
			else
			{
				// keep moving.
				Goal = nextSegment;
			}
		}

		return true;
	}

	public void AdjustSpeed( INextBot bot )
	{
		var mover = bot.NextBot.Locomotion;

		if ( !mover.IsOnGround() )
		{
			// If we are in the air, use max speed.
			mover.Run();
			return;
		}

		// speed based on curvature
		mover.DesiredSpeed = mover.RunSpeed + MathF.Abs( Goal.Curvature ) * (mover.WalkSpeed - mover.RunSpeed);
	}

	public bool IsAtGoal( INextBot bot )
	{
		var mover = bot.NextBot.Locomotion;

		// node from which we are moving forward.
		var current = PriorSegment( Goal );
		var toGoal = Goal.Position - mover.GetFeet();

		// Passed goal.
		if ( current == null )
			return true;

		if ( Goal.SegmentType == NavNodeType.DropDown )
		{
			var landing = NextSegment( Goal );

			// passed goal or corrupt path
			if ( landing == null )
				return true;

			// did we reach the ground
			if ( mover.GetFeet().z - landing.Position.z < mover.StepHeight )
				return true;
		}

		// proximity check
		// Z delta can be anything, since we may be climbing over a tall fence, a physics prop, etc.
		if ( toGoal.WithZ( 0 ).Length < GoalTolerance ) 
		{
			// reached goal
			return true;
		}

		return false;
	}

	public bool IsValid => SegmentCount > 0;

	public float GetRemainingDistance( INextBot bot )
	{
		return 0;
	}

	public void DebugDraw( INextBot me )
	{
		var i = 0;
		foreach ( var segment in Segments )
		{
			DebugOverlay.Line( segment.Position, segment.Position + segment.Forward * segment.Length, Color.Yellow, 0.1f, false );
			DebugOverlay.Sphere( segment.Position, 2, Color.Blue, 0.1f, false );
			DebugOverlay.Text(
				$"How: {segment.How}\n" +
				$"SegmentType: {segment.SegmentType}\n",
			segment.Position, 0.1f );
			i++;
		}
	}

	public NavPathSegment FirstSegment()
	{
		return GetSegment( 0 );
	}

	public NavPathSegment NextSegment( NavPathSegment segment )
	{
		var index = GetSegmentIndex( segment );
		if ( index == -1 )
			return null;

		return GetSegment( index + 1 );
	}

	public NavPathSegment PriorSegment( NavPathSegment segment )
	{
		var index = GetSegmentIndex( segment );
		if ( index == -1 )
			return null;

		return GetSegment( index - 1 );
	}

	public NavPathSegment GetSegment( int index )
	{
		if ( index < 0 || index >= Segments.Count )
			return null;

		return Segments[index];
	}

	public int GetSegmentIndex( NavPathSegment segment )
	{
		if ( SegmentIndexes.TryGetValue( segment, out var index ) )
			return index;

		return -1;
	}

	[ConVar.Server] public static bool nb_allow_climbing { get; set; } = true;
	[ConVar.Server] public static bool nb_allow_gap_jumping { get; set; } = true;
	[ConVar.Server] public static bool nb_allow_avoiding { get; set; } = true;
}
