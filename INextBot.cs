using Sandbox;

namespace Amper.NextBot;

public interface INextBot : IValid
{
	public NextBotController NextBot { get; set; }

	public Entity Entity => this as Entity;

	public Vector3 Position => Entity.Position;
	public Rotation Rotation => Entity.Rotation;
	public Vector3 Velocity => Entity.Velocity;
}
