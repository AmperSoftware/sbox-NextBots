namespace Amper.NextBot;

partial class NextBotAction<T>
{
	public virtual EventDesiredResult<T> OnInjured( T me, NextBotEventInjured args ) => TryContinue();
	public virtual EventDesiredResult<T> OnKilled( T me, NextBotEventKilled args ) => TryContinue();
	public virtual EventDesiredResult<T> OnStuck( T me, NextBotEventStuck args ) => TryContinue();
	public virtual EventDesiredResult<T> OnUnstuck( T me, NextBotEventUnStuck args ) => TryContinue();
	public virtual EventDesiredResult<T> OnMoveToFailure( T me, NextBotEventMoveToFailure args ) => TryContinue();
	public virtual EventDesiredResult<T> OnMoveToSuccess( T me, NextBotEventMoveToSuccess args ) => TryContinue();
	public virtual EventDesiredResult<T> OnSight( T me, NextBotEventSight args ) => TryContinue();
	public virtual EventDesiredResult<T> OnLostSight( T me, NextBotEventLostSight args ) => TryContinue();
}
