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
		for ( var comp = FirstComponent; comp != null; comp = comp.NextComponent ) 
			comp.OnEvent( invokedEvent );
	}
}

public class NextBotEvent
{
	public override string ToString() => GetType().Name;
}
