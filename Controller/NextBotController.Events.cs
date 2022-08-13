using Sandbox;

namespace Amper.NextBot;

partial class NextBotController
{
	/// <summary>
	/// Invoke an event for all the nextbot components to receive.
	/// </summary>
	public void InvokeEvent<T>() where T : NextBotEvent, new()
	{
		InvokeEvent( new T() );
	}

	/// <summary>
	/// Invoke an event for all the nextbot components to receive.
	/// </summary>
	public void InvokeEvent( NextBotEvent invokedEvent ) 
	{
		InvokeEvent( this, invokedEvent );
	}

	private void InvokeEvent( INextBotEventResponder parent, NextBotEvent args )
	{
		parent.OnEvent( args );

		for ( var sub = parent.FirstContainedResponder(); sub != null; sub = parent.NextContainedResponder( sub ) )
		{
			InvokeEvent( sub, args );
		}
	}
}

public class NextBotEvent
{
	public override string ToString() => GetType().Name;
}
