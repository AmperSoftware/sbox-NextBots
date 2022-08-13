using Sandbox;

namespace Amper.NextBot;

public class IntervalTimer
{
	float Timestamp;

	public IntervalTimer()
	{
		Timestamp = -1.0f;
	}

	public void Reset()
	{
		Timestamp = Time.Now;
	}

	public void Start()
	{
		Timestamp = Time.Now;
	}

	public void Invalidate()
	{
		Timestamp = -1.0f;
	}

	public bool HasStarted()
	{
		return Timestamp > 0;
	}

	public float GetElapsedTime()
	{
		return HasStarted() ? Time.Now - Timestamp : 99999.9f;
	}

	public bool IsLessThen( float duration )
	{
		return Time.Now - Timestamp < duration ? true : false;
	}

	public bool IsGreaterThen( float duration )
	{
		return Time.Now - Timestamp > duration ? true : false;
	}
}
