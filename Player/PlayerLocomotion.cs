using Sandbox;

namespace Amper.NextBot;

public partial class NextBotPlayerLocomotion : NextBotLocomotion
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

		var forward = Bot.EyeRotation.Forward;
		var left = Bot.EyeRotation.Left;

		var fmove = forward.Dot( toPoint );
		var smove = left.Dot( toPoint );

		input.AnalogMove( new Vector3( fmove, smove, 0 ) );
	}

	public override void FaceTowards( Vector3 target )
	{
		var input = Bot.NextBot as INextBotPlayerInput;
		if ( input == null )
		{
			Log.Error( "NextBotPlayerLocomotion::Approach: No INextBotPlayerInput" );
			return;
		}

		var toTarget = (target - Bot.EyePosition).Normal;
		input.FaceTowards( Rotation.LookAt( toTarget ) );
	}
}
