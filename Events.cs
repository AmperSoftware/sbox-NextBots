using Sandbox;

namespace Amper.NextBot;

public class NextBotEventKilled : NextBotEvent { }

public class NextBotEventInjured : NextBotEvent
{
	public DamageInfo DamageInfo { get; set; }
}

public class NextBotEventMoveToSuccess : NextBotEvent
{
	public NextBotPathFollower Path { get; set; }
}

public class NextBotEventMoveToFailure : NextBotEvent
{
	public NextBotPathFollower Path { get; set; }
}

public class NextBotEventStuck : NextBotEvent { }
public class NextBotEventUnStuck : NextBotEvent { }
public class NextBotEventLandOnGround : NextBotEvent { }
public class NextBotEventLeaveGround : NextBotEvent { }

public class NextBotEventSight : NextBotEvent
{
	public Entity Entity { get; set; }
}

public class NextBotEventLostSight : NextBotEvent
{
	public Entity Entity { get; set; }
}

