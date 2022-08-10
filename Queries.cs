using Sandbox;

namespace Amper.NextBot;

public class NextBotQuerySelectMoreDangerousThreat : NextBotContextualQuery<KnownEntity>
{
	public KnownEntity Threat1;
	public KnownEntity Threat2;

	public NextBotQuerySelectMoreDangerousThreat( KnownEntity threat1, KnownEntity threat2 )
	{
		Threat1 = threat1;
		Threat2 = threat2;
	}
}

