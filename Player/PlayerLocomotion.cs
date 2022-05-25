using Sandbox;

namespace Amper.NextBot;

public class NextBotPlayerLocomotion : NextBotLocomotion
{
	/// <summary>
	/// The player we're locomoting.
	/// </summary>
	Player Player { get; set; }

	public NextBotPlayerLocomotion( INextBot me ) : base( me )
	{
		Player = (Player)me;
	}

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
		base.Approach( point, goalWeigth );

		if ( NextBots.IsDebugging( NextBotDebugFlags.Locomotion ) )
			DebugOverlay.Line( GetFeet(), point, Color.Magenta, 0.1f, true );

		var input = Bot.NextBot as INextBotPlayerInput;
		if ( input == null ) 
		{
			Log.Error( "NextBotPlayerLocomotion::Approach: No INextBotPlayerInput" );
			return;
		}

		var forward = Player.EyeRotation.Forward.WithZ( 0 ).Normal;
		var right = Player.EyeRotation.Right.WithZ( 0 ).Normal;

		var toPoint = (point - GetFeet()).WithZ( 0 ).Normal;

		input.AnalogMove( toPoint );
		return;

		var ahead = toPoint.Dot( forward );
		var side = toPoint.Dot( right );

		const float epsilon = 0.25f;

		if ( ahead > epsilon )
		{
			input.PressInputButton( InputButton.Forward );

			if ( NextBots.IsDebugging( NextBotDebugFlags.Locomotion ) )
				DebugOverlay.Line( Player.Position, Player.Position + 50 * forward, Color.Green, 0.1f );
		}
		else if ( ahead < -epsilon )
		{
			input.PressInputButton( InputButton.Back );

			if ( NextBots.IsDebugging( NextBotDebugFlags.Locomotion ) )
				DebugOverlay.Line( Player.Position, Player.Position - 50 * forward, Color.Red, 0.1f );
		}

		Bot.NextBot.DisplayDebugText( $"Locomotion: ahead: {ahead}, side: {side}" );


		if ( side <= -epsilon )
		{
			input.PressInputButton( InputButton.Left );

			if ( NextBots.IsDebugging( NextBotDebugFlags.Locomotion ) )
				DebugOverlay.Line( Player.Position, Player.Position - 50 * right, Color.Yellow, 0.1f );
		}
		else if ( side >= epsilon )
		{
			input.PressInputButton( InputButton.Right );

			if ( NextBots.IsDebugging( NextBotDebugFlags.Locomotion ) )
				DebugOverlay.Line( Player.Position, Player.Position + 50 * forward, Color.Cyan, 0.1f );
		}
	}
}
