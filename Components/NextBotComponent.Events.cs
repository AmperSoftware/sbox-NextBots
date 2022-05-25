using System;
using System.Collections.Generic;

namespace Amper.NextBot;

public abstract partial class NextBotComponent : INextBotEventReceiver
{
	public Dictionary<Type, NextBotComponentEventDelegate> EventSubscriptions = new();

	public void SubscribeToEvent<T>( NextBotComponentEventDelegate callback ) where T : NextBotEvent
	{
		Type typeHint = typeof( T );
		EventSubscriptions[typeHint] = callback;
	}

	public virtual void OnEvent( NextBotEvent args )
	{
		var typeHint = args.GetType();

		// invoke all subscriptions.
		if ( EventSubscriptions.TryGetValue( typeHint, out var result ) )
			result.Invoke( args );
	}
}


public delegate void NextBotComponentEventDelegate( NextBotEvent args );

public interface INextBotEventReceiver
{
	public void OnEvent( NextBotEvent args );
}
