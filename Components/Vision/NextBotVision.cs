﻿using Sandbox;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Amper.NextBot;

/// <summary>
/// What the bot sees.
/// </summary>
public class NextBotVision : NextBotComponent
{
	public IReadOnlyList<KnownEntity> KnownEntities => _knownEntities;
	public List<KnownEntity> _knownEntities { get; set; } = new();

	float LastVisionUpdateTime { get; set; } = 0;
	Entity PrimaryThreat { get; set; }

	public float FieldOfView 
	{
		get => FOV;
		set
		{
			CosHalfFOV = MathF.Cos( 0.5f * value * MathF.PI / 180 );
			FOV = value;
		}
	}

	float FOV { get; set; }
	float CosHalfFOV { get; set; }
	public float DefaultFieldOfView { get; set; } = 90;

	public NextBotVision( INextBot bot ) : base( bot )
	{
		Reset();
	}

	public override void Reset()
	{
		base.Reset();

		_knownEntities.Clear();
		LastVisionUpdateTime = 0;
		PrimaryThreat = null;

		SetFieldOfView( DefaultFieldOfView );
	}

	public override void Update()
	{
		if( nb_blind )
		{
			_knownEntities.Clear();
			return;
		}

		UpdateKnownEntities();
		LastVisionUpdateTime = Time.Now;

		if ( NextBots.IsDebugging( NextBotDebugFlags.Vision ) )
		{
			Bot.NextBot.DisplayDebugText( "Vision:" );

			Bot.NextBot.DisplayDebugText( $"- FOV: {FOV}°" );
			Bot.NextBot.DisplayDebugText( $"- Primary Threat: {PrimaryThreat}" );
			Bot.NextBot.DisplayDebugText( "- Known Entities: " );
			foreach ( var known in KnownEntities )
			{
				Bot.NextBot.DisplayDebugText( $"  - {known.Entity} (visible: {known.IsVisible})" );
			}
		}
	}

	/// <summary>
	/// Given an entity, return our known version of it( or NULL if we don't know of it)
	/// </summary>
	public KnownEntity GetKnown( Entity entity )
	{
		if ( entity == null || !entity.IsValid )
			return null;

		foreach ( var known in KnownEntities )
		{
			if ( known.Entity == entity && !known.IsObsolete() )
				return known;
		}

		return null;
	}

	public bool IsAwareOf( KnownEntity known )
	{
		return known.GetTimeSinceBecameKnown() > GetMinRecognizeTime();
	}

	public void SetFieldOfView( float horizAngle )
	{
		FOV = horizAngle;
		CosHalfFOV = MathF.Cos( 0.5f * FOV * MathF.PI / 180 );
	}

	public virtual float GetMinRecognizeTime() => 0;
	public virtual float GetMaxVisionRange() => 2000;
	public virtual bool IsIgnored( Entity entity ) => false;

	public bool IsAbleToSee( Entity subject, bool useFov = true )
	{
		if ( Bot.NextBot.IsRangeGreaterThan( subject, GetMaxVisionRange() ) )
			return false;

		if ( useFov && !IsInFieldOfView( subject ) )
			return false;

		if ( !IsLineOfSightClear( subject ) )
			return false;

		return ShouldNoticeVisibleEntity( subject );
	}

	public bool IsAbleToSee( Vector3 point, bool useFov = true )
	{
		if ( Bot.NextBot.IsRangeGreaterThan( point, GetMaxVisionRange() ) )
			return false;

		if ( useFov && !IsInFieldOfView( point ) )
			return false;

		return IsLineOfSightClear( point );
	}

	public bool ShouldNoticeVisibleEntity( Entity subject ) => true;

	public bool IsInFieldOfView( Entity subject )
	{
		if ( IsInFieldOfView( subject.WorldSpaceBounds.Center ) )
			return true;

		return IsInFieldOfView( subject.EyePosition );
	}

	public bool IsInFieldOfView( Vector3 position )
	{
		return PointWithinViewAngle( Bot.EyePosition, position, Bot.ViewVector, CosHalfFOV );
	}

	public bool IsLineOfSightClear( Entity subject, bool cheaper = false )
	{
		var eyePos = Bot.EyePosition;

		// Try tracing to world space center
		var tr = SetupLineOfSightTrace( eyePos, subject.WorldSpaceBounds.Center )
			.Ignore( subject )
			.Run();

		if ( cheaper && tr.Hit )
			return false;

		// We hit something? Try tracing to eye position.
		if ( tr.Hit )
		{
			tr = SetupLineOfSightTrace( eyePos, subject.EyePosition )
				.Ignore( subject )
				.Run();

			// We hit something? Try tracing to abs position.
			if ( tr.Hit )
			{
				tr = SetupLineOfSightTrace( eyePos, subject.Position )
					.Ignore( subject )
					.Run();

				// We failed.
				if ( tr.Hit )
					return false;
			}
		}

		return true;
	}

	public virtual Trace SetupLineOfSightTrace( Vector3 origin, Vector3 end, bool ignoreActors = true )
	{
		var tr = Trace.Ray( origin, end )
				.Ignore( Bot.Entity );

		if ( ignoreActors )
			tr = tr.WithoutTags( NextBots.CollisionTag );

		return tr;
	}

	public bool IsLineOfSightClear( Vector3 point )
	{
		// Try tracing to world space center
		var tr = Trace.Ray( Bot.EyePosition, point )
			.Ignore( Bot.Entity )
			.Run();

		return !tr.Hit;
	}

	public bool PointWithinViewAngle( Vector3 origin, Vector3 target, Vector3 direction, float cosHalfFov )
	{
		var vecDelta = target - origin;
		float cosDiff = direction.Dot( vecDelta );

		if ( cosDiff < 0 )
			return false;

		float flLen2 = vecDelta.LengthSquared;

		// a/sqrt(b) > c  == a^2 > b * c ^2
		return cosDiff * cosDiff > flLen2 * cosHalfFov * cosHalfFov;
	}

	public IEnumerable<Entity> CollectPotentiallyVisibleEntities()
	{
		// Notice player pawns.
		var pawns = Client.All.Select( x => x.Pawn );
		foreach ( var pawn in pawns )
			yield return pawn;

		// Notice INextBots
		var bots = Entity.All.OfType<INextBot>().OfType<Entity>();
		foreach ( var bot in bots )
			yield return bot;
	}

	public IEnumerable<Entity> CollectVisibleEntities()
	{
		var potentiallyVisible = CollectPotentiallyVisibleEntities();

		foreach ( var entity in potentiallyVisible )
		{
			if ( entity == null || !entity.IsValid )
				continue;

			if ( IsIgnored( entity ) )
				continue;

			if ( entity.LifeState != LifeState.Alive )
				continue;

			if ( entity == Bot )
				continue;

			if ( !IsAbleToSee( entity ) )
				continue;

			yield return entity;
		}
	}

	public virtual void UpdateKnownEntities()
	{
		var visibleNow = CollectVisibleEntities().ToArray();

		// check for new recognizes that were not in the known set
		for ( var i = 0; i < visibleNow.Length; i++ )
		{
			int j;
			for ( j = 0; j < KnownEntities.Count; j++ )
			{
				if ( visibleNow[i] == KnownEntities[j].Entity )
					break;
			}

			if ( j == KnownEntities.Count )
			{
				var known = new KnownEntity( visibleNow[i] );
				known.UpdatePosition();
				known.UpdateVisibilityStatus( true );
				_knownEntities.Add( known );
			}
		}

		for ( var i = 0; i < KnownEntities.Count; i++ )
		{
			var known = KnownEntities[i];

			// clear out obsolete knowledge
			if ( known.IsObsolete() )
			{
				_knownEntities.RemoveAt( i );
				i--;
				continue;
			}

			if ( visibleNow.Contains( known.Entity ) )
			{
				known.UpdatePosition();
				known.UpdateVisibilityStatus( true );

				if ( Time.Now - known.WhenLastBecameVisible >= GetMinRecognizeTime() && 
					LastVisionUpdateTime - known.WhenLastBecameVisible < GetMinRecognizeTime() )
				{
					if ( NextBots.IsDebugging( NextBotDebugFlags.Vision ) )
					{
						NextBots.Msg( NextBotDebugFlags.Vision, $"[NextBot Vision] {Bot} caught sight of {known.Entity}" );
						DebugOverlay.Line( Bot.EyePosition, known.LastKnownPosition, Color.Yellow, 0.2f, false );
					}

					Bot.NextBot.InvokeEvent( new NextBotEventSight { Entity = known.Entity } );
				}
			}
			else
			{
				if ( known.IsVisible )
				{
					known.UpdateVisibilityStatus( false );
					NextBots.Msg( NextBotDebugFlags.Vision, $"[NextBot Vision] {Bot} lost sight of {known.Entity}" );

					Bot.NextBot.InvokeEvent( new NextBotEventLostSight { Entity = known.Entity } );
				}

				if ( !known.HasLastKnownPositionBeforeSeen )
				{
					if ( IsAbleToSee( known.LastKnownPosition ) )
					{
						known.HasLastKnownPositionBeforeSeen = true;
					}
				}
			}
		}

		if ( NextBots.IsDebugging( NextBotDebugFlags.Vision ) )
		{
			foreach ( var known in KnownEntities )
			{
				var pos = known.LastKnownPosition;
				var color = known.IsVisible ? Color.Green : Color.Yellow;

				DebugOverlay.Line( Bot.EyePosition, pos, color, 0.1f );
				DebugOverlay.Sphere( pos, 5, color, 0.1f );
			}
		}
	}

	public virtual bool IsLookingAt( Vector3 pos, float cosTolerance = 0.95f )
	{
		var toPos = pos - Bot.EyePosition;
		toPos = toPos.Normal;

		var forward = Bot.ViewVector;

		return toPos.Dot( forward ) > cosTolerance;
	}

	public virtual bool IsLookingAt( Entity pos, float cosTolerance = 0.95f )
	{
		return IsLookingAt( pos, cosTolerance );
	}

	public virtual void AlertOfEntity( Entity entity )
	{
		// We cant remember this 
		if ( entity == null || !entity.IsValid || entity.IsWorld )
			return;

		// If we already know this entity, just update its position.
		var known = _knownEntities.Find( x => x.Entity == entity );
		if ( known != null )
		{
			known.UpdatePosition();
			return;
		}

		// Otherwise add it to the list.
		_knownEntities.Add( new KnownEntity( entity ) );
	}

	public virtual void ForgetEntity( Entity entity )
	{
		for ( var i = 0; i < KnownEntities.Count; i++ )
		{
			var known = KnownEntities[i];

			if ( known.Entity == entity ) 
			{
				_knownEntities.RemoveAt( i );
				return;
			}
		}
	}

	public virtual void ForgetAllKnownEntities()
	{
		_knownEntities.Clear();
	}

	public virtual KnownEntity GetPrimaryKnownThreat( bool onlyVisibleThreats = false )
	{
		if ( !KnownEntities.Any() )
			return null;

		KnownEntity threat = null;

		// find the first valid entity
		foreach ( var firstThreat in KnownEntities )
		{
			// check in case status changes between updates
			if ( IsAwareOf( firstThreat ) && !firstThreat.IsObsolete() && !IsIgnored( firstThreat.Entity ) && Bot.NextBot.IsEnemy( firstThreat.Entity ) )
			{
				if ( !onlyVisibleThreats || firstThreat.IsVisibleRecently() )
				{
					threat = firstThreat;
					break;
				}
			}
		}

		if ( threat == null )
		{
			PrimaryThreat = null;
			return null;
		}

		// find the first valid entity
		foreach ( var newThreat in KnownEntities )
		{
			// check in case status changes between updates
			if ( IsAwareOf( newThreat ) && !newThreat.IsObsolete() && !IsIgnored( newThreat.Entity ) && Bot.NextBot.IsEnemy( newThreat.Entity ) )
			{
				if ( !onlyVisibleThreats || newThreat.IsVisibleRecently() )
				{
					threat = Bot.NextBot.InvokeQuery( new NextBotQuerySelectMoreDangerousThreat( newThreat, threat ) );
				}
			}
		}

		PrimaryThreat = threat?.Entity;
		return threat;
	}

	[ConVar.Server] public static bool nb_blind { get; set; } = false;
}

public delegate void NextboTCringe<T, U>( T args, ref U retval ) where T : NextBotContextualQuery<U>;
