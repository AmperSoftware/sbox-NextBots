using Sandbox;

namespace Amper.NextBot;

public partial class NextBotController : IValid
{
	public INextBot Bot { get; private set; }

	public NextBotComponent FirstComponent { get; set; }
	public INextBotLocomotion Locomotion { get; set; }
	public INextBotIntention Intention { get; set; }
	public INextBotVision Vision { get; set; }
	public INextBotBody Body { get; set; }

	public NextBotPathFollower Path { get; set; }

	public float LastUpdateTime { get; set; }
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

	public void Reset()
	{
		LastUpdateTime = -999;
		DisplayDebugLine = 0;

		// update all components
		for ( var comp = FirstComponent; comp != null; comp = comp.NextComponent )
			comp.Reset();
	}

	public void DisplayDebugText( string text )
	{
		if ( Bot is not Entity entity )
			return;

		var origin = entity.WorldSpaceBounds.Center;
		DebugOverlay.Text( text, origin, DisplayDebugLine++, Color.White, 0.1f );
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

	public bool IsValid => Bot != null && Bot.IsValid;
}
