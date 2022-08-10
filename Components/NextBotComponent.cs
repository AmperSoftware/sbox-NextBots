using Sandbox;
using System;

namespace Amper.NextBot;

public interface INextBotComponent
{
	public void Upkeep();
	public void Update();
	public void Reset();
}

public abstract partial class NextBotComponent : INextBotComponent, INextBotEventResponder
{
	public float LastUpdateTime { get; protected set; }
	public float CurrentInterval { get; protected set; }

	public NextBotController Controller { get; set; }
	public INextBot Bot => Controller?.Bot;

	public NextBotComponent NextComponent { get; set; }

	public NextBotComponent( INextBot me )
	{
		if ( me.NextBot == null )
		{
			Log.Error( "<NextBot> Attempted to add a component to a NextBot without a controller." );
			return;
		}

		Controller = me.NextBot;

		LastUpdateTime = 0;
		CurrentInterval = Global.TickInterval;

		Controller.RegisterComponent( this );
	}

	public bool ComputeUpdateInterval()
	{
		if ( LastUpdateTime > 0 )
		{
			var interval = Time.Now - LastUpdateTime;

			var minInterval = 0.0001f;
			if ( interval > minInterval )
			{
				CurrentInterval = interval;
				LastUpdateTime = Time.Now;

				return true;
			}

			return false;
		}

		// First update - assume a reasonable interval.
		// We need the very first update to do work, for cases
		// where the bot was just created and we need to propagate
		// an event to it immediately.
		CurrentInterval = 0.033f;
		LastUpdateTime = Time.Now - CurrentInterval;

		return true;
	}

	public virtual void Upkeep() { }
	public virtual void Update() { }
	public virtual void Reset() { }

	public virtual INextBotEventResponder FirstContainedResponder() => null;
	public virtual INextBotEventResponder NextContainedResponder( INextBotEventResponder current ) => null;
}
