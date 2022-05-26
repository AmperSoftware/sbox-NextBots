namespace Amper.NextBot;

partial class NextBotAction<T>
{
	/// <summary>
	/// I have been injured by someone!
	/// </summary>
	public virtual EventDesiredResult<T> OnInjured( T me, NextBotEventInjured args ) => TryContinue();
	/// <summary>
	/// I have been killed by someone!
	/// </summary>
	public virtual EventDesiredResult<T> OnKilled( T me, NextBotEventKilled args ) => TryContinue();

	/// <summary>
	/// I am stuck!
	/// </summary>
	public virtual EventDesiredResult<T> OnStuck( T me, NextBotEventStuck args ) => TryContinue();
	/// <summary>
	/// I am no longer stuck!
	/// </summary>
	public virtual EventDesiredResult<T> OnUnstuck( T me, NextBotEventUnStuck args ) => TryContinue();

	/// <summary>
	/// I have failed to reach my target destination.
	/// </summary>
	public virtual EventDesiredResult<T> OnMoveToFailure( T me, NextBotEventMoveToFailure args ) => TryContinue();
	/// <summary>
	/// I have succesfully reached my target destination.
	/// </summary>
	public virtual EventDesiredResult<T> OnMoveToSuccess( T me, NextBotEventMoveToSuccess args ) => TryContinue();

	/// <summary>
	/// I have caught sight of another entity.
	/// </summary>
	public virtual EventDesiredResult<T> OnSight( T me, NextBotEventSight args ) => TryContinue();
	/// <summary>
	/// I have lost sight of another entity.
	/// </summary>
	public virtual EventDesiredResult<T> OnLostSight( T me, NextBotEventLostSight args ) => TryContinue();

	/// <summary>
	/// I have left the ground.
	/// </summary>
	public virtual EventDesiredResult<T> OnLeaveGround( T me, NextBotEventLeaveGround args ) => TryContinue();
	/// <summary>
	/// I have landed on the ground.
	/// </summary>
	public virtual EventDesiredResult<T> OnLandOnGround( T me,  NextBotEventLandOnGround args ) => TryContinue();
}
