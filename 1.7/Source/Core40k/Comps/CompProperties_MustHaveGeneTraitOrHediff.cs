using RimWorld;
using Verse;

namespace Core40k;

public class CompProperties_MustHaveGene : CompProperties_AbilityEffect
{
    public GeneDef geneDef = null;

    public CompProperties_MustHaveGene()
    {
        compClass = typeof(CompAbilityEffect_MustHaveGene);
    }
}