using Sandbox;

namespace Amper.NextBot;

/// <summary>
/// Extend this interface on any entity that you wish to turn into a nextbot.
/// </summary>
public interface INextBot : IValid
{
	public NextBotController NextBot { get; set; }

	public Entity Entity => this as Entity;

	public Vector3 Position { get => Entity.Position; set => Entity.Position = value; }
	public Rotation Rotation { get => Entity.Rotation; set => Entity.Rotation = value; }
	public Vector3 Velocity { get => Entity.Velocity; set => Entity.Velocity = value; }
	public Entity GroundEntity { get => Entity.GroundEntity; set => Entity.GroundEntity = value; }

	public Vector3 EyePosition { get => Entity.EyePosition; set => Entity.EyePosition = value; }
	public Rotation EyeRotation { get => Entity.EyeRotation; set => Entity.EyeRotation = value; }
	public Vector3 ViewVector => EyeRotation.Forward;

	public Vector3 Mins => WorldSpaceBounds.Mins - Position;
	public Vector3 Maxs => WorldSpaceBounds.Maxs - Position;

	public BBox WorldSpaceBounds => Entity.WorldSpaceBounds;
}
