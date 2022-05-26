using Sandbox;

namespace Amper.NextBot;

/// <summary>
/// Extend this interface on any entity that you wish to turn into a nextbot.
/// </summary>
public interface INextBot : IValid
{
	public NextBotController NextBot { get; set; }

	public Entity Entity => this as Entity;

	public Vector3 Position { get { return Entity.Position; } set { Entity.Position = value; } }
	public Rotation Rotation { get { return Entity.Rotation; } set { Entity.Rotation = value; } }
	public Vector3 Velocity { get { return Entity.Velocity; } set { Entity.Velocity = value; } }
	public Vector3 BaseVelocity { get { return Entity.BaseVelocity; } set { Entity.BaseVelocity = value; } }
	public Entity GroundEntity { get { return Entity.GroundEntity; } set { Entity.GroundEntity = value; } }

	public Vector3 EyePosition => Entity.EyePosition;
	public Rotation EyeRotation => Entity.EyeRotation;
	public Vector3 ViewVector => EyeRotation.Forward;

	public Vector3 Mins => WorldSpaceBounds.Mins - Position;
	public Vector3 Maxs => WorldSpaceBounds.Maxs - Position;

	public BBox WorldSpaceBounds => Entity.WorldSpaceBounds;
}
