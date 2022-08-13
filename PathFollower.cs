﻿using Sandbox;
using System.Collections.Generic;

namespace Amper.NextBot;

public class NextBotPathFollower : IValid
{
	public float GoalTolerance = 25;
	public float DropDistanceDropScale = 0.5f;
	public float MaxPathDistance = -1;
	public bool AllowPartialPaths = false;
	Vector3 LastGoalPosition;

	NavPath _path;

	public IReadOnlyList<NavPathSegment> Segments => _path?.Segments;
	public TimeSince Age => _path?.Age ?? default;
	public float TotalLength => _path?.TotalLength ?? 0;
	public int SegmentCount => _path?.Count ?? 0;

	public int TargetSegmentIndex = 0;


	/// <summary>
	/// Compute shortest path from bot to goal.
	/// If returns true, path was found to the subject.
	/// If returns false, path may either be invalid (use IsValid() to check), or valid but
	/// doesn't reach all the way to the subject.
	/// </summary>
	public bool Build( INextBot bot, Vector3 goal, float maxPathLength = -1, bool includeGoalIfPathFails = true )
	{
		Invalidate();
		var start = bot.Position;
		LastGoalPosition = goal;

		start = NavMesh.GetClosestPoint( bot.Position ).Value;
		goal = NavMesh.GetClosestPoint( goal ).Value;

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
		_path = pathBuilder.Build( goal );
		if ( _path == null )
			return false;

		// We always need to have at least two nodes - end and start.
		if ( Segments.Count < 2 )
		{
			Invalidate();
			return false;
		}

		// Move towards first node.
		TargetSegmentIndex = 1;
		NextBots.Msg( NextBotDebugFlags.Path, $"Path built for {bot}." );

		return IsValid;
	}

	public void Invalidate()
	{
		_path = null;
		TargetSegmentIndex = 0;
	}

	public void Update( INextBot bot )
	{
		// track most recent path followed
		bot.NextBot.Path = this;

		// no segments in path.
		if ( !IsValid )
		{
			bot.NextBot.Locomotion.Approach( LastGoalPosition );
			return;
		}

		// draw path lines
		if ( NextBots.IsDebugging( NextBotDebugFlags.Path ) )
			DebugDraw( bot );

		if ( !CheckProgress( bot ) )
			return;

		var mover = bot.NextBot.Locomotion;

		var goalNode = GetTargetNode();
		var goalPos = goalNode.Position;

		var forward = goalNode.Position - mover.GetFeet();
		forward.z = 0;
		var goalRange = forward.Length;
		forward = forward.Normal;

		var left = new Vector3( -forward.y, forward.x, 0 );

		if ( left.IsNearlyZero() )
		{
			// if left is zero, forward must also be - path follow failure
			bot.NextBot.InvokeEvent( new NextBotEventMoveToFailure { Path = this } );

			// don't invalidate if NextBotOnMoveToFailure just recomputed a new path
			if ( Age > 0 )
			{
				Invalidate();
			}

			NextBots.Msg( NextBotDebugFlags.Path, "PathFollower: NextBotOnMoveToFailure( Stuck ) because forward and left are ZERO" );
			return;
		}

		// unit vectors must follow floor slope
		var normal = mover.GetGroundNormal();

		// get forward vector along floor
		forward = forward.Cross( normal );

		// correct the sideways vector
		left = left.Cross( normal );

		if ( NextBots.IsDebugging( NextBotDebugFlags.Path ) )
		{
			var axisSize = 25;
			var feet = mover.GetFeet();

			DebugOverlay.Line( feet, feet + axisSize * forward, Color.Red, 0.1f );
			DebugOverlay.Line( feet, feet + axisSize * normal, Color.Green, 0.1f );
			DebugOverlay.Line( feet, feet + axisSize * left, Color.Blue, 0.1f );
		}

		// climb ledges
		if ( !Climbing( bot, goalNode, forward, left, goalRange ) )
		{
			// a failed climb could mean an invalid path
			if ( !IsValid )
				return;
		}

		var lookAt = goalPos.WithZ( bot.EyePosition.z );

		bot.NextBot.Locomotion.AimHeadTowards( lookAt, LookAtPriorityType.Boring, 0.1f, "Body facing." );
		bot.NextBot.Locomotion.Approach( goalPos );
	}

	public bool Climbing( INextBot bot, NavPathSegment goal, Vector3 forward, Vector3 left, float goalRange )
	{
		var mover = bot.NextBot.Locomotion;
		
		// Check if we're allowed to climb.
		if ( !mover.IsAbleToClimb() || !nb_allow_climbing )
			return false;

		// Check if we're in state to climb.
		if ( mover.IsClimbingOrJumping() || mover.IsAscendingOrDescendingLadder() || !mover.IsOnGround() )
			return false;

		var climbDirection = forward.WithZ( 0 ).Normal;

		// we can't have this as large as our hull width, or we'll find ledges ahead of us
		// that we will fall from when we climb up because our hull wont actually touch at the top.
		float ledgeLookAheadRange = bot.WorldSpaceBounds.Size.x - 1;

		// Disabled until s&box's NavMesh gives up proper ClimbUp attribute.
#if false
		// Trust what that nav mesh tells us.
		// No need for expensive ledge-finding for games with simpler geometry 

		if ( m_goal->type == CLIMB_UP )
		{
			const Segment *afterClimb = NextSegment( m_goal );
			if ( afterClimb && afterClimb->area )
			{
				// find closest point on climb-destination area
				Vector nearClimbGoal;
				afterClimb->area->GetClosestPointOnArea( mover->GetFeet(), &nearClimbGoal );

				climbDirection = nearClimbGoal - mover->GetFeet();
				climbDirection.z = 0.0f;
				climbDirection.NormalizeInPlace();

				if ( mover->ClimbUpToLedge( nearClimbGoal, climbDirection, NULL ) )
					return true;
			}
		}

		return false;
#endif

		var heightDiff = goal.Position.z - bot.Position.z;
		if ( heightDiff > mover.StepHeight )
		{
			mover.Jump();
		}

		return true;
	}

	public void DebugDraw( INextBot me )
	{
		var i = 0;
		foreach ( var segment in Segments )
		{
			DebugOverlay.Line( segment.Position, segment.Position + segment.Forward * segment.Length, Color.Yellow, 0.1f, false );
			DebugOverlay.Sphere( segment.Position, 2, Color.Blue, 0.1f , false);
			DebugOverlay.Text(
				$"How: {segment.How}\n" +
				$"SegmentType: {segment.SegmentType}\n",
			segment.Position, 0.1f );
			i++;
		}


		if ( me.NextBot != null && me.NextBot.Path != null )
		{
			me.NextBot?.DisplayDebugText( "Locomotion:" );
			me.NextBot?.DisplayDebugText( "- Path Segments: " + me.NextBot.Path.Segments.Count );
			me.NextBot?.DisplayDebugText( "- Path Node Target: " + me.NextBot.Path.TargetSegmentIndex );
		}
	}

	public bool CheckProgress( INextBot bot )
	{
		var mover = bot.NextBot.Locomotion;

#if false
	// skip nearby goal points that are redundant to smooth path following motion
	const Path::Segment *pSkipToGoal = NULL;
	if ( m_minLookAheadRange > 0.0f )
	{
		pSkipToGoal = m_goal;
		const Vector &myFeet = mover->GetFeet();
		while( pSkipToGoal && pSkipToGoal->type == ON_GROUND && mover->IsOnGround() )
		{
			if ( ( pSkipToGoal->pos - myFeet ).IsLengthLessThan( m_minLookAheadRange ) )
			{
				// goal is too close - step to next segment
				const Path::Segment *nextSegment = NextSegment( pSkipToGoal );

				if ( !nextSegment || nextSegment->type != ON_GROUND )
				{
					// can't skip ahead to next segment - head towards current goal
					break;
				}

				if ( nextSegment->pos.z > myFeet.z + mover->GetStepHeight() )
				{
					// going uphill or up stairs tends to cause problems if we skip ahead, so don't
					break;
				}

				// can we reach the next path segment directly
				if ( mover->IsPotentiallyTraversable( myFeet, nextSegment->pos ) && !mover->HasPotentialGap( myFeet, nextSegment->pos ) )
				{
					pSkipToGoal = nextSegment;
				}
				else
				{
					// can't directly reach next segment - keep heading towards current goal
					break;
				}
			}
			else
			{
				// goal is farther than min lookahead
				break;
			}
		}

		// didn't find any goal to skip to
		if (pSkipToGoal == m_goal )
		{
			pSkipToGoal = NULL;
		}
	}
#endif

		if ( IsAtGoal( bot ) )
		{
			var nextSegment = GetNextNode();
			if ( nextSegment == null )
			{
				//if(mover.IsGround())
				{
					NextBots.Msg( NextBotDebugFlags.Path, $"{bot} finished moving to target - Success!" );
					// bot.NextBot.InvokeEvent( new NextBotEventMoveToSuccess() );

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
				TargetSegmentIndex++;
			}
		}

		return true;
	}

	public bool IsAtGoal( INextBot bot )
	{
		var mover = bot.NextBot.Locomotion;

		// node from which we are moving forward.
		var currentNode = GetPriorNode();

		// If we don't have a prior node, assume we're already on goal.
		if ( currentNode == null )
			return true;

		var targetNode = GetTargetNode();
		var toGoal = targetNode.Position - mover.GetFeet();

#if false

		const Segment *next = NextSegment( m_goal );

		if ( next )
		{
			// because mover may be off the path, check if it crossed the plane of the goal
			// check against average of current and next forward vectors
			Vector2D dividingPlane;

			if ( current->ladder )
			{
				dividingPlane = m_goal->forward.AsVector2D();
			}
			else
			{
				dividingPlane = current->forward.AsVector2D() + m_goal->forward.AsVector2D();
			}

			if ( DotProduct2D( toGoal.AsVector2D(), dividingPlane ) < 0.0001f &&
				 abs( toGoal.z ) < body->GetStandHullHeight() )
			{	
				// only skip higher Z goal if next goal is directly reachable
				// can't use this for positions below us because we need to be able
				// to climb over random objects along our path that we can't actually
				// move *through*
				if ( toGoal.z < mover->GetStepHeight() && ( mover->IsPotentiallyTraversable( mover->GetFeet(), next->pos ) && !mover->HasPotentialGap( mover->GetFeet(), next->pos ) ) )
				{
					// passed goal
					return true;
				}
			}
		}
#endif
		var sqrTolerance = GoalTolerance * GoalTolerance;

		// proximity check
		// Z delta can be anything, since we may be climbing over a tall fence, a physics prop, etc.
		if ( toGoal.WithZ( 0 ).LengthSquared < sqrTolerance ) 
		{
			// reached goal
			return true;
		}

		return false;
	}

	public NavPathSegment GetNode( int index )
	{
		if ( index < 0 || index >= Segments.Count )
			return null;

		return Segments[index];
	}

	public NavPathSegment GetPriorNode() => GetNode( TargetSegmentIndex - 1 );
	public NavPathSegment GetTargetNode() => GetNode( TargetSegmentIndex );
	public NavPathSegment GetNextNode() => GetNode( TargetSegmentIndex + 1 );

	public bool IsValid => SegmentCount > 0;

	public float GetRemainingDistance( INextBot bot )
	{
		if ( !IsValid )
			return 0;

		var dist = 0f;
		var targetNode = GetTargetNode();
		if ( targetNode == null )
			return 0;

		dist += bot.Position.Distance( targetNode.Position );
		for ( var i = TargetSegmentIndex + 1; i < SegmentCount; i++ )
		{
			var segment = GetNode( i );
			if ( segment == null )
				continue;

			dist += segment.Length;
		}

		return dist;
	}

	[ConVar.Server] public static bool nb_allow_climbing { get; set; } = true;
	[ConVar.Server] public static bool nb_allow_gap_jumping { get; set; } = true;
	[ConVar.Server] public static bool nb_allow_avoiding { get; set; } = true;
}
