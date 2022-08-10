using System;

namespace Amper.NextBot;

public interface INextBotEventResponder
{
	public INextBotEventResponder FirstContainedResponder();
	public INextBotEventResponder NextContainedResponder( INextBotEventResponder current );

	public void OnEvent( NextBotEvent args );
	public ResponseType OnQuery<ResponseType>( NextBotContextualQuery<ResponseType> args );
}
