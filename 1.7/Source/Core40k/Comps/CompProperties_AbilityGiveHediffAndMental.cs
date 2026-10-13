using RimWorld;
using Verse;

namespace Core40k;

public class CompProperties_AbilityGiveHediffAndMental : CompProperties_AbilityGiveHediff
{
    public MentalStateDef mentalStateDef;

    public CompProperties_AbilityGiveHediffAndMental()
    {
        compClass = typeof(CompAbilityEffect_GiveHediffAndMentalBreak);
    }
}