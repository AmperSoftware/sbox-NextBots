using Sandbox;
using System.Collections.Generic;

namespace Amper.NextBot;

partial class NextBotController : INextBotPlayerInput
{
	NextBotInput Input;
	Dictionary<InputButton, float> InputButtonsTime = new();
	float AnalogMoveTime;

	/// <summary>
	/// Force player bot to press a specific button.
	/// </summary>
	public void PressInputButton( InputButton button, float duration = -1 )
	{
		Input.Buttons |= button;
		InputButtonsTime[button] = Time.Now + duration;
	}

	/// <summary>
	/// Force player bot to release a specific button.
	/// </summary>
	public void ReleaseInputButton( InputButton button )
	{
		Input.Buttons &= ~button;
		InputButtonsTime[button] = -1;
	}

	public void BuildInput( InputBuilder builder )
	{
		//
		// press buttons that we are still pressing, and unpress
		// those that we stopped pressing.
		//

		builder.SetButton( Input.Buttons, true );
		builder.SetButton( ~Input.Buttons, false );

		//
		// compute input direction.
		//

		var move = Input.AnalogMove;

		if ( Input.Buttons.HasFlag( InputButton.Forward ) )
			move.x += 1;

		if ( Input.Buttons.HasFlag( InputButton.Back ) )
			move.x -= 1;

		if ( Input.Buttons.HasFlag( InputButton.Right ) )
			move.y -= 1;

		if ( Input.Buttons.HasFlag( InputButton.Left ) )
			move.y += 1;

		move = move.Clamp( -1, 1 );
		move = move.Normal;
		builder.InputDirection = move;

		//
		// rotation
		//

		builder.ViewAngles = Input.Rotation.Angles();
	}

	public void ReleaseExpiredButtons()
	{
		// Expired Input Buttons
		foreach ( var pair in InputButtonsTime )
		{
			var button = pair.Key;

			if ( !Input.Buttons.HasFlag( button ) )
				continue;

			var time = pair.Value;
			if ( time >= Time.Now )
				continue;

			Input.Buttons &= ~button;
		}

		// Expired Analog Move
		if ( AnalogMoveTime < Time.Now )
			Input.AnalogMove = 0;
	}

	public void AnalogMove( Vector3 vector, float duration = -1 )
	{
		Input.AnalogMove = vector;
		AnalogMoveTime = Time.Now + duration;
	}

	public void FaceTowards( Rotation rotation )
	{
		Input.Rotation = rotation;
	}
}
