using Sandbox;
using System;

namespace Amper.NextBot;

public class KnownEntity : IEquatable<KnownEntity>
{
	public Entity Entity { get; set; }
	public Vector3 LastKnownPosition { get; set; }
	public bool HasLastKnownPositionBeforeSeen { get; set; }
	public bool IsVisible { get; set; }

	public float WhenLastSeen { get; set; }
	public float WhenLastBecameVisible { get; set; }
	public float WhenLastKnown { get; set; }
	public float WhenBecameKnown { get; set; }

	public KnownEntity( Entity who )
	{
		Entity = who;
		WhenLastSeen = -1;
		WhenLastBecameVisible = -1;
		IsVisible = false;

		WhenBecameKnown = Time.Now;
		HasLastKnownPositionBeforeSeen = false;

		UpdatePosition();
	}

	public virtual void UpdatePosition()
	{
		if ( Entity != null && Entity.IsValid )
		{
			LastKnownPosition = Entity.Position;
			WhenLastKnown = Time.Now;
		}
	}

	public virtual void UpdateVisibilityStatus( bool visible )
	{
		if ( visible )
		{
			if ( !IsVisible )
			{
				// just became visible
				WhenLastBecameVisible = Time.Now;
			}

			WhenLastSeen = Time.Now;
		}

		IsVisible = visible;
	}

	public virtual bool WasEverVisible()
	{
		return WhenLastSeen > 0;
	}

	public virtual float GetTimeSinceLastKnown()
	{
		return Time.Now - WhenLastKnown;
	}

	public virtual float GetTimeSinceLastSeen()
	{
		return Time.Now - WhenLastSeen;
	}

	public virtual float GetTimeSinceBecameVisible()
	{
		return Time.Now - WhenLastBecameVisible;
	}

	public virtual float GetTimeSinceBecameKnown()
	{
		return Time.Now - WhenBecameKnown;
	}

	public virtual bool IsObsolete()
	{
		return Entity == null || !Entity.IsValid || Entity.LifeState != LifeState.Alive || GetTimeSinceLastKnown() > 10;
	}

	public virtual bool IsVisibleRecently()
	{
		if ( IsVisible )
			return true;

		if ( WasEverVisible() && GetTimeSinceLastSeen() < 3 )
			return true;

		return false;
	}

	public bool Equals( KnownEntity other )
	{
		if ( Entity == null || other.Entity == null )
			return false;

		return Entity == other.Entity;
	}
}
