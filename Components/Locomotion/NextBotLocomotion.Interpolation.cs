using Sandbox;
using System;

namespace Amper.NextBot;

partial class NextBotLocomotion
{
	public bool InterpolationEnabled = true;
	bool InterpolateTillNextUpdate;
	float InterpolationTime;

	Vector3 LastPosition;
	Vector3 UpdatePosition;

	public bool StartInterpolation()
	{
		InterpolateTillNextUpdate = false;

		if ( !InterpolationEnabled )
			return false;

		LastPosition = Position;
		return true;
	}

	public bool StopInterpolation()
	{
		if ( !InterpolationEnabled )
			return false;

		InterpolationTime = 0;
		InterpolateTillNextUpdate = true;

		UpdatePosition = Position;
		return true;
	}

	public void UpkeepInterpolate()
	{
		InterpolationTime += Time.Delta;
		var fraction = Math.Clamp( InterpolationTime / CurrentInterval, 0, 1 );
		InterpolationMoveToFraction( fraction );

		DebugOverlay.Sphere( LastPosition, 5, Color.Green, CurrentInterval );
		DebugOverlay.Sphere( Bot.Position, 5, Color.Yellow, CurrentInterval );
		DebugOverlay.Sphere( UpdatePosition, 5, Color.Red, CurrentInterval );
	}

	public void InterpolationMoveToFraction( float fraction )
	{
		if ( !InterpolateTillNextUpdate )
			return;

		Bot.Position = LastPosition.LerpTo( UpdatePosition, fraction );
	}
}
