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

	public override KnownEntity OnPreInvocation( INextBot me )
	{
		if ( Threat1 == null || Threat1.IsObsolete() )
		{
			if ( Threat2 != null && !Threat2.IsObsolete() )
				return Threat2;

			return null;
		}
		else if ( Threat2 == null || Threat2.IsObsolete() )
		{
			return Threat1;
		}

		return null;
	}

	public override KnownEntity OnPostInvocation( INextBot me )
	{
		var subject = me.Entity;
		if ( !subject.IsValid() )
			return null;

		// no specific decision was made - return closest threat as most dangerous
		float range1 = (subject.Position - Threat1.LastKnownPosition).LengthSquared;
		float range2 = (subject.Position - Threat2.LastKnownPosition).LengthSquared;

		if ( range1 < range2 )
			return Threat1;

		return Threat2;
	}
}
