using RimWorld;

namespace Core40k;

public class CompProperties_ResetRanks : CompProperties_AbilityEffect
{
    public int canDemoteToTierInclusive = 0;

    public RankCategoryDef rankCategoryDef = null;

    public bool ownRankAsTier = false;

    public CompProperties_ResetRanks()
    {
        compClass = typeof(CompAbilityEffect_ResetRanks);
    }
}