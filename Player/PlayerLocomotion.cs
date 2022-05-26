using Sandbox;

namespace Amper.NextBot;

public class NextBotPlayerLocomotion : NextBotLocomotion
{
	public NextBotPlayerLocomotion( INextBot me ) : base( me ) { }

	bool IsJumping = false;
	CountdownTimer JumpTimer = new();

	public override void Jump()
	{
		IsJumping = true;
		JumpTimer.Start( 0.5f );

		Bot.NextBot.PressInputButton( InputButton.Jump );
	}

	public override bool IsClimbingOrJumping()
	{
		if ( !IsJumping )
			return false;

		if ( JumpTimer.IsElapsed() && IsOnGround() )
		{
			IsJumping = false;
			return false;
		}

		return true;
	}

	public override void Approach( Vector3 point, float goalWeigth = 1 )
	{
		base.Approach( point );

		if ( NextBots.IsDebugging( NextBotDebugFlags.Locomotion ) )
			DebugOverlay.Line( GetFeet(), point, Color.Magenta, 0.1f, true );

		var input = Bot.NextBot as INextBotPlayerInput;
		if ( input == null ) 
		{
			Log.Error( "NextBotPlayerLocomotion::Approach: No INextBotPlayerInput" );
			return;
		}

		var toPoint = (point - GetFeet()).WithZ( 0 ).Normal;
		input.AnalogMove( toPoint );
	}
}
