using Sandbox;

namespace Amper.NextBot;

partial class NextBotLocomotion
{
	public const float StuckRadius = 100;
	TimeSince TimeSinceStuck { get; set; }
	Vector3 StuckPosition { get; set; }
	CountdownTimer StillStuckTimer { get; set; } = new();

	public bool IsStuck { get; set; }

	public void StuckMonitor()
	{
		const float idleTime = 0.25f;
		if ( TimeSinceMoveRequested > idleTime )
		{
			// we have no desire to move, and therefore cannot emit stuck events

			// prepare our internal state for when the bot starts to move next
			StuckPosition = GetFeet();
			TimeSinceStuck = 0;

			return;
		}

		if ( IsStuck )
		{
			if ( Bot.Entity.Position.Distance( StuckPosition ) > StuckRadius )
			{
				// we've just become un-stuck
				ClearStuckStatus( "No longer stuck!" );
			}
			else
			{
				if ( StillStuckTimer.IsElapsed() )
				{
					StillStuckTimer.Start( 1 );

					if ( NextBots.IsDebugging( NextBotDebugFlags.Locomotion ) )
					{
						Log.Info( $"{Bot.Entity} Still Stuck" );
						DebugOverlay.Circle( StuckPosition + Vector3.Up * 5, Rotation.From( -90, 0, 0 ), 5, Color.Red, 1 );
					}

					Bot.NextBot.InvokeEvent<NextBotEventStuck>();
				}
			}
		}
		else
		{
			// we're not stuck - yet

			if( Bot.Entity.Position.Distance( StuckPosition ) > StuckRadius )
			{
				// we have moved - reset anchor
				StuckPosition = GetFeet();
				TimeSinceStuck = 0;
			}
			else
			{
				// within stuck range of anchor. if we've been here too long, we're stuck
				if ( NextBots.IsDebugging( NextBotDebugFlags.Locomotion ) )
				{
					DebugOverlay.Line( Bot.Entity.WorldSpaceBounds.Center, StuckPosition, Color.Magenta, 0.1f, true );
				}

				// commented this until and if we need GetDesiredSpeed() anywhere
				// var minMoveSpeed = 0.1f * GetDesiredSpeed() + 0.1f;
				// var escapeTime = StuckRadius / minMoveSpeed;

				// 2 seconds of being in one place means we're stuck.
				var escapeTime = 2;

				if ( TimeSinceStuck > escapeTime )
				{
					// we have taken too long - we're stuck
					IsStuck = true;

					NextBots.Msg( NextBotDebugFlags.Locomotion, $"{Bot.Entity} is STUCK at position: {StuckPosition}" );

					// tell other components we've become stuck
					Bot.NextBot.InvokeEvent<NextBotEventStuck>();

					// Dont call still stuck immediately after.
					StillStuckTimer.Start( 1 );
				}
			}
		}
	}

	public void ClearStuckStatus( string reason )
	{
		if ( IsStuck )
		{
			IsStuck = false;

			// tell other components we're no longer stuck
			Bot.NextBot.InvokeEvent<NextBotEventUnStuck>();
		}

		StuckPosition = GetFeet();
		TimeSinceStuck = 0;

		NextBots.Msg( NextBotDebugFlags.Locomotion, $"ClearStuckStatus: {Bot.Entity} (reason: \"{reason}\")" );
	}
}
