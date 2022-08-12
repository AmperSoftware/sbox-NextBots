using Sandbox;
using System;

namespace Amper.NextBot;

/// <summary>
/// This is the mind of our nextbot, it manages all the components.
/// </summary>
public partial class NextBotController : IValid, INextBotEventResponder
{
	public INextBot Bot { get; private set; }
	public const string CollisionTag = "nextbot";

	public NextBotComponent FirstComponent { get; set; }

	public INextBotLocomotion Locomotion { get; set; }
	public INextBotIntention Intention { get; set; }
	public INextBotVision Vision { get; set; }
	public INextBotAnimator Animator { get; set; }

	public NextBotPathFollower Path { get; set; }

	float LastUpdateTime { get; set; }
	int DisplayDebugLine { get; set; }

	public NextBotController( INextBot bot )
	{
		Bot = bot;
		Bot.NextBot = this;

		NextBots.Register( this );
	}

	public void RegisterComponent( NextBotComponent component )
	{
		component.NextComponent = FirstComponent;
		FirstComponent = component;
	}

	public void Update()
	{
		if ( Time.Now < (LastUpdateTime + NextBots.nb_update_frequency) )
			return;

		ReleaseExpiredButtons();
		DisplayDebugLine = 0;

		// update all components
		for ( var comp = FirstComponent; comp != null; comp = comp.NextComponent )
		{
			if ( comp.ComputeUpdateInterval() )
				comp.Update();
		}

		LastUpdateTime = Time.Now;
	}

	public void Upkeep()
	{
		// upkeep all components
		for ( var comp = FirstComponent; comp != null; comp = comp.NextComponent )
		{
			comp.Upkeep();
		}
	}

	public void Reset()
	{
		LastUpdateTime = -999;
		DisplayDebugLine = 0;
		Bot.Entity?.Tags.Add( CollisionTag );

		// update all components
		for ( var comp = FirstComponent; comp != null; comp = comp.NextComponent )
			comp.Reset();
	}

	public void DisplayDebugText( string text )
	{
		if ( Bot is not Entity entity )
			return;

		var origin = entity.WorldSpaceBounds.Center;
		DebugOverlay.Text( text, origin, DisplayDebugLine++, Color.White, NextBots.nb_update_frequency );
	}

	public bool IsRangeLessThan( Entity subject, float range )
	{
		return IsRangeLessThan( subject.Position, range );
	}

	public bool IsRangeLessThan( Vector3 pos, float range )
	{
		return GetRangeTo( pos ) <= range;
	}

	public bool IsRangeGreaterThan( Entity subject, float range )
	{
		return IsRangeGreaterThan( subject.Position, range );
	}

	public bool IsRangeGreaterThan( Vector3 pos, float range )
	{
		return GetRangeTo( pos ) >= range;
	}

	public float GetRangeTo( Entity subject )
	{
		return GetRangeTo( subject.Position );
	}

	public float GetRangeTo( Vector3 pos )
	{
		return Bot.Position.Distance( pos );
	}

	public virtual bool IsEnemy( Entity entity ) => false;
	public virtual bool IsFriend( Entity entity ) => !IsEnemy( entity );

	public INextBotEventResponder FirstContainedResponder() => FirstComponent;
	public INextBotEventResponder NextContainedResponder( INextBotEventResponder current ) => ((NextBotComponent)current).NextComponent;

	public void OnEvent( NextBotEvent args ) { }
	public ResponseType OnQuery<ResponseType>( NextBotContextualQuery<ResponseType> args ) => default;

	public bool IsValid => Bot != null && Bot.IsValid;
}
