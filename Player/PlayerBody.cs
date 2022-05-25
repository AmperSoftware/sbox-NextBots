using Sandbox;

namespace Amper.NextBot;

public class NextBotPlayerBody : NextBotBody
{
	/// <summary>
	/// The player we're locomoting.
	/// </summary>
	Player Player { get; set; }

	public NextBotPlayerBody( INextBot me ) : base( me )
	{
		Player = (Player)me;
	}
}
