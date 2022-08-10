using System;

namespace Amper.NextBot;

partial class NextBotController
{
	public T InvokeQuery<T>( NextBotContextualQuery<T> args )
	{
		var preAnswer = args.OnPreInvocation();
		if ( !Equals( preAnswer, default( T ) ) )
			return preAnswer;

		var answer = InvokeQuery( this, args );
		if ( !Equals( answer, default( T ) ) )
			return answer;

		var postAnswer = args.OnPostInvocation();
		if ( !Equals( postAnswer, default( T ) ) )
			return postAnswer;

		return default;
	}

	private T InvokeQuery<T>( INextBotEventResponder parent, NextBotContextualQuery<T> args )
	{
		var answer = parent.OnQuery( args );
		if ( !Equals( answer, default( T ) ) ) 
			return answer;

		for ( var sub = parent.FirstContainedResponder(); sub != null; sub = parent.NextContainedResponder( sub ) )
		{
			var containedAnswer = InvokeQuery( sub, args );
			if ( !Equals( containedAnswer, default( T ) ) )
				return containedAnswer;
		}

		return default;
	}
}

public interface INextBotContextualQuery { }

public abstract class NextBotContextualQuery<T> : INextBotContextualQuery
{
	public virtual T OnPreInvocation() => default( T );
	public virtual T OnPostInvocation() => default( T );
}

public enum QueryResultType 
{
	Undefined,
	No,
	Yes
}
