using Sandbox;

namespace Amper.NextBot;

public interface INextBotPlayerInput
{
	public void PressInputButton( InputButton button, float duration = -1 );
	public void ReleaseInputButton( InputButton button );

	public void AnalogMove( Vector3 vector, float duration = -1 );
	public void FaceTowards( Rotation rotation );
}

public struct NextBotInput
{
	public InputButton Buttons;
	public Vector3 AnalogMove;
	public Rotation Rotation;
}
