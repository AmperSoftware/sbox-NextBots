using System;
using System.Collections.Generic;

namespace Amper.NextBot;

public abstract partial class NextBotComponent
{
	Dictionary<Type, NextBotQuerySubscription> QuerySubscriptions = new();

	public void SubscribeToContextualQuery<T, U>( Func<T, U> args ) where T : NextBotContextualQuery<U>
	{
		var type = typeof( T );
		QuerySubscriptions[type] = ( INextBotContextualQuery c ) => args( (T)c );
	}
		
	public ResponseType OnQuery<ResponseType>( NextBotContextualQuery<ResponseType> args )
	{
		var typeHint = args.GetType();

		// invoke all subscriptions.
		if ( QuerySubscriptions.TryGetValue( typeHint, out var result ) )
			return (ResponseType)result.Invoke( args );

		return default;
	}
}

public delegate object NextBotQuerySubscription( INextBotContextualQuery query );
