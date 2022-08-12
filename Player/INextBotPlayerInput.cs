using Sandbox;

namespace Amper.NextBot;

public interface INextBotPlayerInput
{
	public void PressInputButton( InputButton button, float duration = -1 );
	public void ReleaseInputButton( InputButton button );

	public void SetAnalogMove( Vector3 vector, float duration = -1 );
	public void SetViewAngles( Angles rotation );
}

public struct NextBotInput
{
	public InputButton Buttons;
	public Vector3 AnalogMove;
	public Angles ViewAngles;
}
