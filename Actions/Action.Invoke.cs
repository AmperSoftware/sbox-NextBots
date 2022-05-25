namespace Amper.NextBot;

partial class NextBotAction<T> 
{
	public ActionResult<T> InvokeOnStart( T me, NextBotAction<T> priorAction, NextBotAction<T> buriedUnderMe )
	{
		// these value must be valid before invoking OnStart, in case an OnSuspend happens 
		IsStarted = true;
		Actor = me;

		// maintain parent/child relationship during transitions
		if ( priorAction != null )
			Parent = priorAction.Parent;

		if ( Parent != null )
			Parent.Child = this;

		// maintain stack pointers
		BuriedUnderMe = buriedUnderMe;
		if ( buriedUnderMe != null )
			buriedUnderMe.CoveringMe = this;

		// we are always on top of the stack. if our priorAction was buried, it cleared 
		// everything covering it when it ended (which happens before we start)
		CoveringMe = null;

		// start the optional child action
		Child = InitialContainedAction( me );
		if ( Child != null ) 
		{
			// define initial parent/child relationship
			Child.Parent = this;
			Child = Child.ApplyResult( me, ChangeTo( Child, "Starting child Action" ) );
		}

		return OnStart( me, priorAction );
	}

	public ActionResult<T> InvokeUpdate( T me, float interval )
	{
		if ( IsOutOfScope() ) 
		{
			return Done( "Out of scope" );
		}

		if ( !IsStarted )
		{
			return ChangeTo( this, "Starting Action" );
		}

		// honor any pending event results 
		var eventResult = ProcessPendingEvents();
		if ( !eventResult.IsContinue )
		{
			return eventResult;
		}

		// update our child action first, since it has the most specific behavior
		if ( Child != null )
		{
			Child = Child.ApplyResult( me, Child.InvokeUpdate( me, interval ) );
		}

		// update ourselves
		return Update( me, interval );
	}

	public void InvokeOnEnd( T me, NextBotAction<T> nextAction )
	{
		// We are not started (or never were)
		if ( !IsStarted )
			return;

		// we are no longer started
		IsStarted = false;

		// tell child Action(s) to leave (but don't disturb the list itself)
		NextBotAction<T> next;
		for ( var child = Child; child != null; child = next )
		{
			next = child.BuriedUnderMe;
			child.InvokeOnEnd( me, nextAction );
		}

		// leave ourself
		OnEnd( me, nextAction );

		// leave any Actions stacked on top of me
		if ( CoveringMe != null ) 
		{
			CoveringMe.InvokeOnEnd( me, nextAction );
		}
	}

	public NextBotAction<T> InvokeOnSuspend( T me, NextBotAction<T> interruptingAction )
	{
		// suspend child Action
		if ( Child != null ) 
		{
			Child = Child.InvokeOnSuspend( me, interruptingAction );
		}

		IsSuspended = true;
		var result = OnSuspend( me, interruptingAction );

		if ( result.IsDone )
		{
			// we want to be replaced instead of suspended
			InvokeOnEnd( me, null );

			// new Action on top of the stack
			return BuriedUnderMe;
		}

		// we are still on top of the stack at this moment
		return this;
	}

	public ActionResult<T> InvokeOnResume( T me, NextBotAction<T> interruptingAction )
	{
		if ( !IsSuspended )
		{
			// we were never suspended
			return Continue();
		}

		if ( EventResult.IsRequestingChange )
		{
			// this Action is not actually being Resumed, because a change
			// is already pending from a prior event
			return Continue();
		}

		IsSuspended = false;
		CoveringMe = null;

		if ( Parent != null )
		{
			// we are once again our parent's active child
			Parent.Child = this;
		}


		// update our child action first, since it has the most specific behavior
		if ( Child != null )
		{
			Child = Child.ApplyResult( me, Child.InvokeOnResume( me, interruptingAction ) );
		}

		// update ourselves
		return OnResume( me, interruptingAction );
	}
}
