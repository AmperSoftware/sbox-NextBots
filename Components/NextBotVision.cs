﻿using System.Collections.Generic;
using Sandbox;
using System;
using System.Linq;

namespace Amper.NextBot;

public interface INextBotVision
{
	public float GetDefaulFieldOfView();
}

public class NextBotVision : NextBotComponent, INextBotVision
{

	List<KnownEntity> KnownEntities { get; set; } = new();
	float LastVisionUpdateTime { get; set; } = 0;
	Entity PrimaryThreat { get; set; }
	public float FOV { get; private set; }
	float CosHalfFOV { get; set; }

	public NextBotVision( INextBot bot ) : base( bot )
	{
		Reset();
	}

	public override void Reset()
	{
		base.Reset();

		KnownEntities.Clear();
		LastVisionUpdateTime = 0;
		PrimaryThreat = null;

		SetFieldOfView( GetDefaulFieldOfView() );
	}

	public override void Update()
	{
		if( nb_blind )
		{
			KnownEntities.Clear();
			return;
		}

		UpdateKnownEntities();
		LastVisionUpdateTime = Time.Now;

		if ( NextBots.IsDebugging( NextBotDebugFlags.Vision ) )
		{
			Bot.NextBot.DisplayDebugText( "Vision:" );

			Bot.NextBot.DisplayDebugText( "- Known Entities: " );
			foreach ( var known in KnownEntities )
			{
				Bot.NextBot.DisplayDebugText( $"  - {known.Entity} (visible: {known.IsVisible})" );
			}
		}
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
	public virtual float GetDefaulFieldOfView() => 90;
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
		return PointWithinViewAngle( Bot.NextBot.Body.GetEyePosition(), position, Bot.NextBot.Body.GetViewVector(), CosHalfFOV );
	}

	public bool IsLineOfSightClear( Entity subject, bool cheaper = false )
	{
		// Try tracing to world space center
		var tr = Trace.Ray( Bot.NextBot.Body.GetEyePosition(), subject.WorldSpaceBounds.Center )
			.Ignore( Bot.Entity )
			.Ignore( subject )
			.Run();

		if ( cheaper && tr.Hit )
			return false;

		// We hit something? Try tracing to eye position.
		if ( tr.Hit )
		{
			tr = Trace.Ray( Bot.NextBot.Body.GetEyePosition(), subject.EyePosition )
				.Ignore( Bot.Entity )
				.Ignore( subject )
				.Run();

			// We hit something? Try tracing to abs position.
			if ( tr.Hit )
			{
				tr = Trace.Ray( Bot.NextBot.Body.GetEyePosition(), subject.Position )
					.Ignore( Bot.Entity )
					.Ignore( subject )
					.Run();

				// We failed.
				if ( tr.Hit )
					return false;
			}
		}

		return true;
	}

	public bool IsLineOfSightClear( Vector3 point, bool cheaper = false )
	{
		// Try tracing to world space center
		var tr = Trace.Ray( Bot.NextBot.Body.GetEyePosition(), point )
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
		var players = Entity.All.OfType<Player>();
		foreach ( var player in players )
			yield return player;

		// Notice NextBots
		foreach ( var bot in NextBots.Current.Bots )
			yield return bot.Bot.Entity;
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
				KnownEntities.Add( known );
			}
		}

		for ( var i = 0; i < KnownEntities.Count; i++ )
		{
			var known = KnownEntities[i];

			// clear out obsolete knowledge
			if ( known.IsObsolete() )
			{
				KnownEntities.RemoveAt( i );
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
						DebugOverlay.Line( Bot.NextBot.Body.GetEyePosition(), known.LastKnownPosition, Color.Yellow, 0.2f, false );
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

				DebugOverlay.Line( Bot.NextBot.Body.GetEyePosition(), pos, color, 0.1f );
				DebugOverlay.Sphere( pos, 5, color, 0.1f );
			}
		}
	}

	[ConVar.Server] public static bool nb_blind { get; set; } = false;
}
