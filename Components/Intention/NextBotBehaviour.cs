namespace Amper.NextBot;

public interface INextBotBehavior { }

/// <summary>
/// This is the behavior of our bot, it manages Actions that define what the bot is doing.
/// </summary>
public class NextBotBehavior<T> : INextBotEventReceiver where T :  INextBot
{
	T Me { get; set; }
	NextBotAction<T> Action { get; set; }

	public NextBotBehavior( NextBotAction<T> initialAction )
	{
		Action = initialAction;
	}

	public bool IsEmpty => Action == null;

	public void Update( T me, float interval )
	{
		if ( me == null || IsEmpty ) 
			return;

		Me = me;
		Action = Action.ApplyResult( Me, Action.InvokeUpdate( Me, interval ) );

		if ( Action != null && NextBots.IsDebugging( NextBotDebugFlags.Behavior ) )
			PrintDebug();
	}

	public void OnEvent( NextBotEvent args )
	{
		Action?.OnEvent( args );
	}

	public void Stop()
	{
		if ( Me != null && Action != null )
		{
			Action.InvokeOnEnd( Me, null );
			Me = default( T );
		}
	}

	public void PrintDebug()
	{
		Me.NextBot.DisplayDebugText( "Actions:" );

		var rootAction = Action;
		while ( rootAction.Parent != null )
			rootAction = rootAction.Parent;

		var action = rootAction;

		do
		{
			var debugStr = $"- {action}";

			var buried = action.BuriedUnderMe;
			if ( buried != null )
			{
				debugStr += " (";

				while ( buried != null )
				{
					debugStr += " << " + buried;
					buried = buried.BuriedUnderMe;
				}

				debugStr += " )";
			}

			Me.NextBot.DisplayDebugText( debugStr );
			action = action.Child;
		}
		while ( action != null );
	}
}
