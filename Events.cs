using Sandbox;

namespace Amper.NextBot;

/// <summary>
/// This bot has been killed.
/// </summary>
public class NextBotEventKilled : NextBotEvent { }
/// <summary>
/// This bot was injured.
/// </summary>
public class NextBotEventInjured : NextBotEvent
{
	public DamageInfo DamageInfo { get; set; }
}
/// <summary>
/// We have succesfully reached our path destination.
/// </summary>
public class NextBotEventMoveToSuccess : NextBotEvent
{
	public NextBotPathFollower Path { get; set; }
}
/// <summary>
/// We have failed to reach our path destination.
/// </summary>
public class NextBotEventMoveToFailure : NextBotEvent
{
	public NextBotPathFollower Path { get; set; }
}
/// <summary>
/// This bot is stuck.
/// </summary>
public class NextBotEventStuck : NextBotEvent { }
/// <summary>
/// This bot is no longer stuck.
/// </summary>
public class NextBotEventUnStuck : NextBotEvent { }
/// <summary>
/// This bot has landed on the ground.
/// </summary>
public class NextBotEventLandOnGround : NextBotEvent { }
/// <summary>
/// This bot has left the ground.
/// </summary>
public class NextBotEventLeaveGround : NextBotEvent { }
/// <summary>
/// We have sighted another entity.
/// </summary>
public class NextBotEventSight : NextBotEvent
{
	public Entity Entity { get; set; }
}
/// <summary>
/// We have lost sight of another entity.
/// </summary>
public class NextBotEventLostSight : NextBotEvent
{
	public Entity Entity { get; set; }
}

