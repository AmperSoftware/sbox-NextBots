using System;
using System.Collections.Generic;

namespace Amper.NextBot;

partial class NextBotAction<T> 
{
	Dictionary<Type, NextBotActionEventDelegate<T>> EventSubscriptions = new();

	public void OnEvent( NextBotEvent args )
	{
		if ( !IsStarted )
			return;

		var action = this;
		EventDesiredResult<T> result = null;

		while ( action != null )
		{
			result = action.InvokeEventDelegate( args );
			if ( result != null && !result.IsContinue )
				break;

			action = action.BuriedUnderMe;
		}

		if ( action != null && result != null ) 
		{
			NextBots.Msg( NextBotDebugFlags.Events, $"[NextBot] {action} responded to event {args} with {result.Type} ({result.Reason})" );
			StorePendingEventResult( result, args );
		}
	}

	/// <summary>
	/// Set the callback of a nextbot event to some function. Can be only one per event!
	/// </summary>
	public void SubscribeToEvent<U>( NextBotActionEventCallback<T, U> callback ) where U: NextBotEvent
	{
		var typeHint = typeof( U );
		EventSubscriptions[typeHint] = ( me, args ) => callback( me, (U)args );
	}

	protected EventDesiredResult<T> InvokeEventDelegate( NextBotEvent args )
	{
		// Log
		NextBots.Msg( NextBotDebugFlags.Events, $"[NextBot] {Actor} {this} has received event {args}" );

		var typeHint = args.GetType();

		if ( EventSubscriptions.TryGetValue( typeHint, out var result ) )
			return result.Invoke( Actor, args );

		return TryContinue();
	}

	EventDesiredResult<T> EventResult = null;

	public void StorePendingEventResult( EventDesiredResult<T> result, NextBotEvent args )
	{
		if ( result.IsContinue )
			return;

		if ( result.Priority >= EventResult.Priority )
		{
			// New result is as important or more important.

			// If we're critical, throw an error. This wont prevent from overriding this event, but 
			// at least we are warned that the collision happened.
			if ( EventResult.Priority == EventResultPriorityType.Critical )
				NextBots.MsgError( NextBotDebugFlags.Events, $"[NextBot] {Actor} {this} {args} - critical events collision!" );

			EventResult = result;
		}
	}

	public ActionResult<T> ProcessPendingEvents()
	{
		// if an event has requested a change, honor it
		if ( EventResult.IsRequestingChange ) 
		{
			var result = new ActionResult<T>( EventResult.Type, EventResult.Action, EventResult.Reason );

			// clear event result in case this change is a suspend and we later resume this action
			EventResult = TryContinue( EventResultPriorityType.None );

			return result;
		}

		// check for pending event changes buried in the stack
		var under = BuriedUnderMe;
		while ( under != null ) 
		{
			if ( under.EventResult.IsSuspendFor ) 
			{
				// process this pending event in-place and push new Action on the top of the stack
				var result = new ActionResult<T>( under.EventResult.Type, under.EventResult.Action, under.EventResult.Reason );

				// clear event result in case this change is a suspend and we later resume this action
				under.EventResult = TryContinue( EventResultPriorityType.None );

				return result;
			}	

			under = under.BuriedUnderMe;
		}

		return Continue();
	}
}

public delegate EventDesiredResult<T> NextBotActionEventDelegate<T>( T me, NextBotEvent args ) where T : INextBot;
public delegate EventDesiredResult<T> NextBotActionEventCallback<T, U>( T me, U args ) where T : INextBot where U : NextBotEvent;
