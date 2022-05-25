namespace Amper.NextBot;

public interface INextBotBody 
{
	public float GetHullWidth();
	public float GetHullHeight();

	public Vector3 GetEyePosition();
	public Vector3 GetViewVector();
}

public class NextBotBody : NextBotComponent, INextBotBody
{
	public NextBotBody( INextBot bot ) : base( bot ) { }

	public virtual float GetHullWidth() => Bot.Entity.WorldSpaceBounds.Size.x;
	public virtual float GetHullHeight() => Bot.Entity.WorldSpaceBounds.Size.y;

	public Vector3 GetEyePosition() => Bot.Entity.EyePosition;
	public Vector3 GetViewVector() => Bot.Entity.EyeRotation.Forward;
}
