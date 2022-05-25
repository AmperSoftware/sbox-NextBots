using Sandbox;
using System;

namespace Amper.NextBot;

partial class NextBots
{
	public static NextBotDebugFlags DebugFlags { get; set; }
	/// <summary>
	/// Are we currently debugging any specific nextbot system?
	/// </summary>
	public static bool IsDebugging( NextBotDebugFlags flag ) => DebugFlags.HasFlag( flag );
	/// <summary>
	/// Are we currently debugging anything?
	/// </summary>
	public static bool IsDebugging() => DebugFlags != 0;

	//
	// Debug Logs
	//

	public static void Msg( NextBotDebugFlags flags, FormattableString message )
	{
		if ( DebugFlags.HasFlag( flags ) )
			Log.Info( message );
	}

	public static void Msg( NextBotDebugFlags flags, object message )
	{
		if ( DebugFlags.HasFlag( flags ) )
			Log.Info( message );
	}

	//
	// Debug Errors
	//

	public static void MsgError( NextBotDebugFlags flags, FormattableString message )
	{
		if ( DebugFlags.HasFlag( flags ) )
			Log.Error( message );
	}

	public static void MsgError( NextBotDebugFlags flags, object message )
	{
		if ( DebugFlags.HasFlag( flags ) )
			Log.Error( message );
	}

	[ConCmd.Server]
	public static void nb_debug( NextBotDebugFlags flag )
	{
		if ( DebugFlags.HasFlag( flag ) )
		{
			Log.Info( "<NextBot> Debug Flag DISABLED: " + flag );
			DebugFlags &= ~flag;
		}
		else
		{
			Log.Info( "<NextBot> Debug Flag ENABLED: " + flag );
			DebugFlags |= flag;
		}
	}
}


[Flags]
public enum NextBotDebugFlags
{
	None = 0,

	Behavior = 1,
	Events = 2,
	Locomotion = 4,
	Path = 8,
	Vision = 16,

	All = 0xFFFF
}
