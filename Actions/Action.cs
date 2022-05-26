namespace Amper.NextBot;

public partial class NextBotAction<T> where T : INextBot
{
	/// <summary>
	/// The action that we are currently suspending.
	/// </summary>
	public NextBotAction<T> BuriedUnderMe { get; set; }
	/// <summary>
	/// The action that is suspending us.
	/// </summary>
	public NextBotAction<T> CoveringMe { get; set; }

	/// <summary>
	/// Action that created us by calling <see cref="InitialContainedAction(T)"/>.
	/// </summary>
	public NextBotAction<T> Parent { get; set; }
	/// <summary>
	/// The action we have created using <see cref="InitialContainedAction(T)"/>
	/// </summary>
	public NextBotAction<T> Child { get; set; }

	/// <summary>
	/// Has this action been started?
	/// </summary>
	public bool IsStarted { get; private set; }
	/// <summary>
	/// Is this action currently suspended by some other action?
	/// </summary>
	public bool IsSuspended { get; private set; }

	/// <summary>
	/// Active means that action is both started and not suspended by any other entity.
	/// </summary>
	public bool IsActive => IsStarted && !IsSuspended;

	public NextBotAction()
	{
		EventResult = TryContinue( EventResultPriorityType.None );

		SubscribeToEvent<NextBotEventKilled>( OnKilled );
		SubscribeToEvent<NextBotEventInjured>( OnInjured );

		SubscribeToEvent<NextBotEventStuck>( OnStuck );
		SubscribeToEvent<NextBotEventUnStuck>( OnUnstuck );

		SubscribeToEvent<NextBotEventMoveToSuccess>( OnMoveToSuccess );
		SubscribeToEvent<NextBotEventMoveToFailure>( OnMoveToFailure );

		SubscribeToEvent<NextBotEventSight>( OnSight );
		SubscribeToEvent<NextBotEventLostSight>( OnLostSight );

		SubscribeToEvent<NextBotEventLeaveGround>( OnLeaveGround );
		SubscribeToEvent<NextBotEventLandOnGround>( OnLandOnGround );
	}

	T Actor { get; set; }

	/// <summary>
	/// This method is used to create contained actions that will act as our children. They will not suspend us and run independently from us, but will be stopped if this action gets changed to something else.
	/// </summary>
	public virtual NextBotAction<T> InitialContainedAction( T me ) { return null; }

	/// <summary>
	/// The action has just been started. If you change or suspend this action here, <see cref="Update(T, float)"/> will never be called.
	/// </summary>
	public virtual ActionResult<T> OnStart( T me, NextBotAction<T> priorAction = null ) { return Continue(); }
	/// <summary>
	/// Do the work of the Action. It is possible for Update to not be 
	/// called between a given OnStart / OnEnd pair due to immediate transitions.
	/// </summary>
	public virtual ActionResult<T> Update( T me, float interval ) { return Continue(); }
	/// <summary>
	/// Invoked when an Action is ended for any reason.
	/// </summary>
	public virtual void OnEnd( T me, NextBotAction<T> nextAction ) { }

	/// <summary>
	/// When an Action is suspended by a new action.
	/// </summary>
	public virtual ActionResult<T> OnSuspend( T me, NextBotAction<T> interruptingAction ) { return Continue(); }
	/// <summary>
	/// When an Action is resumed after being suspended
	/// </summary>
	public virtual ActionResult<T> OnResume( T me, NextBotAction<T> interruptingAction ) { return Continue(); }

	/// <summary>
	/// Keep doing what we're doing. No change is needed! This is the default option.
	/// </summary>
	public ActionResult<T> Continue()
	{
		return new ActionResult<T>( ActionResultType.Continue );
	}
	/// <summary>
	/// Change current action to anything else. This will stop our current action and all its children.
	/// </summary>
	public ActionResult<T> ChangeTo( NextBotAction<T> action, string reason = "" )
	{
		return new ActionResult<T>( ActionResultType.ChangeTo, action, reason );
	}
	/// <summary>
	/// Change current action to anything else. This will stop our current action and all its children.
	/// </summary>
	public ActionResult<T> ChangeTo<U>( string reason = "" ) where U : NextBotAction<T>, new()
	{
		return ChangeTo( new U(), reason );
	}
	/// <summary>
	/// Suspend this action for any other action. This will not affect any children actions.
	/// </summary>
	public ActionResult<T> SuspendFor( NextBotAction<T> action, string reason = "" )
	{
		// clear any pending transitions requested by events, or this SuspendFor will
		// immediately be out of scope
		EventResult = TryContinue( EventResultPriorityType.None );

		return new ActionResult<T>( ActionResultType.SuspendFor, action, reason );
	}
	/// <summary>
	/// Suspend this action for any other action. This will not affect any children actions.
	/// </summary>
	public ActionResult<T> SuspendFor<U>( string reason = "" ) where U : NextBotAction<T>, new()
	{
		return SuspendFor( new U(), reason );
	}
	/// <summary>
	/// We are done this this action. Stop it, and resume whatever action we suspended if there are any.
	/// </summary>
	public ActionResult<T> Done( string reason = "" )
	{
		return new ActionResult<T>( ActionResultType.Done, null, reason );
	}

	/// <summary>
	/// We have received an event, and we don't want to change anything. Leave everything as it is right now. This is the default option.
	/// </summary>
	public EventDesiredResult<T> TryContinue( EventResultPriorityType priority = EventResultPriorityType.Try )
	{
		return new EventDesiredResult<T>( ActionResultType.Continue, null, priority );
	}

	/// <summary>
	/// We have received an event, and we want to try to change our action to something else.
	/// </summary>
	public EventDesiredResult<T> TryChangeTo( NextBotAction<T> action, EventResultPriorityType priority = EventResultPriorityType.Try, string reason = "" )
	{
		return new EventDesiredResult<T>( ActionResultType.ChangeTo, action, priority, reason );
	}	
	/// <summary>
	/// We have received an event, and we want to try to change our action to something else.
	/// </summary>
	public EventDesiredResult<T> TryChangeTo<U>( EventResultPriorityType priority = EventResultPriorityType.Try, string reason = "" ) where U : NextBotAction<T>, new()
	{
		return TryChangeTo( new U(), priority, reason );
	}

	/// <summary>
	/// We have received an event, and we want to try to suspend our action with something else.
	/// </summary>
	public EventDesiredResult<T> TrySuspendFor( NextBotAction<T> action, EventResultPriorityType priority = EventResultPriorityType.Try, string reason = "" )
	{
		return new EventDesiredResult<T>( ActionResultType.SuspendFor, action, priority, reason );
	}
	/// <summary>
	/// We have received an event, and we want to try to suspend our action with something else.
	/// </summary>
	public EventDesiredResult<T> TrySuspendFor<U>( EventResultPriorityType priority = EventResultPriorityType.Try, string reason = "" ) where U : NextBotAction<T>, new()
	{
		return TrySuspendFor( new U(), priority, reason );
	}

	/// <summary>
	/// We have received an event, and we want to end this action, and resume our suspended action if it exists.
	/// </summary>
	public EventDesiredResult<T> TryDone( EventResultPriorityType priority = EventResultPriorityType.Try, string reason = "" )
	{
		return new EventDesiredResult<T>( ActionResultType.Done, null, priority, reason );
	}

	/// <summary>
	/// We have received an event, and want to block any native event's result. This is same as "Continue" except it has priority over native results.
	/// </summary>
	public EventDesiredResult<T> TryToSustain( EventResultPriorityType priority = EventResultPriorityType.Try, string reason = "" )
	{
		return new EventDesiredResult<T>( ActionResultType.Sustain, null, priority, reason );
	}

	public NextBotAction<T> ApplyResult( T me, ActionResult<T> result )
	{
		if ( result == null )
			return this;

		var newAction = result.Action;
		switch( result.Type )
		{
			case ActionResultType.ChangeTo:
				{
					//
					// Change to a new action.
					//

					if ( newAction == null )
					{
						Log.Error( "<NextBot> Error: Attempted ChangeTo to a NULL action" );
						return this;
					}

					// we are done.
					InvokeOnEnd( me, newAction );

					// start new event.
					var startResult = newAction.InvokeOnStart( me, this, BuriedUnderMe );

					NextBots.Msg( NextBotDebugFlags.Behavior, $"<NextBot> Changed: {this} → {newAction} ({result.Reason})" );

					// apply result of starting the Action
					return newAction.ApplyResult( me, startResult );
				}

			// temporarily suspend ourselves for the newAction, covering it on the stack
			case ActionResultType.SuspendFor:
				{
					// interrupting Action always goes on the TOP of the stack - find it
					var topAction = this;
					while ( topAction.CoveringMe != null ) 
					{
						topAction = topAction.CoveringMe;
					}

					topAction = topAction.InvokeOnSuspend( me, newAction );

					// start new action.
					var startResult = newAction.InvokeOnStart( me, topAction, topAction );

					NextBots.Msg( NextBotDebugFlags.Behavior, $"<NextBot> Suspended: {this} for {newAction} ({result.Reason})" );
					return newAction.ApplyResult( me, startResult );
				}

			// resume buried action
			case ActionResultType.Done:
				{
					var resumedAction = BuriedUnderMe;
					InvokeOnEnd( me, resumedAction );

					if ( resumedAction == null )
					{
						// all Actions complete
						return null;
					}

					// resume ancestor.
					var resumeResult = resumedAction.InvokeOnResume( me, this );

					// log
					NextBots.Msg( NextBotDebugFlags.Behavior, $"<NextBot> Resumed: {resumedAction} (interrupted by {this}) ({result.Reason})" );
					return resumedAction.ApplyResult( me, resumeResult );
				}

			case ActionResultType.Sustain:
			case ActionResultType.Continue:
			default:
				return this;
		}
	}

	/// <summary>
	/// Has this action been put out of scope by any actions that we suspended that wish to change or stop themselves, based on the event results.
	/// </summary>
	public bool IsOutOfScope()
	{
		for ( var under = BuriedUnderMe; under != null; under = under.BuriedUnderMe )
		{
			if ( under.EventResult.IsChangeTo || under.EventResult.IsDone )
				return true;
		}

		return false;
	}

	public override string ToString() => GetType().Name;
}

public abstract class IActionResult<T> where T : INextBot
{
	public ActionResultType Type;
	public NextBotAction<T> Action;
	public string Reason;

	public IActionResult( ActionResultType type, NextBotAction<T> action = null, string reason = "" )
	{
		Type = type;
		Action = action;
		Reason = reason;
	}

	public bool IsDone => Type == ActionResultType.Done;
	public bool IsContinue => Type == ActionResultType.Continue;
	public bool IsSuspendFor => Type == ActionResultType.SuspendFor;
	public bool IsChangeTo => Type == ActionResultType.ChangeTo;
	public bool IsRequestingChange => Type == ActionResultType.ChangeTo || Type == ActionResultType.SuspendFor || IsDone;
}

public class ActionResult<T> : IActionResult<T> where T : INextBot
{
	public ActionResult( ActionResultType type, NextBotAction<T> action = null, string reason = "" ) : base( type, action, reason ) { }
}


public class EventDesiredResult<T> : IActionResult<T> where T : INextBot
{
	public EventResultPriorityType Priority;

	public EventDesiredResult( ActionResultType type, NextBotAction<T> action = null, EventResultPriorityType priority = EventResultPriorityType.Try, string reason = "" ) : base( type, action, reason )
	{
		Priority = priority;
	}
}

public enum ActionResultType
{
	Continue,
	ChangeTo,
	SuspendFor,
	Done,
	Sustain
}

public enum EventResultPriorityType
{
	/// <summary>
	/// No result
	/// </summary>
	None,
	/// <summary>
	/// Use this result, or toss it out, either is ok
	/// </summary>
	Try,
	/// <summary>
	/// Try extra-hard to use this result
	/// </summary>
	Important,
	/// <summary>
	/// This result must be used - emit an error if it can't be
	/// </summary>
	Critical
}
