using Sandbox;

namespace Amper.NextBot;

partial class NextBotController
{
	public void InvokeEvent<T>() where T : NextBotEvent, new()
	{
		InvokeEvent( new T() );
	}

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
