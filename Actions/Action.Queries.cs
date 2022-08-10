using System;
using System.Collections.Generic;

namespace Amper.NextBot;

partial class NextBotAction<T> 
{
	Dictionary<Type, NextBotActionQueryDelegate<T>> QuerySubscriptions = new();

	public ResponseType OnQuery<ResponseType>( NextBotContextualQuery<ResponseType> args )
	{
		if ( !IsStarted )
			return default;

		// Log
		var action = this;
		ResponseType result = default;

		while ( action != null )
		{
			result = action.InvokeQueryDelegate( args );
			if ( !Equals( result, default( ResponseType ) ) ) 
				break;

			action = action.BuriedUnderMe;
		}

		return result;
	}

	protected ResponseType InvokeQueryDelegate<ResponseType>( NextBotContextualQuery<ResponseType> args )
	{
		// Log
		var typeHint = args.GetType();

		if ( QuerySubscriptions.TryGetValue( typeHint, out var result ) )
			return (ResponseType)result.Invoke( Actor, args );

		return default;
	}

	public void SubscribeToContextualQuery<Query, ResponseType>( NextBotActionQueryCallback<T, Query, ResponseType> args ) where Query : NextBotContextualQuery<ResponseType>
	{
		var type = typeof( Query );
		QuerySubscriptions[type] = ( me, c ) => args( me, (Query)c );
	}
}

public delegate object NextBotActionQueryDelegate<T>( T me, INextBotContextualQuery args ) where T : INextBot;
public delegate R NextBotActionQueryCallback<T, U, R>( T me, U args ) where T : INextBot where U : INextBotContextualQuery;
